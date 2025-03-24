using HyperQuant.WPF.ViewModel;
using Microsoft.Extensions.DependencyInjection;

namespace HyperQuant.WPF.Register
{
    internal static class RegistratorViewModels
    {
        public static IServiceCollection RegisterViewModels(this IServiceCollection services)
        {
            services.AddSingleton<MainViewModel>();
            return services;
        }
    }
}
