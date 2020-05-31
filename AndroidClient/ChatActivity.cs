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
using ChatLib;
using ChatLib.Extras;

namespace AndroidClient
{
    [Activity(Label = "ChatActivity")]
    public class ChatActivity : Activity
    {
        // Com stuff
        User user;
        TcpClient client;

        // Input/Lists
        private ListView Messages;
        private Button SendBtn;
        private EditText Message;

        // Other stuff i could care less about
        private string username;
        private NColor color;
        private Random rnd;
        private List<string> items;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.layout_chat);
            rnd = new Random();
            color = NColor.FromRGB(rnd.Next(0, 255), rnd.Next(0, 255), rnd.Next(0, 255));
            items = new List<string>();

            // Create your application here
            int port = int.Parse(Intent.GetStringExtra("Port"));
            string address = Intent.GetStringExtra("Ip");
            username = Intent.GetStringExtra("Username");

            client = new TcpClient(address, port);

            user = new User(username, client.GetStream());
            user.Init();
            user.OnMessageStatusReceivedCallback += User_OnMessageStatusReceivedCallback;
            user.OnErrorCallback += User_OnErrorCallback;
            user.OnMessageReceivedCallback += User_OnMessageReceivedCallback;

            Messages = FindViewById<ListView>(Resource.Id.ChatList);
            SendBtn = FindViewById<Button>(Resource.Id.button1);
            SendBtn.Click += SendBtn_Click;
            Message = FindViewById<EditText>(Resource.Id.input_message);
        }

        private void User_OnMessageReceivedCallback(ChatLib.Message message)
        {
            items.Add(message.Name + ": " + message.Content);

            ArrayAdapter<string> adapter = new ArrayAdapter<string>(this, Android.Resource.Layout.SimpleListItem1, items);
            Messages.Adapter = adapter;
        }

        private void SendBtn_Click(object sender, EventArgs e)
        {
            user.CreateMessage(Message.Text, color);
            Message.Text = "";
        }

        private void User_OnErrorCallback(Exception exception)
        {
            Toast.MakeText(this, exception.Message, ToastLength.Short).Show();
        }

        private void User_OnMessageStatusReceivedCallback(ChatLib.Message message)
        {
            if (message.StatusType == StatusType.Disconnecting)
            {
                user.Close();
                client.Close();
                user = null;
                client.Dispose();
            }
        }
    }
}