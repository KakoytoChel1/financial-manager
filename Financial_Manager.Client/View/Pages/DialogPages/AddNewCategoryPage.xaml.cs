using CommunityToolkit.Mvvm.DependencyInjection;
using Financial_Manager.Client.ViewModel;
using Microsoft.UI.Xaml.Controls;


namespace Financial_Manager.Client.View.Pages.DialogPages
{
    public sealed partial class AddNewCategoryPage : Page
    {
        public MainViewModel ViewModel { get; set; }

        public AddNewCategoryPage()
        {
            this.InitializeComponent();
            ViewModel = Ioc.Default.GetRequiredService<MainViewModel>();
        }
    }
}
