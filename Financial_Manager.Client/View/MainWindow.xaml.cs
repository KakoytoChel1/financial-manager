using CommunityToolkit.Mvvm.DependencyInjection;
using Financial_Manager.Client.Services;
using Financial_Manager.Client.ViewModel;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;

namespace Financial_Manager.Client
{
    public sealed partial class MainWindow : Window
    {
        public MainViewModel ViewModel { get; }

        public MainWindow()
        {
            this.InitializeComponent();

            Ioc.Default.GetRequiredService<DispatcherQueueProvider>().Initialize(DispatcherQueue.GetForCurrentThread());
            Ioc.Default.GetRequiredService<INavigationService>().InitializeFrame(this.rootFrame);
            
            ViewModel = Ioc.Default.GetRequiredService<MainViewModel>();

            this.ExtendsContentIntoTitleBar = true;
            this.SetTitleBar(AppTitleBar);
        }
    }
}
