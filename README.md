# Concurrent Order Processing

ASP.NET Core (Clean Architecture) + EF Core/SQLite backend, and a minimal Angular standalone-component
frontend, implementing the "Concurrent Order Processing" technical assessment.

## Architecture

```
backend/
  src/OrderProcessing.Domain          - entities, enums, domain exceptions (no dependencies)
  src/OrderProcessing.Application     - MediatR commands/queries, validators, DTOs, interfaces (Clean Architecture core)
  src/OrderProcessing.Infrastructure  - EF Core DbContext + migrations, notification worker, fake delivery service
  src/OrderProcessing.Api             - Controllers, middleware, composition root
  tests/OrderProcessing.Domain.Tests
  tests/OrderProcessing.Application.Tests
  tests/OrderProcessing.IntegrationTests   - WebApplicationFactory + real SQLite file DB (per test class)
frontend/order-processing-ui          - Angular (standalone components, signals)
docs/design-note.md                   - transaction/concurrency/idempotency/scaling design notes
```

Dependency rule: `Domain <- Application <- Infrastructure`; `Api` composes `Application` + `Infrastructure`.

## Prerequisites

- .NET 8 SDK (or a newer SDK that can target `net8.0`, which is what this solution targets)
- Node.js 18+ and npm
- Angular CLI (`npm install -g @angular/cli`, or use `npx ng`)

## Setup, seed, and run

Database: SQLite, file-based (`orders.db`), created and **migrated + seeded automatically on startup**
(two products: `WIDGET-1`, `GADGET-1`). No manual seed step is required.

```powershell
# Backend API
cd backend
dotnet build
dotnet run --project src/OrderProcessing.Api
# Listens on http://localhost:5080 by default (see Properties/launchSettings.json) — matches the
# Angular app's API_BASE_URL. Swagger UI: http://localhost:5080/swagger
```

```powershell
# Frontend (in a second terminal)
cd frontend/order-processing-ui
npm install --legacy-peer-deps   # a known npm/arborist bug requires --legacy-peer-deps here
npx ng serve --port 4200
# Open http://localhost:4200
```

The Angular app calls the API at `http://localhost:5080/api` (see `src/app/api.config.ts`); the API's CORS
policy allows `http://localhost:4200`. Adjust both if you run on different ports.

## Tests

```powershell
cd backend
dotnet test
```

Runs 3 projects: `OrderProcessing.Domain.Tests`, `OrderProcessing.Application.Tests` (unit tests), and
`OrderProcessing.IntegrationTests`, which spins up the full API via `WebApplicationFactory` against a
**real, file-based SQLite database** (a fresh temp file per test class — the EF Core InMemory provider is
never used) and covers all six required scenarios:

1. `ConcurrentDistinctOrdersTests` — two different orders vs 1 unit of stock: one 201, one 409, stock ends at 0.
2. `ConcurrentSameIdempotencyKeyTests` — same key fired 8x concurrently: exactly one order, one stock
   deduction, one notification row.
3. `IdempotencyKeyReuseWithDifferentPayloadTests` — reusing a settled key with a different quantity: 409, no
   additional DB changes.
4. `ConcurrentCancelTests` — cancelling the same order twice concurrently: final status `Cancelled`, stock
   restored exactly once.
5. `TransactionRollbackTests` — a chaos hook forces a failure just before commit: stock/order/notification/
   idempotency-key changes all roll back.
6. `NotificationRetryTests` — delivery configured to fail then succeed (retry + eventual `Sent`), and a
   separate case that exhausts `MaxAttempts` and ends `Failed`.

## API

| Endpoint | Notes |
|---|---|
| `GET /api/products` | current price/stock |
| `POST /api/orders` | requires `Idempotency-Key` header; body `{ customerReference, lines: [{ productCode, quantity }] }` |
| `GET /api/orders/{id}` | order, total, status, notification status |
| `POST /api/orders/{id}/cancel` | cancels a `Confirmed` order, restores stock once |

Error codes returned in the JSON body (`{ code, message }`): `insufficient_stock`, `idempotency_key_conflict`,
`request_in_progress`, `order_not_found`, `unknown_product`, `validation_error`.

## Idempotency-Key: payload equivalence & concurrency behavior

Two requests with the same `Idempotency-Key` are considered equivalent when they have the same
`customerReference` and the same set of `(productCode, quantity)` line pairs, compared **order-insensitively**
(a SHA-256 hash of the normalized/sorted payload is stored and compared — see `IdempotencyPayloadHasher`).

The whole claim → business-logic → finalize sequence runs in **one** DB transaction (see
[docs/design-note.md](docs/design-note.md) for the full rationale). Practical effect for a concurrent duplicate
under SQLite (single-writer engine): the second request's claim insert **blocks** for up to 5s (`Default
Timeout=5` on the connection string) waiting for the first to commit/rollback, then resolves deterministically
— either replaying the first's cached response, or (if the busy-timeout is exceeded) returning `409
request_in_progress`. On a genuinely multi-writer engine (Postgres/SQL Server) the `409 request_in_progress`
path is reachable directly, without blocking.

## Assumptions

- No authentication, real payment integration, message broker, or deployment/Docker setup (explicitly out of
  scope per the assessment).
- Two seed products are sufficient; product catalog management (create/update) is out of scope.
- A cached idempotent replay returns the **stored** response body/status code verbatim (not a live re-fetch of
  current order state), matching the literal behavior described in the assessment.
- `Idempotency-Key` is a client-supplied opaque string (UUID recommended); the Angular client generates one via
  `crypto.randomUUID()`.
- Controllers (not Minimal APIs) and MediatR/CQRS were chosen for the Application layer for testability and
  separation of concerns; this is a design choice, not a hard requirement from the spec.

## Time spent / unfinished work

- Core backend (Domain/Application/Infrastructure/Api), all required automated tests, and the Angular client
  were implemented and manually verified end-to-end (create → view → cancel, with live notification status).
- Not implemented (design-only, as the assessment allows): multi-worker notification claiming, a real payment
  provider integration. Both are covered in [docs/design-note.md](docs/design-note.md).
- Angular unit/e2e tests were not added (manual browser verification only) given the time-boxed scope; the
  backend automated test suite covers all required concurrency/transaction scenarios.

## AI tool disclosure

This solution was developed with GitHub Copilot (agentic mode, Claude Sonnet 4.5) assisting with code
generation, test scaffolding, and this documentation. All code was reviewed for correctness and matches the
design described in `docs/design-note.md`; the author can walk through and explain any part of it.
