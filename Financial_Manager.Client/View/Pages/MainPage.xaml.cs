using Microsoft.UI.Xaml.Controls;
using Financial_Manager.Client.ViewModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml.Media.Animation;
using Financial_Manager.Client.Services;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace Financial_Manager.Client.View.Pages
{
    public sealed partial class MainPage : Page
    {
        public MainViewModel ViewModel { get; }
        private IConfigurationService _configurationService;

        public MainPage()
        {
            this.InitializeComponent();
            contentFrame.SourcePageType = typeof(ProfilePage);

            ViewModel = Ioc.Default.GetRequiredService<MainViewModel>();
            _configurationService = Ioc.Default.GetRequiredService<IConfigurationService>();
        }

        private void Page_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            var settings = _configurationService.GetConfigurationSettings();

            if(settings != null)
            {
                NavigateProvidedPage(settings.CurrentPageName ?? "Profile");
            }
        }

        private void SaveCurrentPageToConfiguration(string pageName)
        {
            var settings = _configurationService.GetConfigurationSettings();
            
            if(settings != null)
            {
                settings.CurrentPageName = pageName;
                _configurationService.SaveConfigurationSettings(settings);
            }
        }

        private void NavigateProvidedPage(string pageTag)
        {
            switch (pageTag)
            {
                case "Profile":
                    contentFrame.Navigate(typeof(ProfilePage), null, new SlideNavigationTransitionInfo() { Effect = SlideNavigationTransitionEffect.FromRight });
                    break;
                case "Financials":
                    contentFrame.Navigate(typeof(FinancialsPage), null, new SlideNavigationTransitionInfo() { Effect = SlideNavigationTransitionEffect.FromRight });
                    break;
                case "Categories":
                    contentFrame.Navigate(typeof(CategoriesPage), null, new SlideNavigationTransitionInfo() { Effect = SlideNavigationTransitionEffect.FromRight });
                    break;
                case "Settings":
                    contentFrame.Navigate(typeof(SettingsPage), null, new SlideNavigationTransitionInfo() { Effect = SlideNavigationTransitionEffect.FromRight });
                    break;
            }
        }

        private void MainNavigationView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            var selectedItem = args.SelectedItem as NavigationViewItem;

            if (selectedItem != null)
            {
                NavigateProvidedPage(selectedItem.Tag.ToString()!);

                SaveCurrentPageToConfiguration(selectedItem.Tag.ToString()!);
            }
        }
    }
}
