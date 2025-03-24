using HyperQuant.WPF.Register;
using HyperQuant.WPF.ViewModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Windows;

namespace HyperQuant.WPF
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : System.Windows.Application
    {
        private readonly IHost _host;

        public App()
        {
            _host = CreateHostBuilder(Environment.GetCommandLineArgs()).Build();
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            await _host.StartAsync().ConfigureAwait(false);
            var services = _host.Services;

            var mainWindow = services.GetRequiredService<MainWindow>();
            mainWindow.DataContext = services.GetRequiredService<MainViewModel>();
            mainWindow.Show();

            base.OnStartup(e);
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            await _host.StopAsync().ConfigureAwait(false);
            _host.Dispose();

            base.OnExit(e);
        }

        private static IHostBuilder CreateHostBuilder(string[] args) =>
           Host.CreateDefaultBuilder(args)
               .UseContentRoot(Environment.CurrentDirectory)
               .ConfigureServices(ConfigureServices);

        private static void ConfigureServices(HostBuilderContext host, IServiceCollection services)
        {
            services.RegisterViews()
                .RegisterViewModels()
                .RegisterServices()
                .AddSingleton<CancellationTokenSource>()
                ;
        }
    }
}
