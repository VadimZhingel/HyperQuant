using HyperQuant.Application;
using HyperQuant.Domain.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace HyperQuant.WPF.Register
{
    internal static class RegistratorServices
    {
        public static IServiceCollection RegisterServices(this IServiceCollection services)
        {
            services.AddSingleton<ITestConnector, TestConnector>()
                .AddSingleton<CryptoBalanceCalculator>()
                ;

            return services;
        }
    }
}
