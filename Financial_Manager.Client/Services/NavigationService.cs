using Microsoft.UI.Xaml.Controls;
using System;

namespace Financial_Manager.Client.Services
{
    public class NavigationService : INavigationService
    {
        private Frame? _frame { get; set; }

        public void InitializeFrame(Frame? frame)
        {
            _frame = frame ?? throw new ArgumentNullException("Provided instance of the Frame is null.");
        }

        public void ChangePage<TPage>() where TPage : Page
        {
            if (_frame == null)
                throw new ArgumentNullException("The instance of the root frame is null.");
            else
                _frame.Navigate(typeof(TPage));
        }

        public void ChangePage<TPage>(Frame? frame) where TPage : Page
        {
            if(frame != null)
            {
                frame.Navigate(typeof(TPage));
            }
        }
    }
}
