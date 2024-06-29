using Microsoft.UI.Xaml.Controls;

namespace Financial_Manager.Client.Services
{
    public interface INavigationService
    {
        void InitializeFrame(Frame? frame);
        void ChangePage<TPage>() where TPage : Page;

        void ChangePage<TPage>(Frame? frame) where TPage : Page;
    }
}
