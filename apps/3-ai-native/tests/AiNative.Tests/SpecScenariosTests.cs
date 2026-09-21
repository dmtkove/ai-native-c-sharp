using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using AiNative.Api;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace AiNative.Tests;

public sealed class AiNativeApiFactory : WebApplicationFactory<Program>
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"ai-native-{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:Default", $"Data Source={_dbPath}");
        builder.UseEnvironment("Development");
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        try
        {
            File.Delete(_dbPath);
        }
        catch (IOException)
        {
        }
    }
}

public sealed class DepositTests
{
    private static readonly JsonSerializerOptions Json = Serializer();

    [Fact]
    public async Task When_deposit_succeeds_balance_is_updated_and_transaction_is_written()
    {
        var body = await Post<Deposit.Response>("/deposits", SeedCustomers.AliceId, 10.00m, "stripe");
        Assert.Equal(OperationStatus.Success, body.Status);
        Assert.Equal(110.00m, body.Balance);
        Assert.NotEqual(Guid.Empty, body.TransactionId);
    }

    [Fact]
    public async Task When_customer_is_unknown_operation_returns_AccountNotFound()
    {
        var body = await Post<Deposit.Response>(
            "/deposits",
            Guid.Parse("33333333-3333-3333-3333-333333333333"),
            10.00m,
            "stripe");
        Assert.Equal(OperationStatus.AccountNotFound, body.Status);
    }

    [Fact]
    public async Task When_amount_is_invalid_operation_returns_InvalidAmount()
    {
        var body = await Post<Deposit.Response>("/deposits", SeedCustomers.AliceId, 0m, "stripe");
        Assert.Equal(OperationStatus.InvalidAmount, body.Status);
    }

    [Fact]
    public async Task When_provider_is_unknown_operation_returns_UnknownProvider()
    {
        var body = await Post<Deposit.Response>("/deposits", SeedCustomers.AliceId, 10.00m, "bitcoin");
        Assert.Equal(OperationStatus.UnknownProvider, body.Status);
    }

    [Fact]
    public async Task When_provider_declines_star13_amount_ledger_is_unchanged()
    {
        var body = await Post<Deposit.Response>("/deposits", SeedCustomers.AliceId, 10.13m, "paypal");
        Assert.Equal(OperationStatus.ProviderDeclined, body.Status);
        Assert.Equal(100.00m, body.Balance);
        Assert.Null(body.TransactionId);
    }

    internal static async Task<T> Post<T>(string path, Guid customerId, decimal amount, string provider)
    {
        await using var factory = new AiNativeApiFactory();
        using var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync(path, new { customerId, amount, provider });
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<T>(Json);
        Assert.NotNull(body);
        return body;
    }

    internal static JsonSerializerOptions Serializer() => new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };
}

public sealed class WithdrawTests
{
    [Fact]
    public async Task When_withdraw_succeeds_balance_is_updated()
    {
        var body = await DepositTests.Post<Withdraw.Response>("/withdrawals", SeedCustomers.AliceId, 40.00m, "bank");
        Assert.Equal(OperationStatus.Success, body.Status);
        Assert.Equal(60.00m, body.Balance);
    }

    [Fact]
    public async Task When_account_has_insufficient_funds_withdrawal_is_rejected()
    {
        var body = await DepositTests.Post<Withdraw.Response>("/withdrawals", SeedCustomers.BobId, 40.00m, "stripe");
        Assert.Equal(OperationStatus.InsufficientFunds, body.Status);
        Assert.Equal(25.00m, body.Balance);
        Assert.Null(body.TransactionId);
    }

    [Fact]
    public async Task When_daily_limit_is_set_and_withdrawal_is_within_remaining_limit_it_succeeds()
    {
        await using var factory = new AiNativeApiFactory();
        using var client = factory.CreateClient();

        var setResponse = await client.PutAsJsonAsync(
            $"/customers/{SeedCustomers.AliceId}/daily-withdrawal-limit",
            new { amount = 50.00m });
        setResponse.EnsureSuccessStatusCode();

        var withdrawResponse = await client.PostAsJsonAsync(
            "/withdrawals",
            new { customerId = SeedCustomers.AliceId, amount = 40.00m, provider = "bank" });
        withdrawResponse.EnsureSuccessStatusCode();
        var body = await withdrawResponse.Content.ReadFromJsonAsync<Withdraw.Response>(DepositTests.Serializer());
        Assert.NotNull(body);
        Assert.Equal(OperationStatus.Success, body.Status);
        Assert.Equal(60.00m, body.Balance);
    }

