# Balanced payment API

Minimal APIs → feature handlers that aggregate account/transaction modules → EF Core in the modules → provider `switch`.

Business errors are `status` values on HTTP 200.

```bash
dotnet run --project src/Balanced.Api --urls http://127.0.0.1:5102
dotnet test Balanced.slnx
```

Listens on http://localhost:5102
