using CommunityToolkit.Mvvm.DependencyInjection;
using Financial_Manager.Client.Services;
using Financial_Manager.Client.Services.Interfaces;
using Financial_Manager.Client.View.Pages;
using Financial_Manager.Client.ViewModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using System;
using System.Net;
using System.Net.Http;

namespace Financial_Manager.Client
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
            UnhandledException += OnUnhandledException;
            Ioc.Default.ConfigureServices(ConfigureServices());
        }

        /// <summary>
        /// Invoked when an error occurred.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
        {
            e.Handled = true;
            var viewModel = Ioc.Default.GetRequiredService<MainViewModel>();
            var exception = e.Exception;

            if(exception is HttpRequestException)
            {
                Ioc.Default.GetRequiredService<INavigationService>().ChangePage<ConnectionErrorPage>();
                return;
            }

            viewModel.UnhandledErrorMessage = 
                $"{exception.HResult} - {exception.Source}: " +
                $"{exception.Message}\n" +
                $"{exception.StackTrace}\n" +
                $"{exception.Data}";
            Ioc.Default.GetRequiredService<INavigationService>().ChangePage<ErrorPage>();
        }

        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected async override void OnLaunched(LaunchActivatedEventArgs args)
        {
            Window = new MainWindow();
            Window.Activate();

            Ioc.Default.GetRequiredService<INavigationService>().ChangePage<LoadingPage>();
            var signalRService = Ioc.Default.GetRequiredService<ISignalRService>();

            signalRService.InitializeConnection();
            await signalRService.StartAsync();
        }


        private static IServiceProvider ConfigureServices()
        {
            var services = new ServiceCollection();
            services.AddSingleton<IConfigurationService, ConfigurationService>();
            services.AddSingleton<INavigationService, NavigationService>();
            services.AddSingleton<ISignalRService, SignalRService>();
            services.AddSingleton<DispatcherQueueProvider>();
            services.AddSingleton<MainViewModel>();
            return services.BuildServiceProvider();
        }

        public static Window? Window { get; private set; }
    }
}
