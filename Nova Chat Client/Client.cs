using ChatLib;
using ChatLib.DataStates;
using ChatLib.Extras;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Windows.Forms;
using static Client.EmbeddedYepDevelopmentTools;

namespace Client
{
    public partial class Tcp_Client : Form
    {
        About AboutBox = new About();
        Help HelpWindow = new Help();
        Settings SettingsWindow;

        ObservableDictionary<string, object> settings = new ObservableDictionary<string, object>();
        public string[] Rooms { get; private set; }

        TcpClient tcpClient;
        User user;
        NColor TagColor;

        Random rnd = new Random();
        NotificationManager notifications;

        #region Debug Variables
        public bool debug { get; private set; }
        private string ServerPath;
        #endregion

        public Tcp_Client(string[] args)
        {
            foreach (string argument in args)
            {
                if (argument == "/debug")
                {
                    debug = true;
                }
            }
            InitializeComponent();
            print("Welcome to the Nova Chat Client. Please enter an IP address above and click 'Connect' to begin.\n" +
                "Press 'Delete' when focused in this box to clear it, or use the 'Clear History' button in the menu.", Chat);

            SettingsWindow = new Settings(this, ref settings);
            notifications = new NotificationManager(ref settings);
            TagColor = NColor.FromRGB(rnd.Next(256), rnd.Next(256), rnd.Next(256));
        }

        private void SendMessage()
        {
            try
            {
                if (chatBox.Text.ToLower().StartsWith("/whisper"))
                {
                    string[] text = chatBox.Text.Split('"', '"');
                    try
                    {
                        user.CreateWhisper(text[3], TagColor, text[1]);
                        print(nameBox.Text + ": " + "Message privately sent to " + text[1], Chat, NColorToColor(TagColor));
                    }
                    catch (Exception ex)
                    {
                        print("Couldn't run command", Chat, Color.Red);
                        printToLog("Couldn't run command -> " + ex.Message, Color.Red);
                    }
                }
                else if (chatBox.Text.StartsWith("/color"))
                {
                    string[] text = chatBox.Text.Replace("/color ", "").Split(' ');
                    if (text.Length > 1)
                    {
                        TagColor = NColor.FromRGB(int.Parse(text[0]), int.Parse(text[1]), int.Parse(text[2]));
                    }
                    else
                    {
                        TagColor = ColorToNColor(Color.FromName(text[0]));
                    }
                }
                else if (chatBox.Text.StartsWith("/info"))
                {
                    string[] command = chatBox.Text.Replace("/info ", "").Split(' ');
                    if (command[0] == "users")
                    {
                        user.CreateInformation(InfomationType.ConnectedUsers);
                    }
                    else if (command[0] == "time")
                    {
                        user.CreateInformation(InfomationType.ConnectTime);
                    }
                    else if (command[0] == "sent")
                    {
                        user.CreateInformation(InfomationType.MessagesSent);
                    }
                    else if (command[0] == "uptime")
                    {
                        user.CreateInformation(InfomationType.ServerUptime);
                    }
                    else if (command[0] == "rooms")
                    {
                        user.CreateInformation(InfomationType.Rooms);
                    }
                    else
                    {
                        print("Unknown Parameter", Chat);
                    }
                }
                else if (chatBox.Text.StartsWith("/changeroom"))
                {
                    string room = chatBox.Text.Replace("/changeroom ", "");
                    user.CreateStatus(StatusType.ChangeRoom, room);
                }
                else
                {
                    if (user != null)
                    {
                        print(nameBox.Text + ": " + chatBox.Text, Chat, NColorToColor(TagColor));
                        user.CreateMessage(chatBox.Text, TagColor);
                    }
                }
            }
            catch (Exception ex)
            {
                print("Error Sending Message -> " + ex.Message, Log, Color.Red);
            }
            chatBox.Clear();
        }

