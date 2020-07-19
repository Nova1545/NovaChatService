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

namespace NovaChatClient.Pages
{
    public sealed partial class ChatUI : Page
    {
        public ChatUI()
        {
            this.InitializeComponent();
        }

        private void MessageInput_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter)
            {
                TextBox txt = (TextBox)sender;
                ChatField.Items.Add(new FormattedMessage(Username, txt.Text, DateTime.Now));
                txt.Text = "";
            }
        }
    }
}
