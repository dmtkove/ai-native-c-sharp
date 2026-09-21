# Agent notes for the AI-native payment API

This app is optimized for an AI agent to change one operation without loading a layered architecture. Portable skills live in [`.agents/skills/`](.agents/skills/).

## One way to add an operation

1. Copy [`src/AiNative.Api/Features/_Template.cs`](src/AiNative.Api/Features/_Template.cs) to `src/AiNative.Api/Features/<Name>.cs`.
2. Put request, response, closed `Outcome` union, validation, EF calls, dummy-provider rules, and HTTP mapping in **that one file**.
3. Map the route in [`src/AiNative.Api/Program.cs`](src/AiNative.Api/Program.cs) by constructing `new AppDbContext(dbOptions)` in the lambda. Do not register the feature in DI.
4. Add a row to [`FEATURE-MAP.md`](FEATURE-MAP.md).
5. Add a behavior test named `When_<condition>_<result>` in `tests/AiNative.Tests/`.

## Do not add

- MediatR, AutoMapper, FluentValidation
- Repository / unit-of-work / factory interfaces
- Constructor DI on features
- Shared validation helpers (copy the few lines instead)
- Exception flow for business outcomes

## Error model

Every handled outcome returns HTTP 200 with `status`:

`Success | AccountNotFound | InsufficientFunds | InvalidAmount | UnknownProvider | ProviderDeclined | DailyLimitExceeded`

Dummy providers decline when `amount % 1 == 0.13`. Seed customers are in `SPEC.md` at the repo root.

Behavior rules live in [`../../SPEC.md`](../../SPEC.md). Engineering principles live in [`../../docs/ai-native-csharp-principles.md`](../../docs/ai-native-csharp-principles.md).
