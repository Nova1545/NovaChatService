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

using ChatLib;
using ChatLib.DataStates;
using ChatLib.Extras;
using User = ChatLib.User;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Net.Security;

namespace NovaChatClient.Pages
{
    public sealed partial class ChatUI : Page
    {
        User user;
        TcpClient client;

        public ChatUI()
        {
            this.InitializeComponent();

            Color = NColor.GenerateRandomColor();

            Loaded += async (s, e) =>
            {
                client = new TcpClient(Address, Port);
                NetworkStream stream = client.GetStream();
                Message secure = MessageHelpers.GetMessage(stream);
                string password = "";
                if (secure.Name == "locked")
                {
                    PasswordDialog pd = new PasswordDialog();
                    if(await pd.ShowAsync() == ContentDialogResult.Primary)
                    {
                        password = pd.Password;
                    }
                    else
                    {
                        if (Frame.CanGoBack)
                        {
                            Frame.GoBack();
                        }
                    }
                }

                if(secure.Content != "")
                {
                    SslStream ssl = new SslStream(stream);
                    ssl.AuthenticateAsClient(secure.Content);
                    user = new User(Username, ssl);
                    user.Init(password);
                }
                else
                {
                    user = new User(Username, stream);
                    user.Init(password);
                }

                user.OnMessageReceivedCallback += User_OnMessageReceivedCallback;
                user.OnMessageStatusReceivedCallback += User_OnMessageStatusReceivedCallback;
                user.OnMessageTransferReceivedCallback += User_OnMessageTransferReceivedCallback;
                user.OnMessageWhisperReceivedCallback += User_OnMessageWhisperReceivedCallback;
                user.OnMessageInitReceivedCallback += User_OnMessageInitReceivedCallback;
                user.OnMesssageInformationReceivedCallback += User_OnMesssageInformationReceivedCallback;
                user.OnErrorCallback += (d) => { Debug.WriteLine(d.Message); };
            };
        }

        private async void User_OnMessageReceivedCallback(Message message)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                ChatField.Items.Add(new UserMessage(message.Name, message.Content, DateTime.Now, message.Color));
            });
        }

        private void User_OnMesssageInformationReceivedCallback(Message message)
        {

        }

        private void User_OnMessageInitReceivedCallback(Message message)
        {
            if (message.Content == "admin")
            {
                //string password = "";
                //using (TextInput input = new TextInput())
                //{
                //    if (input.ShowDialog() == DialogResult.OK)
                //    {
                //        password = input.Password;
                //    }
                //    else
                //    {
                //        user.CreateStatus(StatusType.Disconnecting);
                //        ChangeConnectionInputState(true);
                //        return;
                //    }
                //}
                //user.Init(password);
            }
        }

        private void User_OnMessageWhisperReceivedCallback(Message message)
        {
            Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                ChatField.Items.Add(new UserMessage("[Private] " + message.Name, message.Content, DateTime.Now, message.Color));
            }).GetResults();
        }

        private void User_OnMessageTransferReceivedCallback(Message message)
        {
            
        }

        private void User_OnMessageStatusReceivedCallback(Message message)
        {
            //if (message.StatusType == StatusType.Connected)
            //{
            //    print(message.Name + " Connected", Log);
            //}
            //else if (message.StatusType == StatusType.Disconnected)
            //{
            //    print(message.Name + " Disconnected", Log);
            //}
            if (message.StatusType == StatusType.Disconnecting)
            {
                user.Close();
                client.Close();
                user = null;
                client.Dispose();
            }
            else if (message.StatusType == StatusType.ErrorDisconnect)
            {
                user.Close();
                client.Close();
                user = null;
                client.Dispose();
            }
        }

        private void SendMessage()
        {
            user.CreateMessage(MessageInput.Text, Color);
            //ChatField.Items.Add(new UserMessage(Username, MessageInput.Text, DateTime.Now, Color));
            ChatField.Items.Add(new StatusMessage("System", MessageInput.Text));
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

        private void LeaveChatButton_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(MainPage));
        }
    }
}
