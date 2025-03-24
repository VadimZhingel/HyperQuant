using Microsoft.Extensions.DependencyInjection;

namespace HyperQuant.WPF.Register
{
    internal static class RegistratorViews
    {
        public static IServiceCollection RegisterViews(this IServiceCollection services)
        {
            services.AddSingleton<MainWindow>();
            return services;
        }
    }
}
