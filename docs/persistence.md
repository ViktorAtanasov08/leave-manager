# Persistence walkthrough

## Submit a request

1. LeaveController takes employee identity from the authenticated cookie, never a posted employee ID.
2. LeaveBook opens a SQLite connection and an IMMEDIATE transaction.
3. It reads saved requests, checks overlap and allowance, then inserts the new request.
4. Commit makes the write durable. Returning a validation error disposes and rolls back the transaction.

The early write transaction matters: two separate app instances must not both read the same available balance and approve spending it. An in-process lock alone cannot protect two processes.

## Make a decision

The manager-only controller accepts an approve/reject POST with an anti-forgery token. The SQL update includes WHERE Status=0. Only a pending row can change, so two simultaneous decisions cannot both succeed. An unknown or already-decided ID returns HTTP 409.

## Balances and limitations

Approved and pending days are calculated from saved rows, not a second mutable balance column. Rejected days no longer reserve allowance. Employee pages filter rows by the signed identity. The current small demo reads the request list in memory after fetching it; production-sized datasets would need scoped SQL queries and pagination.

Demo identities remain fixed in code. There is no registration, password recovery, production identity store or complete real-world leave policy. SQLite stores requests, not accounts. Initialization uses CREATE TABLE IF NOT EXISTS, not a migration framework.

## Independent follow-up

Add employee cancellation for pending requests only. Think through ownership, statuses, releasing reserved days, CSRF and a cancellation racing with approval. Write failing tests before changing the implementation. This exercise is not completed by the current update.
