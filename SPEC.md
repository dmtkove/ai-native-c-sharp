# Payment API Behavior Spec

Identical product behavior for all three apps. Error *representation* differs by app; the rules below do not.

## Seed customers

Created on startup via `EnsureCreated` + seed. IDs are stable so tests and demos can hard-code them.

| Name  | CustomerId                             | Opening balance |
|-------|----------------------------------------|-----------------|
| Alice | `11111111-1111-1111-1111-111111111111` | `100.00`        |
| Bob   | `22222222-2222-2222-2222-222222222222` | `25.00`         |

## Operations

| Method | Path                                                | Body fields                                      |
|--------|-----------------------------------------------------|--------------------------------------------------|
| POST   | `/deposits`                                         | `customerId` (guid), `amount` (decimal), `provider` (`stripe` \| `paypal` \| `bank`) |
| POST   | `/withdrawals`                                      | `customerId` (guid), `amount` (decimal), `provider` (`stripe` \| `paypal` \| `bank`) |
| GET    | `/customers/{customerId}/balance`                   | —                                                |
| GET    | `/customers/{customerId}/daily-withdrawal-limit`    | —                                                |
| PUT    | `/customers/{customerId}/daily-withdrawal-limit`    | `amount` (decimal, must be > 0)                  |

Amounts are USD implied. There is no authentication and no customer CRUD.

`GET`/`PUT` daily-withdrawal-limit return `customerId` and `dailyWithdrawalLimit`. Seed customers start with **no** daily limit (`null` = unlimited). Changing the limit is another `PUT` with a new positive amount. The daily window is the UTC calendar day.

## Domain rules

| Condition                                         | Outcome             | Ledger effect                         |
|---------------------------------------------------|---------------------|---------------------------------------|
| `customerId` is not a seeded/existing customer    | `AccountNotFound`   | none                                  |
| `amount` ≤ 0                                      | `InvalidAmount`     | none                                  |
| `provider` is not `stripe`, `paypal`, or `bank`   | `UnknownProvider`   | none                                  |
| Withdrawal and `balance < amount`                 | `InsufficientFunds` | none; provider is **not** called      |
| Withdrawal, a daily limit is set, and today's successful withdrawals + `amount` would exceed the limit | `DailyLimitExceeded` | none; provider is **not** called |
| `PUT` daily-withdrawal-limit and `amount` ≤ 0     | `InvalidAmount`     | none                                  |
| Fractional part of `amount` equals `0.13`         | `ProviderDeclined`  | none (dummy provider decline)         |
| Otherwise                                         | `Success`           | insert transaction, update balance    |

Provider decline uses `amount % 1 == 0.13m` (example: `10.13`). Dummy providers do not use the network. Decline is evaluated only after the customer exists, the amount is valid, the provider is known, and (for withdrawals) funds are sufficient and the daily limit (when set) would not be exceeded.

"Today's withdrawals" is the sum of persisted `Withdrawal` rows for that customer whose `CreatedAt` falls on the current UTC calendar day. The daily-limit check runs after `InsufficientFunds` and before the dummy provider is called.

On success:

- persist a transaction row (`Deposits` type `Deposit`, `Withdrawals` type `Withdrawal`)
- update the customer balance (credit on deposit, debit on withdrawal)
- return `customerId`, new `balance`, and `transactionId`

## Schema

```
Customers(Id, Name, Balance, DailyWithdrawalLimit)
Transactions(Id, CustomerId, Type, Amount, Provider, CreatedAt, ExternalReference)
```

Each app uses its own SQLite file. No shared database.

## Error representation (intentionally different)

- **App 1 (legacy):** domain exceptions mapped to ProblemDetails. `AccountNotFound` → 404; `InvalidAmount` / `UnknownProvider` → 400; `InsufficientFunds` / `ProviderDeclined` / `DailyLimitExceeded` → 422. Success → 200 with `customerId`, `balance`, `transactionId` (payments) or `customerId`, `dailyWithdrawalLimit` (limit get/set).
- **App 2 and App 3:** HTTP 200 for every handled business outcome. Body includes `status` plus `customerId` / `balance` / `transactionId` / `dailyWithdrawalLimit` / `errorMessage` as applicable. HTTP 400 only for malformed JSON.

## Test scenarios

1. Deposit success updates balance and writes a transaction
2. Withdraw success
3. Withdraw insufficient funds (no write, provider not charged)
4. Unknown customer
5. Invalid amount
6. Unknown provider
7. Provider decline on `*.13` (no write)
8. Get balance
9. Set daily withdrawal limit, then change it
10. Withdraw within remaining daily limit
11. Withdraw that would exceed the daily limit (no write, provider not charged)
