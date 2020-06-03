using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using ChatLib.DataStates;
using Android.Graphics;
using ChatLib.Extras;
using ChatLib;
using Android.Text;
using Android.Text.Style;

namespace AndroidClient
{
    [Activity(Label = "ChatActivity", WindowSoftInputMode = SoftInput.AdjustUnspecified)]
    public class ChatActivity : Activity
    {
        // Com stuff
        User user;
        TcpClient client;

        // Input/Lists
        private TextView Messages;
        private Button SendBtn;
        private EditText Message;

        // Other stuff i could care less about
        private string username;
        private int port;
        private string address;
        private NColor color;
        private Random rnd;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.layout_chat);
            rnd = new Random();
            color = NColor.FromRGB(rnd.Next(0, 255), rnd.Next(0, 255), rnd.Next(0, 255));

            // Create your application here
            port = int.Parse(Intent.GetStringExtra("Port"));
            address = Intent.GetStringExtra("Ip");
            username = Intent.GetStringExtra("Username");
        }

        private void Message_KeyPress(object sender, View.KeyEventArgs e)
        {
            if (e.KeyCode == Keycode.Enter && e.Event.Action == KeyEventActions.Down)
            {
                var span = new SpannableString(username + ": " + Message.Text + "\n");
                span.SetSpan(new ForegroundColorSpan(NColorToColor(color)), 0, span.Length(), 0);
                Messages.Append(span);

                user.CreateMessage(Message.Text, color);
                Message.Text = "";
            }
            else if(e.KeyCode == Keycode.Back)
            {
                Finish();
            }
        }

        private void User_OnMessageWisperReceivedCallback(ChatLib.Message message)
        {
            RunOnUiThread(() => { 
                var span = new SpannableString("Private message from " + message.Name + ": " + message.Content + "\n");
                span.SetSpan(new ForegroundColorSpan(NColorToColor(message.Color)), 0, span.Length(), 0);
                Messages.Append(span);
            });
        }

        private void User_OnMessageReceivedCallback(ChatLib.Message message)
        {
            RunOnUiThread(() =>
            {
                var span = new SpannableString(message.Name + ": " + message.Content + "\n");
                span.SetSpan(new ForegroundColorSpan(NColorToColor(message.Color)), 0, span.Length(), 0);
                Messages.Append(span);
            });
        }

        private void User_OnMessageStatusReceivedCallback(ChatLib.Message message)
        {
            RunOnUiThread(() =>
            {
                if (message.StatusType == StatusType.Disconnecting)
                {
                    user.Close();
                    client.Close();
                    user = null;
                    client.Dispose();
                }
                else if (message.StatusType == StatusType.Connected)
                {
                    var span = new SpannableString(message.Name + " Connected" + "\n");
                    span.SetSpan(new ForegroundColorSpan(NColorToColor(color)), 0, span.Length(), 0);
                    Messages.Append(span);
                }
                else if (message.StatusType == StatusType.Disconnected)
                {
                    var span = new SpannableString(message.Name + " Disconnected" + "\n");
                    span.SetSpan(new ForegroundColorSpan(NColorToColor(color)), 0, span.Length(), 0);
                    Messages.Append(span);
                }
            });
        }

        private void SendBtn_Click(object sender, EventArgs e)
        {
            var span = new SpannableString(username + ": " + Message.Text + "\n");
            span.SetSpan(new ForegroundColorSpan(NColorToColor(color)), 0, span.Length(), 0);
            Messages.Append(span);

            user.CreateMessage(Message.Text, color);
            Message.Text = "";
        }

        protected override void OnStop()
        {
            base.OnStop();
            user.CreateStatus(StatusType.Disconnecting);
        }

        protected override void OnStart()
        {
            base.OnStart();
            client = new TcpClient(address, port);
            Toast.MakeText(this, "Connected", ToastLength.Short).Show();

            user = new User(username, client.GetStream());
            user.Init();
            user.OnMessageStatusReceivedCallback += User_OnMessageStatusReceivedCallback;
            user.OnMessageReceivedCallback += User_OnMessageReceivedCallback;
            user.OnMessageWisperReceivedCallback += User_OnMessageWisperReceivedCallback;

            Messages = FindViewById<TextView>(Resource.Id.ChatList);
            SendBtn = FindViewById<Button>(Resource.Id.button1);
            SendBtn.Click += SendBtn_Click;
            Message = FindViewById<EditText>(Resource.Id.input_message);

            Message.KeyPress += Message_KeyPress;
        }

        Color NColorToColor(NColor color)
        {
            return new Color(color.R, color.G, color.B);
        }
    }
}