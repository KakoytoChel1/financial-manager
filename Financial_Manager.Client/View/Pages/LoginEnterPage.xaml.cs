using Microsoft.UI.Xaml.Controls;
using Financial_Manager.Client.ViewModel;
using CommunityToolkit.Mvvm.DependencyInjection;

namespace Financial_Manager.Client.View.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class LoginEnterPage : Page
    {
        public MainViewModel ViewModel { get; }

        public LoginEnterPage()
        {
            this.InitializeComponent();
            ViewModel = Ioc.Default.GetRequiredService<MainViewModel>();
        }
    }
}
