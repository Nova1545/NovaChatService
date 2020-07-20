using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.System;
using static NovaChatClient.Globals;
using Windows.UI.Core;

namespace NovaChatClient.Pages
{
    public sealed partial class ChatUI : Page
    {
        public ChatUI()
        {
            this.InitializeComponent();
        }

        private void SendMessage()
        {
            ChatField.Items.Add(new FormattedMessage(Username, MessageInput.Text, DateTime.Now));
            MessageInput.Text = "";
        }

        private void MessageInput_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (CoreWindow.GetForCurrentThread().GetKeyState(VirtualKey.Shift) == CoreVirtualKeyStates.Down && e.Key == VirtualKey.Enter)
            {
                MessageInput.Text += "\n";
            }
            else if (e.Key == VirtualKey.Enter)
            {
                SendMessage();
            }
        }

        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            SendMessage();
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(Settings));
        }
    }
}