    [Fact]
    public async Task When_withdrawal_would_exceed_daily_limit_it_is_rejected()
    {
        await using var factory = new AiNativeApiFactory();
        using var client = factory.CreateClient();

        var setResponse = await client.PutAsJsonAsync(
            $"/customers/{SeedCustomers.AliceId}/daily-withdrawal-limit",
            new { amount = 50.00m });
        setResponse.EnsureSuccessStatusCode();

        var first = await client.PostAsJsonAsync(
            "/withdrawals",
            new { customerId = SeedCustomers.AliceId, amount = 40.00m, provider = "bank" });
        first.EnsureSuccessStatusCode();

        var second = await client.PostAsJsonAsync(
            "/withdrawals",
            new { customerId = SeedCustomers.AliceId, amount = 20.00m, provider = "bank" });
        second.EnsureSuccessStatusCode();
        var body = await second.Content.ReadFromJsonAsync<Withdraw.Response>(DepositTests.Serializer());
        Assert.NotNull(body);
        Assert.Equal(OperationStatus.DailyLimitExceeded, body.Status);
        Assert.Equal(60.00m, body.Balance);
        Assert.Null(body.TransactionId);
    }
}

public sealed class GetBalanceTests
{
    [Fact]
    public async Task When_getting_balance_for_seeded_customer_current_balance_is_returned()
    {
        await using var factory = new AiNativeApiFactory();
        using var client = factory.CreateClient();
        var response = await client.GetAsync($"/customers/{SeedCustomers.AliceId}/balance");
        var body = await response.Content.ReadFromJsonAsync<GetBalance.Response>(DepositTests.Serializer());
        Assert.NotNull(body);
        Assert.Equal(OperationStatus.Success, body.Status);
        Assert.Equal(100.00m, body.Balance);
    }
}

public sealed class DailyWithdrawalLimitTests
{
    [Fact]
    public async Task When_setting_and_changing_daily_withdrawal_limit_the_new_amount_is_returned()
    {
        await using var factory = new AiNativeApiFactory();
        using var client = factory.CreateClient();

        var unset = await client.GetFromJsonAsync<GetDailyWithdrawalLimit.Response>(
            $"/customers/{SeedCustomers.AliceId}/daily-withdrawal-limit",
            DepositTests.Serializer());
        Assert.NotNull(unset);
        Assert.Equal(OperationStatus.Success, unset.Status);
        Assert.Null(unset.DailyWithdrawalLimit);

        var setResponse = await client.PutAsJsonAsync(
            $"/customers/{SeedCustomers.AliceId}/daily-withdrawal-limit",
            new { amount = 50.00m });
        setResponse.EnsureSuccessStatusCode();
        var set = await setResponse.Content.ReadFromJsonAsync<SetDailyWithdrawalLimit.Response>(DepositTests.Serializer());
        Assert.NotNull(set);
        Assert.Equal(OperationStatus.Success, set.Status);
        Assert.Equal(50.00m, set.DailyWithdrawalLimit);

        var changeResponse = await client.PutAsJsonAsync(
            $"/customers/{SeedCustomers.AliceId}/daily-withdrawal-limit",
            new { amount = 75.00m });
        changeResponse.EnsureSuccessStatusCode();
        var changed = await changeResponse.Content.ReadFromJsonAsync<SetDailyWithdrawalLimit.Response>(DepositTests.Serializer());
        Assert.NotNull(changed);
        Assert.Equal(75.00m, changed.DailyWithdrawalLimit);
    }

    [Fact]
    public async Task When_set_limit_amount_is_invalid_operation_returns_InvalidAmount()
    {
        await using var factory = new AiNativeApiFactory();
        using var client = factory.CreateClient();
        var response = await client.PutAsJsonAsync(
            $"/customers/{SeedCustomers.AliceId}/daily-withdrawal-limit",
            new { amount = 0m });
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<SetDailyWithdrawalLimit.Response>(DepositTests.Serializer());
        Assert.NotNull(body);
        Assert.Equal(OperationStatus.InvalidAmount, body.Status);
    }
}
