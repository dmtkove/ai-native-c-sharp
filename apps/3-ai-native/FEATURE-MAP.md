# Feature map

| Method | Route | File | Statuses | Tables |
|--------|-------|------|----------|--------|
| POST | `/deposits` | [`src/AiNative.Api/Features/Deposit.cs`](src/AiNative.Api/Features/Deposit.cs) | Success, InvalidAmount, AccountNotFound, UnknownProvider, ProviderDeclined | Customers, Transactions |
| POST | `/withdrawals` | [`src/AiNative.Api/Features/Withdraw.cs`](src/AiNative.Api/Features/Withdraw.cs) | Success, InvalidAmount, AccountNotFound, UnknownProvider, InsufficientFunds, DailyLimitExceeded, ProviderDeclined | Customers, Transactions |
| GET | `/customers/{customerId}/balance` | [`src/AiNative.Api/Features/GetBalance.cs`](src/AiNative.Api/Features/GetBalance.cs) | Success, AccountNotFound | Customers |
| GET | `/customers/{customerId}/daily-withdrawal-limit` | [`src/AiNative.Api/Features/GetDailyWithdrawalLimit.cs`](src/AiNative.Api/Features/GetDailyWithdrawalLimit.cs) | Success, AccountNotFound | Customers |
| PUT | `/customers/{customerId}/daily-withdrawal-limit` | [`src/AiNative.Api/Features/SetDailyWithdrawalLimit.cs`](src/AiNative.Api/Features/SetDailyWithdrawalLimit.cs) | Success, InvalidAmount, AccountNotFound | Customers |

Schema and seed data: [`src/AiNative.Api/Data/AppDbContext.cs`](src/AiNative.Api/Data/AppDbContext.cs)

Composition root (routes + SQLite options): [`src/AiNative.Api/Program.cs`](src/AiNative.Api/Program.cs)

Shared types (`CustomerId`, `Money`, `ProviderId`, `OperationStatus`): [`src/AiNative.Api/Domain/Types.cs`](src/AiNative.Api/Domain/Types.cs)
