# AI-Native C# — Three Payment APIs

Three isolated .NET 10 Web APIs that implement the same payment behavior (deposits, withdrawals, customer balance) with different internal architectures. The point is to compare **legacy**, **balanced**, and **AI-native** maintainability — including later token consumption and “files touched per change.”

Behavior is defined once in [SPEC.md](SPEC.md). Principles for the AI-oriented apps are in [docs/ai-native-csharp-principles.md](docs/ai-native-csharp-principles.md).

This is a **demo**, not a payments product. There is no authentication, no real money movement, and `stripe` / `paypal` / `bank` are dummy labels with no SDKs or API keys. Do not deploy it.

There are **no shared class libraries**. Each app has its own solution, SQLite file, and tests so measurements stay uncontaminated.

## Apps

| App | Path | Style |
|-----|------|--------|
| 1. Legacy | [apps/1-legacy](apps/1-legacy) | Controllers → services → MediatR → repositories + provider factory. Errors as exceptions. |
| 2. Balanced | [apps/2-balanced](apps/2-balanced) | Minimal APIs → handlers that aggregate modules. EF Core in modules. Provider `switch`. Statuses in the response body. |
| 3. AI-native | [apps/3-ai-native](apps/3-ai-native) | One file per operation, closed result union, strong types, agent maps. No feature DI. |

## Prerequisites

- .NET 10 SDK (see [`global.json`](global.json))

## Run

```bash
dotnet run --project apps/1-legacy/src/Legacy.Api
dotnet run --project apps/2-balanced/src/Balanced.Api
dotnet run --project apps/3-ai-native/src/AiNative.Api
```

Seed customers (see SPEC.md):

- Alice `11111111-1111-1111-1111-111111111111` balance `100.00`
- Bob `22222222-2222-2222-2222-222222222222` balance `25.00`

## Test

```bash
dotnet test apps/1-legacy/Legacy.slnx
dotnet test apps/2-balanced/Balanced.slnx
dotnet test apps/3-ai-native/AiNative.slnx
```

## Out of scope (for now)

Token-measurement harness, shared “add a refund” benchmark, Docker, auth, real payment providers, OpenTelemetry.
