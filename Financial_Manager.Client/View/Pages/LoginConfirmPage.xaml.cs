using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml.Controls;
using Financial_Manager.Client.ViewModel;
using CommunityToolkit.Mvvm.DependencyInjection;

namespace Financial_Manager.Client.View.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class LoginConfirmPage : Page
    {
        public MainViewModel ViewModel { get; }

        public LoginConfirmPage()
        {
            this.InitializeComponent();
            ViewModel = Ioc.Default.GetRequiredService<MainViewModel>();
        }
    }
}
