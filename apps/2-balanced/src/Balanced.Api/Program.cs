using System.Text.Json.Serialization;
using Balanced.Api.Data;
using Balanced.Api.Features;
using Balanced.Api.Features.DailyWithdrawalLimit;
using Balanced.Api.Features.Deposit;
using Balanced.Api.Features.GetBalance;
using Balanced.Api.Features.Withdraw;
using Balanced.Api.Modules.Accounts;
using Balanced.Api.Modules.Transactions;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddOpenApi();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Default") ?? "Data Source=balanced.db"));

builder.Services.AddScoped<AccountModule>();
builder.Services.AddScoped<TransactionModule>();
builder.Services.AddScoped<DepositHandler>();
builder.Services.AddScoped<WithdrawHandler>();
builder.Services.AddScoped<GetBalanceHandler>();
builder.Services.AddScoped<GetDailyWithdrawalLimitHandler>();
builder.Services.AddScoped<SetDailyWithdrawalLimitHandler>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await DatabaseSeeder.SeedAsync(db);
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapPost("/deposits", async (PaymentRequest request, DepositHandler handler, CancellationToken cancellationToken) =>
    Results.Ok(await handler.Handle(request, cancellationToken)));

app.MapPost("/withdrawals", async (PaymentRequest request, WithdrawHandler handler, CancellationToken cancellationToken) =>
    Results.Ok(await handler.Handle(request, cancellationToken)));

app.MapGet("/customers/{customerId:guid}/balance", async (Guid customerId, GetBalanceHandler handler, CancellationToken cancellationToken) =>
    Results.Ok(await handler.Handle(customerId, cancellationToken)));

app.MapGet("/customers/{customerId:guid}/daily-withdrawal-limit", async (Guid customerId, GetDailyWithdrawalLimitHandler handler, CancellationToken cancellationToken) =>
    Results.Ok(await handler.Handle(customerId, cancellationToken)));

app.MapPut("/customers/{customerId:guid}/daily-withdrawal-limit", async (Guid customerId, SetDailyWithdrawalLimitRequest request, SetDailyWithdrawalLimitHandler handler, CancellationToken cancellationToken) =>
    Results.Ok(await handler.Handle(customerId, request, cancellationToken)));

app.Run();

public partial class Program;
