using AccountingTool;
using CommunityToolkit.Mvvm.DependencyInjection;
using Financial_Manager.Client.ViewModel;
using Microsoft.UI.Xaml.Controls;
using System.Linq;

namespace Financial_Manager.Client.View.Pages
{
    public sealed partial class FinancialsPage : Page
    {
        public MainViewModel ViewModel { get; }

        public FinancialsPage()
        {
            this.InitializeComponent();
            ViewModel = Ioc.Default.GetRequiredService<MainViewModel>();
        }

        private void financialsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ViewModel.SelectedFinancialOperations = financialsListView.SelectedItems.Cast<FinancialOperation>().ToList();
        }
    }
}
