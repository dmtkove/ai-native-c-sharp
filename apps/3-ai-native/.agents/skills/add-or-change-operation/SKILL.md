---
name: add-or-change-operation
description: >-
  Adds or changes one operation in the AI-native payment API (one file per
  operation, closed result union, no feature DI). Use when working under
  apps/3-ai-native, copying Features/_Template.cs, mapping a route, or updating
  FEATURE-MAP.md.
---

# Add or change an operation

This app is optimized for changing one operation without loading a layered architecture.

1. Copy `src/AiNative.Api/Features/_Template.cs` to `src/AiNative.Api/Features/<Name>.cs`.
2. Put request, response, closed `Outcome` union, validation, EF calls, dummy-provider rules, and HTTP mapping in **that one file**.
3. Map the route in `src/AiNative.Api/Program.cs` by constructing `new AppDbContext(dbOptions)` in the lambda. Do not register the feature in DI.
4. Add a row to `FEATURE-MAP.md`.
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

Dummy providers decline when `amount % 1 == 0.13`. Seed customers are in repo-root `SPEC.md`.

Behavior rules live in `SPEC.md`. Follow `AGENTS.md` in this app directory.
