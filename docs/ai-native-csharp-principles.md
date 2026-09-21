# AI-Native C# Engineering Principles

A set of rules for writing C#/.NET code that is cheap for an AI agent to understand, modify, test, and run.

---

## The 10 Core Rules

| Rule | Principle |
|------|-----------|
| 1 | **Boring beats clever** |
| 2 | **Explicit beats implicit** |
| 3 | **Local reasoning beats global knowledge** |
| 4 | **Few abstractions beats abstraction everywhere** |
| 5 | **Consistent patterns beat flexibility** |
| 6 | **Immutable state beats hidden mutation** |
| 7 | **Strong types beat primitive obsession** |
| 8 | **Tests are executable documentation** |
| 9 | **Measured optimization beats premature optimization** |
| 10 | **Minimize the context required to understand a change** |

> **Overarching metric:** How many files, concepts, and dependencies does an AI agent need to understand before it can safely change this code?

---

## Rule 1 — Boring beats clever

If two implementations are equivalent, choose the one an AI can understand immediately.

```csharp
// Prefer
if (user.IsActive)
    return Process(user);

return Reject(user);
```

Avoid clever expressions, elaborate patterns, or unnecessary abstractions.

---

## Rule 2 — Keep functions small, but don't over-fragment

A function should ideally answer **one question**.

```csharp
public async Task<Result> ProcessPayment(Payment payment)
{
    Validate(payment);

    var account = await GetAccount(payment.AccountId);

    if (!account.HasFunds(payment.Amount))
        return Result.InsufficientFunds();

    account.Debit(payment.Amount);

    await Save(account);

    return Result.Success();
}
```

Don't turn this into 17 one-line methods just to satisfy a "method must be 10 lines" rule.

> A method should contain enough context that an AI can understand its purpose without jumping through many files.

---

## Rule 3 — Minimize abstraction

Avoid unnecessary abstraction chains:

```
IPaymentProcessor → PaymentProcessor → PaymentProcessorDecorator
  → PaymentProcessorFactory → PaymentProcessorFactoryProvider
```

when this would do:

```
PaymentService
```

Use interfaces only when they provide a genuine architectural benefit:

- External dependencies
- Infrastructure boundaries
- Multiple implementations
- Testing seams where needed
- Plugin/provider architectures

> Every abstraction must justify its existence.

---

## Rule 4 — Prefer explicit dependencies

```csharp
// Good
public class PaymentService(
    PaymentRepository repository,
    PaymentValidator validator,
    PaymentGateway gateway)

// Less AI-friendly
public class PaymentService(IServiceProvider services)
```

An AI should be able to look at the constructor and immediately understand what the component depends on.

> Dependencies should be visible at the point of construction.

---

## Rule 5 — Keep dependency graphs shallow

Prefer:

```
API → Service → Repository → Database
```

over:

```
API → Mediator → Handler → Facade → Manager → Strategy
  → Factory → Repository → UnitOfWork → Database
```

Every additional hop increases the amount of context an AI needs to retrieve.

> Minimize the number of files an agent must inspect to understand one operation.

---

## Rule 6 — Prefer local reasoning

A developer or AI should be able to answer "What happens when a withdrawal is created?" by looking at:

```
Withdrawals/
    CreateWithdrawal.cs
    Withdrawal.cs
    WithdrawalRepository.cs
```

rather than searching through 50 projects. **Vertical slices** work extremely well for this in .NET.

> Keep related behavior physically close together.

---

## Rule 7 — Avoid "magic"

Prefer explicit values over opaque configuration references where possible:

```csharp
if (transaction.Amount > 10_000)
```

Use named parameters to make call sites self-documenting:

```csharp
// Avoid
Process(transaction, true, false, 3);

// Prefer
Process(
    transaction,
    validateBalance: true,
    publishEvent: false,
    retryCount: 3);
```

> Meaning should be visible in the code.

---

## Rule 8 — Use strong domain types

```csharp
// Instead of
public Task Transfer(string accountId, decimal amount, string currency)

// Prefer
public Task Transfer(AccountId accountId, Money amount)
```

```csharp
public readonly record struct AccountId(Guid Value);

public readonly record struct Money(decimal Amount, Currency Currency);
```

This reduces the number of invalid states an AI has to reason about.

> Make invalid states difficult to represent.

---

## Rule 9 — Prefer immutable data

