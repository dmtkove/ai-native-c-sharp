using Legacy.Domain.Providers;
using Legacy.Domain.Repositories;
using Legacy.Infrastructure.Persistence;
using Legacy.Infrastructure.Providers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Legacy.Infrastructure;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlite(configuration.GetConnectionString("Default")));

        services.Configure<PaymentProvidersOptions>(configuration.GetSection(PaymentProvidersOptions.SectionName));

        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<IPaymentTransactionRepository, PaymentTransactionRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddSingleton<IPaymentProvider, StripePaymentProvider>();
        services.AddSingleton<IPaymentProvider, PayPalPaymentProvider>();
        services.AddSingleton<IPaymentProvider, BankPaymentProvider>();
        services.AddSingleton<IPaymentProviderFactory, PaymentProviderFactory>();

        return services;
    }
}
