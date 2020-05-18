using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading;
using ChatLib;
using ChatLib.DataStates;
using ChatLib.Extras;
using ChatLib.Json;
using System.Threading.Tasks;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;
using System.Security.Authentication;

namespace Server
{
    class Tcp_Server
    {
        static Dictionary<string, ClientInfo> clients;
        static List<Message> buffer;
        static IPAddress iPAddress = IPAddress.Parse(GetLocalIPAddress());
        static int TotalMessagesSent = 0;

        //static X509Certificate2 X509 = null;

        static void Main(string[] args)
        {
            clients = new Dictionary<string, ClientInfo>();
            buffer = new List<Message>();

            DateTime startup = DateTime.UtcNow;

            //X509 = new X509Certificate2(@"C:\Users\aiden\OneDrive\Desktop\sslforfree\certificateChat.pfx", "");
            //X509 = X509Certificate2.CreateFromSignedFile(@"C:\Users\aiden\OneDrive\Desktop\sslforfree\certificateChat.pfx");

            Thread web = new Thread(() => WebListener());
            web.IsBackground = true;
            web.Start();

            Thread desktop = new Thread(() => ClientListener());
            desktop.IsBackground = true;
            desktop.Start();

            Console.WriteLine("Press Enter to Close");
            Console.ReadLine();
        }


        static void WebListener()
        {
            TcpListener server = new TcpListener(iPAddress, 8911);
            server.Start();
            while (true)
            {
                TcpClient client = server.AcceptTcpClient();

                //SslStream ssl = new SslStream(client.GetStream(), false);
                //ssl.AuthenticateAsServer(X509, false, SslProtocols.Default, true);

                while (client.Available < 3) ;

                //JsonMessageHelpers.HandleHandshake(client.GetStream(), client.Available);
                NetworkStream stream = client.GetStream();
                JsonMessageHelpers.HandleHandshake(stream, client.Available);

                while (client.Available < 3) ;

                JsonMessage json = JsonMessageHelpers.GetJsonMessage(stream, client.Available);
                Console.WriteLine(json.Content);

                if (json.MessageType == MessageType.Initionalize && !clients.ContainsKey(json.Name))
                {
                    clients.Add(json.Name, new ClientInfo(json.Name, stream, ClientType.Web));
                    ThreadPool.QueueUserWorkItem(HandleClientWebWorker, new object[3] { client, json.Name, stream });
                }
                else
                {
                    Console.WriteLine("Name Already Exsits: " + json.Name);
                    JsonMessage h = new JsonMessage(json.Name, MessageType.Status);
                    h.SetStatusType(StatusType.ErrorDisconnect);
                    h.SetContent("User with that name already exsists");
                    JsonMessageHelpers.SetJsonMessage(stream, h);
                    stream.Close();
                    stream.Dispose();
                    client.Close();
                    client.Dispose();
                }
            }
        }

        static void ClientListener()
        {
            TcpListener server = new TcpListener(iPAddress, 8910);
            server.Start();
            while (true)
            {
                Console.WriteLine("Waiting for Connection...");
                TcpClient client = server.AcceptTcpClient();

                NetworkStream stream = client.GetStream();

                //SslStream ssl = new SslStream(client.GetStream(), false);
                //ssl.AuthenticateAsServer(X509, false, true);

                Message message = MessageHelpers.GetMessage(stream);
                if (message.MessageType == MessageType.Initionalize && !clients.ContainsKey(message.Name))
                {
                    string name = message.Name;
                    clients.Add(name, new ClientInfo(name, stream, ClientType.Desktop));
                    Console.WriteLine(name + " connected!");
                    ThreadPool.QueueUserWorkItem(HandleClientDesktopWorker, new object[2] { name, stream });
                }
                else
                {
                    Console.WriteLine("Name Already Exsits: " + message.Name);
                    Message h = new Message(message.Name, MessageType.Status);
                    h.SetStatusType(StatusType.ErrorDisconnect);
                    h.SetContent("User with that name already exsists");
                    MessageHelpers.SetMessage(stream, h);
                    stream.Close();
                    stream.Dispose();
                    client.Close();
                    client.Dispose();
                }
            }
        }

        private static string GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            throw new Exception("No network adapters with an IPv4 address in the system!");
        }

