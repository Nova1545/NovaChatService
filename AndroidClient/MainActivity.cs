using System;
using System.Collections.Generic;
using Android.App;
using Android.OS;
using Android.Runtime;
using Android.Support.Design.Widget;
using Android.Support.V7.App;
using Android.Views;
using Android.Widget;
using AlertDialog = Android.App.AlertDialog;
using Xamarin.Essentials;
using Android.Content;

namespace AndroidClient
{
    [Activity(Theme = "@style/AppTheme.NoActionBar", MainLauncher = true)]
    public class MainActivity : AppCompatActivity
    {
        private ListView Servers;
        private AlertDialog alertDialog;

        List<string> items = new List<string>();

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.activity_main);

            Android.Support.V7.Widget.Toolbar toolbar = FindViewById<Android.Support.V7.Widget.Toolbar>(Resource.Id.toolbar);
            SetSupportActionBar(toolbar);

            FloatingActionButton fab = FindViewById<FloatingActionButton>(Resource.Id.fab);
            fab.Click += FabOnClick;

            Servers = FindViewById<ListView>(Resource.Id.ServerList);
            Servers.ItemClick += Servers_ItemClick;
            Servers.ItemLongClick += Servers_ItemLongClick;


            string[] servers = Preferences.Get("servers", "") != "" ? Preferences.Get("servers", "").Split('|') : new string[0];
            foreach (string server in servers)
            {
                if(server != ""){
                    items.Add(server);
                }
            }

            ArrayAdapter<string> adapter = new ArrayAdapter<string>(this, Android.Resource.Layout.SimpleListItem1, items);
            Servers.Adapter = adapter;
        }

        private void Servers_ItemLongClick(object sender, AdapterView.ItemLongClickEventArgs e)
        {
            Preferences.Set("servers", Preferences.Get("servers", "").Replace(items[e.Position] + "|", ""));

            items.RemoveAt(e.Position);
            Toast.MakeText(this, "Removed Server", ToastLength.Short).Show();

            ArrayAdapter<string> adapter = new ArrayAdapter<string>(this, Android.Resource.Layout.SimpleListItem1, items);
            Servers.Adapter = adapter;
        }

        private void Servers_ItemClick(object sender, AdapterView.ItemClickEventArgs e)
        {
            string[] address = items[e.Position].Split(':');
            string[] d = address[1].Split("->");
            Intent intent = new Intent(this, typeof(ChatActivity));
            intent.PutExtra("Ip", address[0]);
            intent.PutExtra("Port", d[0].Replace(" ", ""));
            intent.PutExtra("Username", d[1].Replace(" ", ""));
            StartActivity(intent);
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.menu_main, menu);
            return true;
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            //int id = item.ItemId;
            //if (id == Resource.Id.action_settings)
            //{
            //    return true;
            //}

            return base.OnOptionsItemSelected(item);
        }

        EditText username;
        EditText address;
        EditText port;

        private void FabOnClick(object sender, EventArgs eventArgs)
        {
            Intent intent = new Intent(this, typeof(Drawer));
            StartActivity(intent);
            return;

            View view = (View) sender;
            //Snackbar.Make(view, "Replace with your own action", Snackbar.LengthLong)
            //    .SetAction("Action", (View.IOnClickListener)null).Show();

            View content = LayoutInflater.Inflate(Resource.Layout.layout_dialog, null);

            //2. Getting the view elements
            Button button1 = (Button)content.FindViewById(Resource.Id.dialog_btn_cancel);
            Button button2 = (Button)content.FindViewById(Resource.Id.dialog_btn_sure);

            username = content.FindViewById<EditText>(Resource.Id.input_username);
            address = content.FindViewById<EditText>(Resource.Id.input_address);
            port = content.FindViewById<EditText>(Resource.Id.input_port);

            //3.create a new alertDialog
            alertDialog = new AlertDialog.Builder(this).Create();

            //4. set the view
            alertDialog.SetView(content);

            //5. show the dialog
            alertDialog.Show(); // This should be called before looking up for elements

            //6.Button click event
            button2.Click += Button2_Click;
            button1.Click += Button1_Click;
        }

        private void Button1_Click(object sender, EventArgs e)
        {
            alertDialog.Dismiss();
        }

        private void Button2_Click(object sender, EventArgs e)
        {
            alertDialog.Dismiss();
            Toast.MakeText(this, "Server saved", ToastLength.Short).Show();

            items.Add(address.Text + ":" + port.Text + " -> " + username.Text);
            Preferences.Set("servers", Preferences.Get("servers", "") + address.Text + ":" + port.Text + " -> " + username.Text + "|");

            ArrayAdapter<string> adapter = new ArrayAdapter<string>(this, Android.Resource.Layout.SimpleListItem1, items);
            Servers.Adapter = adapter;

            Intent intent = new Intent(this, typeof(ChatActivity));
            intent.PutExtra("Ip", address.Text);
            intent.PutExtra("Port", port.Text);
            intent.PutExtra("Username", username.Text);
            StartActivity(intent);
        }
    }
}

