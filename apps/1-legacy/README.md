# Legacy payment API

Controllers → `IPaymentService` → MediatR handlers → repositories / `IUnitOfWork` → `IPaymentProviderFactory`.

Business errors are domain exceptions mapped to ProblemDetails.

```bash
dotnet run --project src/Legacy.Api --urls http://127.0.0.1:5101
dotnet test Legacy.slnx
```

Listens on http://localhost:5101
