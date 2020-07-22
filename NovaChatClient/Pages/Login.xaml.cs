using NovaChatClient.Pages;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using static NovaChatClient.Globals;
using Windows.System;
using System.Net;
using ChatLib.Security;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace NovaChatClient
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
        }

        private void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            Username = UsernameInput.Text;
            AdminPassword = AdminPasswordInput.Password.StringToSecure();
            AdminPasswordInput.Password = "";

            string[] addrSplit = IPInput.Text.Split(':');
            if (addrSplit.Length == 2)
            {
                Address = IPInput.Text;
                if(int.TryParse(addrSplit[1], out int port))
                {
                    Port = port;
                }
                else
                {
                    Port = 8910;
                }
            }
            else
            {
                Address = IPInput.Text;
                Port = 8910;
            }

            Frame.Navigate(typeof(ChatUI));
        }

        private void UseAdminPasswordCheckbox_Click(object sender, RoutedEventArgs e)
        {
            CheckBox checkBox = (CheckBox)sender;

            if (checkBox.IsChecked == true)
            {
                AdminPasswordInput.Visibility = Visibility.Visible;
            }
            else
            {
                AdminPasswordInput.Visibility = Visibility.Collapsed;
            }
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
           Frame.Navigate(typeof(Settings));
        }

        private void UsernameInput_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter)
            {
                IPInput.Focus(FocusState.Keyboard);
            }
        }

        private void IPInput_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter)
            {
                if (UseAdminPasswordCheckbox.IsChecked == true)
                {
                    AdminPasswordInput.Focus(FocusState.Keyboard);
                }
                else
                {
                    ConnectButton_Click(sender, e);
                }
            }
        }

        private void AdminPasswordInput_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter)
            {
                ConnectButton_Click(sender, e);
            }
        }
    }
}
