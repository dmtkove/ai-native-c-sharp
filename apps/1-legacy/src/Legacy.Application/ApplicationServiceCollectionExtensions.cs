using Legacy.Application.Abstractions;
using Legacy.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Legacy.Application;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(configuration => configuration.RegisterServicesFromAssembly(typeof(PaymentService).Assembly));
        services.AddScoped<IPaymentService, PaymentService>();
        return services;
    }
}
