using CommunityToolkit.Mvvm.DependencyInjection;
using Financial_Manager.Client.ViewModel;
using Microsoft.UI.Xaml.Controls;

namespace Financial_Manager.Client.View.Pages
{
    public sealed partial class SettingsPage : Page
    {
        public MainViewModel ViewModel { get; }

        public SettingsPage()
        {
            this.InitializeComponent();
            ViewModel = Ioc.Default.GetRequiredService<MainViewModel>();
        }
    }
}
