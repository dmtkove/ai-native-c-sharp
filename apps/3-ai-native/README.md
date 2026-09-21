# AI-native payment API

One file per operation. Closed result unions. Strong types. No feature DI.

Read [AGENTS.md](AGENTS.md) and [FEATURE-MAP.md](FEATURE-MAP.md) before changing anything.

```bash
dotnet run --project src/AiNative.Api --urls http://127.0.0.1:5103
dotnet test AiNative.slnx
```

Listens on http://localhost:5103
