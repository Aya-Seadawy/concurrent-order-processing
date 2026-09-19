# Design Note: Concurrent Order Processing

## 1. Database Schema & ER Diagram

```mermaid
erDiagram
    Products {
        int Id PK
        string Code UK "Unique Product Code"
        string Name
        decimal UnitPrice "Decimal precision"
        int AvailableQuantity "Check >= 0"
    }

    Orders {
        guid Id PK
        string CustomerReference
        string Status "Confirmed | Cancelled"
        decimal TotalAmount
        datetime CreatedAtUtc
        datetime CancelledAtUtc "Nullable"
    }

    OrderLines {
        guid Id PK
        guid OrderId FK "Cascade on delete"
        string ProductCode
        int Quantity "Positive integer"
        decimal UnitPriceAtPurchase
    }

    OrderNotifications {
        guid Id PK "Stable EventId across retries"
        guid OrderId FK
        string Status "Pending | InProgress | Sent | Failed"
        int AttemptCount
        datetime NextAttemptAtUtc
        datetime ClaimedAtUtc "Nullable (lease check)"
        string LastError "Nullable"
        datetime CreatedAtUtc
        datetime SentAtUtc "Nullable"
    }

    IdempotencyKeys {
        string Key PK "Header Idempotency-Key"
        string RequestHash "SHA256 normalized payload"
        string Status "Processing | Completed"
        guid OrderId "Nullable FK"
        int ResponseStatusCode "Nullable (201 / 409)"
        text ResponseBody "Nullable JSON snapshot"
        datetime CreatedAtUtc
        datetime CompletedAtUtc "Nullable"
    }

    Orders ||--|{ OrderLines : contains
    Orders ||--o| OrderNotifications : triggers
    Orders ||--o| IdempotencyKeys : references
```

---

## 2. Order Creation & Idempotency Flow

Everything runs inside **one atomic database transaction**:

```mermaid
flowchart TD
    Start([POST /api/orders]) --> Hash[Compute Payload SHA256 Hash]
    Hash --> Tx[Begin DB Transaction]
    Tx --> InsertKey{Try INSERT into<br/>IdempotencyKeys<br/>Status=Processing}

    InsertKey -- Unique Constraint Failed --> CatchConflict[Rollback Tx & Query Existing Key]
    CatchConflict --> CheckHash{Stored Hash == New Hash?}
    CheckHash -- No --> Return409Conflict[Return 409 Conflict<br/>idempotency_key_conflict]
    CheckHash -- Yes & Status=Completed --> ReturnCached[Return Stored Response<br/>201 / 409 verbatim]
    CheckHash -- Yes & Status=Processing --> Return409Progress[Return 409 Conflict<br/>request_in_progress]

    InsertKey -- Insert Succeeded --> DeductStock[For each line: UPDATE Products<br/>AvailableQuantity = AvailableQuantity - Qty<br/>WHERE AvailableQuantity >= Qty]
    DeductStock --> StockSufficient{Rows Affected == 1?}

    StockSufficient -- No --> Compensate[Revert already deducted lines<br/>Set Key Status=Completed<br/>StatusCode=409 insufficient_stock]
    Compensate --> CommitReject[Commit Tx & Return 409 Conflict]

    StockSufficient -- Yes --> InsertOrder[INSERT Orders & OrderLines]
    InsertOrder --> InsertNotification[INSERT OrderNotifications<br/>Status=Pending]
    InsertNotification --> FinalizeKey[UPDATE IdempotencyKeys<br/>Status=Completed, StatusCode=201, OrderId]
    FinalizeKey --> CommitAll[Commit DB Transaction]
    CommitAll --> Return201[Return 201 Created]
```

---

## 3. Core Architecture Highlights

### A. Concurrency & Transactions
- **No Process-Local Locks**: Concurrency is enforced by database atomic conditional updates (`UPDATE Products ... WHERE AvailableQuantity >= @qty`) and relational unique keys (`PK on IdempotencyKeys.Key`).
- **All-in-One Transaction**: Idempotency claim, stock deduction, order record, and notification row share a single transaction. If a crash occurs before commit, the claim is rolled back completely—preventing ghost "stuck" records without distributed 2PC.
- **SQLite Concurrency Model**: SQLite uses single-writer serialized writes with a 5-second busy timeout (`Default Timeout=5`). Concurrent writes queue briefly; if the timeout expires, `409 request_in_progress` is returned. On PostgreSQL/SQL Server, this path is hit directly without waiting.

### B. Order Cancellation (Idempotent)
- Conditional atomic update: `UPDATE Orders SET Status='Cancelled' WHERE Id=@id AND Status='Confirmed'`.
- Only if exactly **1 row is updated** is inventory restored.
- If **0 rows are updated**, the system re-reads the order: if already `Cancelled`, it returns 200 idempotently without double-restoring stock; if missing, returns 404.

---

## 4. Extensions & Future Architecture (Design-Only)

### A. Multi-Worker Notification Scaling
```mermaid
sequenceDiagram
    participant W1 as Worker 1
    participant DB as Database (OrderNotifications)
    participant Svc as Delivery Service
    
    W1->>DB: UPDATE TOP(N) SET Status='InProgress', ClaimedBy='W1', ClaimedAtUtc=NOW<br/>WHERE Status='Pending' AND NextAttemptAtUtc <= NOW (SKIP LOCKED)
    W1->>Svc: SendAsync(StableEventId, Payload)
    alt Success
        W1->>DB: UPDATE SET Status='Sent', SentAtUtc=NOW
    else Failure
        W1->>DB: UPDATE SET Status='Pending', AttemptCount++, NextAttemptAtUtc=NOW+Backoff
    end
    Note over DB: Heartbeat & Lease Sweep:<br/>If ClaimedAtUtc expired (>30s) -> reset to 'Pending'
```
- **At-Least-Once Delivery**: Handled downstream by idempotency consumers keyed on the stable `EventId` (`OrderNotifications.Id`).

### B. Real Payment Gateway (Decoupled Network Call)
- **Do not hold DB transactions over HTTP calls**:
  1. Transaction 1: Reserve stock and save order as `PendingPayment`. Commit immediately.
  2. Outside transaction: Call payment provider with idempotent `PaymentIntentId`.
  3. Transaction 2: On payment success, update order to `Confirmed`.
  4. Background reconciliation job checks stuck `PendingPayment` orders against the payment gateway API to either finalize or release reserved stock.

---

## 5. Minimum Telemetry (Logs & Metrics)

| Telemetry Type | Name | Purpose |
|---|---|---|
| **Structured Log** | `StockConflict` | Alert when stock is exhausted (product, requested, available). |
| **Structured Log** | `IdempotencyKeyConflict` | Track payload tampering with reused keys. |
| **Structured Log** | `NotificationExhausted` | Alert operations when a delivery permanently fails. |
| **Metric Counter** | `orders_stock_conflict_total` | Business metric for inventory sizing. |
| **Metric Counter** | `orders_idempotency_duplicate_total` | Network/client retry volume. |
| **Metric Gauge** | `pending_notification_oldest_age_seconds` | SLA monitoring for notification worker backlog. |