```csharp
public sealed record PaymentCreated(
    PaymentId PaymentId,
    AccountId AccountId,
    Money Amount,
    DateTimeOffset CreatedAt);
```

is easier to reason about than a mutable object with 15 setters.

> Prefer immutable state and explicit state transitions.

---

## Rule 10 — Make state transitions explicit

```csharp
// Avoid
payment.Status = PaymentStatus.Processed;
payment.ProcessedAt = DateTime.UtcNow;
payment.Error = null;

// Prefer
payment.MarkProcessed(clock.UtcNow);
```

Or for simple systems:

```csharp
payment = payment with
{
    Status = PaymentStatus.Processed,
    ProcessedAt = clock.UtcNow
};
```

The important thing is that the transition is obvious.

---

## Rule 11 — Don't hide important behavior in framework magic

.NET provides powerful mechanisms (middleware, filters, source generators, reflection, DI conventions, EF interceptors, etc.) that are useful but each hidden behavior increases the context required by an AI.

> Use framework magic at the edges; keep business logic explicit.

---

## Rule 12 — One obvious way to do things

Don't allow multiple data access patterns (EF Core, Dapper, Repository, stored procedures, raw SQL) without strong reason. Establish conventions:

```
Queries  → Dapper
Commands → EF Core
```

An AI won't have to discover your preferred approach every time.

> Consistency beats theoretical flexibility.

---

## Rule 13 — Keep classes cohesive

Avoid a single `PaymentService` containing `CreatePayment()`, `CancelPayment()`, `RefundPayment()`, `ValidateFees()`, `SendEmail()`, `WriteAuditLog()`, `GenerateReport()`, `ExportCsv()`, etc.

Split by actual business responsibility. But also avoid creating a class for every verb.

> One business concept / responsibility per component.

---

## Rule 14 — Keep configuration close to its usage

Instead of a giant `appsettings.json`, use strongly typed options:

```csharp
public sealed class PaymentOptions
{
    public int TimeoutSeconds { get; init; }
    public int MaxRetries { get; init; }
}
```

and make the dependency explicit.

---

## Rule 15 — Favor deterministic code

Prefer:

```csharp
public Money CalculateFee(Money amount)
```

over functions whose output depends on global state, current time, environment variables, static caches, or hidden database calls. When external state is necessary, make it explicit:

```csharp
public Money CalculateFee(
    Money amount,
    DateTimeOffset now,
    FeeConfiguration configuration)
```

---

## Rule 16 — Make errors explicit

```csharp
// Prefer
return Result.NotFound();

// Avoid
catch (Exception)
{
    return null;
}
```

Don't use exceptions for normal business flow. Define a clear set of outcomes an AI can reason about:

```
Success | InsufficientFunds | AccountNotFound | AccountLocked
```

---

## Rule 17 — Tests should describe behavior

```csharp
// Avoid
PaymentService_ShouldCallRepository()

// Prefer
When_account_has_insufficient_funds_payment_is_rejected()
```

The test suite becomes an executable specification that an AI can use to understand the system.

> Tests are executable documentation.

---

## Rule 18 — Keep comments for "why", not "what"

```csharp
// Bad
// Increment retry count
retryCount++;

// Good
// Provider occasionally returns 202 before the transaction is actually persisted.
// Retry only for this response.
retryCount++;
```

> Code explains what; comments explain why.

---

## Rule 19 — Avoid premature performance optimization

Don't reach for `unsafe`, `Span<byte>`, `ArrayPool`, custom allocators, or lock-free queues everywhere just because they're faster.

```
Simple implementation → Measure → Identify bottleneck → Optimize only that boundary
```

> Complexity must buy measurable performance.

---

## Rule 20 — Make performance characteristics obvious

Avoid APIs that secretly perform deep eager loads:

```csharp
// Hidden cost: loads Customer + Accounts + Transactions + Bonuses + KYC + ...
var customer = await repository.GetCustomer(id);
```

Prefer APIs whose cost is obvious:

```csharp
GetCustomer(id)
GetCustomerBalance(id)
GetRecentTransactions(id, 20)
```

> The computational and I/O cost of an operation should be predictable from its API.

---

## AI Maintainability Index

An **AI Maintainability Index** could be defined around:

- Dependency depth
- Number of abstractions
- Files touched per feature
- Cyclomatic complexity
- Test coverage
- Token/context requirements per change

For large .NET transactional systems, this becomes a practical engineering standard rather than just a philosophy.
