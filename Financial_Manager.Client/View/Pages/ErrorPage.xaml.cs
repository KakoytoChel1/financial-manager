using CommunityToolkit.Mvvm.DependencyInjection;
using Financial_Manager.Client.ViewModel;
using Microsoft.UI.Xaml.Controls;

namespace Financial_Manager.Client.View.Pages
{
    public sealed partial class ErrorPage : Page
    {
        public MainViewModel ViewModel { get; }

        public ErrorPage()
        {
            this.InitializeComponent();
            ViewModel = Ioc.Default.GetRequiredService<MainViewModel>();
        }
    }
}