        private static void HandleClientDesktopWorker(object token)
        {
            object[] info = (object[])token;
            NetworkStream stream = (NetworkStream)info[1];
            string name = (string)info[0];

            // Tell all connected clients that we are connected
            Message cm = new Message(name, MessageType.Status);
            cm.SetStatusType(StatusType.Connected);
            SendToAllClients(cm, null);

            // Send The Server Message Buffer
            foreach (Message message in buffer)
            {
                MessageHelpers.SetMessage(stream, message);
            }
            bool active = true;
            while (active)
            {
                try
                {
                    if(active == false)
                    {
                        break;
                    }
                    Message m = MessageHelpers.GetMessage(stream);

                    // Manage Message buffer
                    if (m.MessageType == MessageType.Message)
                    {
                        TotalMessagesSent += 1;
                        buffer.Add(m);
                        if (buffer.Count > 10)
                        {
                            buffer.RemoveRange(0, buffer.Count - 10);
                        }
                    }

                    if(m.MessageType == MessageType.Infomation)
                    {

                    }

                    if (m.Name != name)
                    {
                        m.SetName(name);
                    }
                    if (m.StatusType == StatusType.Disconnecting && m.MessageType == MessageType.Status)
                    {
                        MessageHelpers.SetMessage(stream, m);
                        stream.Close();
                        clients.Remove(name);
                        Console.WriteLine(name + " disconnected");

                        Message d = new Message(name, MessageType.Status);
                        d.SetStatusType(StatusType.Disconnected);

                        SendToAllClients(d, null);

                        active = false;
                        break;
                    }

                    foreach (KeyValuePair<string, ClientInfo> network in clients)
                    {
                        if (network.Key == name)
                        {
                            continue;
                        }
                        else
                        {
                            if (m.EndPoint == "")
                            {
                                SendToAllClients(m, null);
                                break;
                            }
                            else
                            {
                                if (network.Key == m.EndPoint)
                                {
                                    MessageHelpers.SetMessage(network.Value.Stream, m);
                                }
                            }
                        }
                    }
                }
                catch (IOException)
                {
                    clients[name].Stream.Close();
                    clients.Remove(name);
                    Console.WriteLine(name + " disconnected");
                    foreach (KeyValuePair<string, ClientInfo> network in clients)
                    {
                        Message d = new Message(network.Key == name ? "You" : name, MessageType.Status);
                        d.SetStatusType(StatusType.Disconnected);
                        MessageHelpers.SetMessage(network.Value.Stream, d);
                    }
                    active = false;
                    break;
                }
                catch (Exception ex)
                {
                    clients[name].Stream.Close();
                    clients.Remove(name);
                    Console.WriteLine(name + " disconnected due to an error. Details: " + ex.Message);
                    foreach (KeyValuePair<string, ClientInfo> network in clients)
                    {
                        Message e = new Message(network.Key == name ? "You" : name, MessageType.Status);
                        e.SetStatusType(StatusType.ErrorDisconnect);
                        MessageHelpers.SetMessage(network.Value.Stream, e);
                    }
                    active = false;
                    break;
                }
            }
        }

        private static void HandleClientWebWorker(object token)
        {
            object[] b = (object[])token;
            TcpClient client = (TcpClient)b[0];
            string name = b[1].ToString();
            NetworkStream stream = (NetworkStream)b[2];

            // Tell all connected clients that we are connected
            JsonMessage cm = new JsonMessage(name, MessageType.Status);
            cm.SetStatusType(StatusType.Connected);
            SendToAllClients(null, cm);

            // Send The Server Message Buffer
            foreach (Message message in buffer)
            {
                JsonMessageHelpers.SetJsonMessage(stream, message.ToJsonMessage());
            }

            while (true)
            {
                if (client.Available < 3)
                {
                    Thread.Sleep(20);
                    continue;
                }

                JsonMessage m = JsonMessageHelpers.GetJsonMessage(stream, client.Available);

                if (m.MessageType == MessageType.Message)
                {
                    TotalMessagesSent += 1;
                    buffer.Add(m.ToMessage());
                    if (buffer.Count > 10)
                    {
                        buffer.RemoveRange(0, buffer.Count - 10);
                    }
                }

                if (m.Name != name)
                {
                    m.SetName(name);
                }

                if (m.StatusType == StatusType.Disconnecting && m.MessageType == MessageType.Status)
                {
                    stream.Close();
                    clients.Remove(name);
                    Console.WriteLine(name + " disconnected");

                    JsonMessage d = new JsonMessage(name, MessageType.Status);
                    d.SetStatusType(StatusType.Disconnected);

                    SendToAllClients(null, d);
                    break;
                }

                SendToAllClients(null, m);
            }
        }

        public static async void SendToAllClients(Message message, JsonMessage json)
        {
            List<Task> tasks = new List<Task>();
            foreach (KeyValuePair<string, ClientInfo> network in clients)
            {
                if (message != null)
                {
                    if (message.Name == network.Key)
                    {
                        continue;
                    }
                }
                else
                {
                    if (json.Name == network.Key)
                    {
                        continue;
                    }
                }

                if (network.Value.ClientType == ClientType.Desktop)
                {
                    if (message == null)
                    {
                        tasks.Add(MessageHelpers.SetMessageAsync(network.Value.Stream, json.ToMessage()));
                    }
                    else
                    {
                        tasks.Add(MessageHelpers.SetMessageAsync(network.Value.Stream, message));
                    }
                }
                else if(network.Value.ClientType == ClientType.Web)
                {
                    if (json == null) {
                        tasks.Add(JsonMessageHelpers.SetJsonMessageAsync(network.Value.Stream, message.ToJsonMessage()));
                    }
                    else
                    {
                        tasks.Add(JsonMessageHelpers.SetJsonMessageAsync(network.Value.Stream, json));
                    }
                }
                else
                {
                    Console.WriteLine("Unknown Client");
                }
            }

            while (tasks.Any())
            {
                tasks.Remove(await Task.WhenAny(tasks));
            }
        }

        //public static string InformationHandler(InfomationType type)
        //{

        //}
    }
}
