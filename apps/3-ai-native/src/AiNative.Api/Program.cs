using System.Text.Json.Serialization;
using AiNative.Api;
using AiNative.Api.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
builder.Services.AddOpenApi();

var connectionString = builder.Configuration.GetConnectionString("Default") ?? "Data Source=ai-native.db";
var dbOptions = new DbContextOptionsBuilder<AppDbContext>()
    .UseSqlite(connectionString)
    .Options;

await using (var startupDb = new AppDbContext(dbOptions))
{
    await DatabaseSeeder.SeedAsync(startupDb);
}

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapPost("/deposits", async (Deposit.Request request) =>
{
    await using var db = new AppDbContext(dbOptions);
    return await Deposit.Handle(request, db, DateTimeOffset.UtcNow);
});

app.MapPost("/withdrawals", async (Withdraw.Request request) =>
{
    await using var db = new AppDbContext(dbOptions);
    return await Withdraw.Handle(request, db, DateTimeOffset.UtcNow);
});

app.MapGet("/customers/{customerId:guid}/balance", async (Guid customerId) =>
{
    await using var db = new AppDbContext(dbOptions);
    return await GetBalance.Handle(customerId, db);
});

app.MapGet("/customers/{customerId:guid}/daily-withdrawal-limit", async (Guid customerId) =>
{
    await using var db = new AppDbContext(dbOptions);
    return await GetDailyWithdrawalLimit.Handle(customerId, db);
});

app.MapPut("/customers/{customerId:guid}/daily-withdrawal-limit", async (Guid customerId, SetDailyWithdrawalLimit.Request request) =>
{
    await using var db = new AppDbContext(dbOptions);
    return await SetDailyWithdrawalLimit.Handle(customerId, request, db);
});

app.Run();

public partial class Program;