        private void Connect()
        {
            if (IPBox.Text == "IP Address" || IPBox.Text.Length < 1)
            {
                if (debug)
                {
                    IPBox.Text = GetLocalIPAddress();
                }
                else
                {
                    IPBox.Text = "novastudios.tk";
                }
            }

            Thread t = new Thread(delegate ()
            {
                try
                {
                    if (tcpClient != null)
                    {
                        if (tcpClient.Connected)
                        {
                            user.CreateStatus(StatusType.Disconnecting);
                            ChangeConnectionInputState(true);
                            return;
                        }
                    }

                    print("Connecting... ", Log);
                    tcpClient = new TcpClient(IPBox.Text, 8910);

                    ChatLib.Message secure = MessageHelpers.GetMessage(tcpClient.GetStream());
                    string password = "";
                    if(secure.Name == "locked")
                    {
                        using(TextInput input = new TextInput())
                        {
                            if(input.ShowDialog() == DialogResult.OK)
                            {
                                password = input.Password;
                            }
                            else
                            {
                                user.CreateStatus(StatusType.Disconnecting);
                                ChangeConnectionInputState(true);
                                return;
                            }
                        }
                    }

                    if(secure.Content != "")
                    {
                        SslStream ssl = new SslStream(tcpClient.GetStream(), false, new RemoteCertificateValidationCallback(ValidateServerCertificate), null);
                        ssl.AuthenticateAsClient(secure.Content);

                        user = new User(nameBox.Text, ssl);
                        user.Init(password);
                    }
                    else
                    {
                        user = new User(nameBox.Text, tcpClient.GetStream());
                        user.Init();
                    }

                    // Setup Callbacks
                    user.OnMessageReceivedCallback += User_OnMessageReceivedCallback;
                    user.OnMessageStatusReceivedCallback += User_OnMessageStatusReceivedCallback;
                    user.OnMessageTransferReceivedCallback += User_OnMessageTransferReceivedCallback;
                    user.OnMessageWhisperReceivedCallback += User_OnMessageWhisperReceivedCallback;
                    //user.OnMessageAnyReceivedCallback += User_OnMessageAnyReceivedCallback;
                    user.OnMesssageInformationReceivedCallback += User_OnMesssageInformationReceivedCallback;
                    user.OnErrorCallback += (e) => { print(e.Message + " " + e.TargetSite + " " + GetLineNumber(e), Log); };

                    ChangeConnectionInputState(false);
                    print("Successfully connected to " + IPBox.Text, Log, Color.LimeGreen);
                }
                catch (Exception ex)
                {
                    print("Connection failed -> " + ex.Message, Log, Color.Red);
                }
            });

            t.IsBackground = true;
            t.Start();
        }

        static int GetLineNumber(Exception e)
        {
            var st = new StackTrace(e, true);
            var frame = st.GetFrame(0);
            return frame.GetFileLineNumber();
        }

        private void User_OnMesssageInformationReceivedCallback(ChatLib.Message message)
        {
            if (message.InfomationType == InfomationType.Rooms)
            {
                Rooms = message.Content.Split();
            }
            print(message.Name + ": " + message.Content, Chat);
        }

        private void User_OnMessageWhisperReceivedCallback(ChatLib.Message message)
        {
            print("Private Message From " + message.Name + ": " + message.Content, Chat, NColorToColor(message.Color));
        }

        private void User_OnMessageTransferReceivedCallback(ChatLib.Message message)
        {
            File.WriteAllBytes(message.Filename, message.FileContents);
            print(message.Name + ": file://" + new FileInfo(message.Filename).FullName.Replace(@"\", "/"), Chat, NColorToColor(message.Color));
        }

        private void User_OnMessageStatusReceivedCallback(ChatLib.Message message)
        {
            if (message.StatusType == StatusType.Connected)
            {
                print(message.Name + " Connected", Log);
            }
            else if (message.StatusType == StatusType.Disconnected)
            {
                print(message.Name + " Disconnected", Log);
            }
            else if (message.StatusType == StatusType.Disconnecting)
            {
                if (message.Content != null || message.Content != "")
                {
                    print(message.Content, Log, Color.Red);
                }
                print(message.Name + " Disconnected", Log);
                user.Close();
                tcpClient.Close();
                user = null;
                tcpClient.Dispose();
                ChangeConnectionInputState(true);
            }
            else if (message.StatusType == StatusType.ErrorDisconnect)
            {
                print(message.Content, Log);
                user.Close();
                tcpClient.Close();
                user = null;
                tcpClient.Dispose();
                ChangeConnectionInputState(true);
            }
        }

        private void User_OnMessageReceivedCallback(ChatLib.Message message)
        {
            print(message.Name + ": " + message.Content, Chat, NColorToColor(message.Color));
            notifications.ShowNotification(message.Name, message.Content);
        }

        public static bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            if (sslPolicyErrors == SslPolicyErrors.None)
                return true;

            Console.WriteLine("Certificate error: {0}", sslPolicyErrors);

            // Do not allow this client to communicate with unauthenticated servers.
            return false;
        }

        private void sendToastToolStripMenuItem_Click(object sender, EventArgs e)
        {
            notifications.ShowNotification("System", "This is a test notification");
        }
    }
}
