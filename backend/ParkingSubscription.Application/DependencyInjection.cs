using Microsoft.Extensions.DependencyInjection;
using ParkingSubscription.Application.Auth;
using ParkingSubscription.Application.Common;
using ParkingSubscription.Application.Facade;
using ParkingSubscription.Application.Payments;
using ParkingSubscription.Application.Wallet;

namespace ParkingSubscription.Application;

public static class DependencyInjection
{
    /// <summary>Registers application services (business logic). Ports are provided by Infrastructure.</summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ChangePropagator>();

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IPlanService, PlanService>();
        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IParkingCardService, ParkingCardService>();
        services.AddScoped<IValueCardService, ValueCardService>();
        services.AddScoped<IPaymentService, PaymentService>();
        services.AddScoped<IWalletAppService, WalletAppService>();

        return services;
    }
}
