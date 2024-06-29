using AccountingTool;
using CommunityToolkit.Mvvm.DependencyInjection;
using Financial_Manager.Client.ViewModel;
using Microsoft.UI.Xaml.Controls;
using System.Linq;

namespace Financial_Manager.Client.View.Pages
{
    public sealed partial class CategoriesPage : Page
    {
        public MainViewModel ViewModel { get; }

        public CategoriesPage()
        {
            this.InitializeComponent();
            ViewModel = Ioc.Default.GetRequiredService<MainViewModel>();
        }

        private void categoryListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ViewModel.SelectedOperationCategories = categoryListView.SelectedItems.Cast<OperationCategory>().ToList();
        }
    }
}
