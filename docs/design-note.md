# Design Note

## Transaction boundary, concurrency mechanism, DB constraints

**Order creation** runs inside a single DB transaction per request: (1) attempt to `INSERT` a claim row into
`IdempotencyKeys` (PK on `Key`), (2) if that succeeds, run a conditional atomic `UPDATE Products SET
AvailableQuantity = AvailableQuantity - @qty WHERE Code=@code AND AvailableQuantity >= @qty` per line and check
rows-affected — zero means insufficient stock; on a multi-line failure, lines already deducted in this same
attempt are compensated (added back) before the transaction commits, so a rejected order never leaves a partial
stock change, (3) insert the `Order`/`OrderLines`/`OrderNotification` rows, (4) mark the claim `Completed` with
the response snapshot, (5) commit everything atomically. No process-local locks are used anywhere — correctness
comes entirely from the DB transaction plus the conditional `UPDATE`'s row-count check and the unique index on
`IdempotencyKeys.Key`, so the same design would hold under SQL Server/Postgres with real concurrent writers.
**Cancellation** uses the same pattern: `UPDATE Orders SET Status='Cancelled' WHERE Id=@id AND Status='Confirmed'`;
only if exactly one row is affected does the handler restore stock and commit — a second concurrent/replayed
cancel finds zero rows affected and idempotently returns the current (already-Cancelled) state without a second
restoration.

## Idempotency lifecycle

Everything — claim, business logic, and finalization — happens in **one** transaction, so a crash anywhere
before commit leaves nothing behind (no stuck "Processing" rows to reconcile later). The claim `INSERT` is
attempted first, before any business logic, to fail fast on a duplicate. On a unique-constraint violation the
existing row is read: hash mismatch → 409 `idempotency_key_conflict`; `Completed` + matching hash → the
**stored** response is replayed verbatim (no re-execution); still `Processing` → 409 `request_in_progress`.
Payload equivalence = same `customerReference` + the same multiset of `(productCode, quantity)` pairs, compared
order-insensitively via a SHA-256 hash of the normalized payload. SQLite is single-writer: a second concurrent
request's claim `INSERT` blocks (governed by `Default Timeout=5s` on the connection string) rather than failing
immediately, then resolves deterministically once the first transaction commits or rolls back — this is
documented as the "response while a duplicate is processing" behavior for this engine; on a real multi-writer
engine the 409 `request_in_progress` path would be hit directly instead of blocking.

## Multi-worker notification scaling (design-only)

Today one `BackgroundService` polls and claims rows itself (single instance, adequate for the assessment). To
run several worker instances safely: add a `ClaimedBy` (worker id) column and a lease (`ClaimedAtUtc` +
expiry), and claim with an atomic conditional update (`UPDATE ... SET Status='InProgress', ClaimedBy=@me WHERE
Status='Pending' AND NextAttemptAtUtc<=now LIMIT n`) — on Postgres/SQL Server this pairs with `FOR UPDATE SKIP
LOCKED` to let workers claim disjoint batches without blocking each other. Each worker renews its lease with a
heartbeat while processing; a periodic sweep releases any `InProgress` row whose lease has expired (crash
recovery) back to `Pending`. This makes delivery **at-least-once**: if a worker sends successfully but crashes
before marking `Sent`, the lease-expiry sweep will hand the same notification to another worker, which resends.
The stable `EventId` (the notification's own `Id`, unchanged across retries) is what lets a real downstream
consumer deduplicate that resend; this assessment's fake delivery service doesn't need to, but the id is already
plumbed through for that purpose.

## Real payment provider (design-only)

Add a `PendingPayment` order status. The DB transaction that reserves stock and creates the order **commits
first** (payment not yet attempted), with a `PaymentIntentId` column left null. A separate step calls the
payment provider **outside any DB transaction**; on success, a new short transaction stores the returned
`PaymentIntentId` and flips status to `Confirmed`. If the app crashes after the provider call succeeds but
before that update commits, the order is stuck `PendingPayment` with no recorded intent — solved by a
reconciliation job that, for stuck `PendingPayment` orders past a timeout, queries the provider (idempotently,
by an intent key generated before the call and sent to the provider) to discover the actual outcome and finalize
or compensate (release stock) accordingly. This avoids ever holding a DB transaction open across a network call.

## Minimum logs and metrics

Structured log events: `StockConflict` (product, requested/available qty), `IdempotencyDuplicate`/`IdempotencyReplay`
(key, outcome), `NotificationExhausted` (notification id, order id, attempts). Metrics: counters for
`orders_stock_conflict_total`, `orders_idempotency_duplicate_total`, `notifications_failed_total`; a
gauge/histogram for pending-notification age (`now - NextAttemptAtUtc` over the `Pending` set) to detect stuck
notifications before they exhaust retries.
