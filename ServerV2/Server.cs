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
using System.Drawing;
using System.Reflection;
using Microsoft.CSharp.RuntimeBinder;
using System.Diagnostics;
using System.Xml;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Security.Authentication;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;

namespace ServerV2
{
    class Server
    {
        static Dictionary<string, ClientInfo> Clients;
        static Dictionary<string, Room> Rooms;
        static List<IPAddress> BanList;
        static BotHandler BotHandler;

        static int WebPort = 8911;
        static int DefaultRoomID = 0;
        static int DesktopPort = 8910;
        static bool WebActive = false;
        static bool DesktopActive = false;
        static bool HasPassword = false;
        static IPAddress IPAddress = null;
        static X509Certificate2 X509 = null;
        static string sPassword = "";

        public delegate MessageState OnMessageSent(Message message, ClientInfo sender);
        public static event OnMessageSent OnMessageSentCallback;

        public delegate MessageState OnJsonMessageSent(JsonMessage message, ClientInfo sender);
        public static event OnJsonMessageSent OnJsonMessageSentCallback;

        public delegate void OnUserChangeRoom(ClientInfo sender, Room oldRoom, Room newRoom);
        public static event OnUserChangeRoom OnUserChangeRoomCallback;

        public delegate void OnUserConnect(ClientInfo client);
        public static event OnUserConnect OnUserConnectCallback;

        public delegate void OnUserDisconnected(ClientInfo client);
        public static event OnUserDisconnected OnUserDisconnectCallback;

        static void Main(string[] args)
        {
            Clients = new Dictionary<string, ClientInfo>();
            Rooms = new Dictionary<string, Room>();
            BanList = new List<IPAddress>();

            LoadConfig();
            LoadBots();

            if (WebActive)
            {
                TcpListener WebServer = new TcpListener(IPAddress, WebPort);
                WebServer.Start();
                WebServer.BeginAcceptTcpClient(OnWebAccept, WebServer);
            }

            if (DesktopActive)
            {
                TcpListener DesktopServer = new TcpListener(IPAddress, DesktopPort);
                DesktopServer.Start();
                DesktopServer.BeginAcceptTcpClient(OnDesktopAccept, DesktopServer);
            }

            bool run = true;
            while (run)
            {
                string[] command = Console.ReadLine().Split('|');
                if (command[0] == "rooms")
                {
                    foreach (KeyValuePair<string, Room> room in Rooms)
                    {
                        Console.WriteLine(room.Value.ToString());
                    }
                }
                else if(command[0] == "clients")
                {
                    foreach (KeyValuePair<string, ClientInfo> client in Clients)
                    {
                        Console.WriteLine(client.Value.ToString());
                    }
                }
                else if(command[0] == "bans")
                {
                    foreach (IPAddress address in BanList)
                    {
                        Console.WriteLine(address.ToString());
                    }
                }
                else if(command[0] == "ban")
                {
                    if(Clients.Any(x => x.Value.Name == command[1]))
                    {
                        ClientInfo client = Clients.Where(x => x.Value.Name == command[1]).First().Value;
                        Room r = Rooms.Where(x => x.Value.ID == client.RoomId).First().Value;
                        BanList.Add(client.ClientAddress);
                        File.AppendAllText("banned.txt", client.ClientAddress.ToString());
                        Message error = new Message("error", MessageType.Status);
                        error.SetStatusType(StatusType.ErrorDisconnect);
                        error.SetContent("You have been banned from this server");
                        if (Clients.ContainsKey(client.GUID))
                        {
                            r.RemoveUser(client);
                            Clients.Remove(client.GUID);
                        }

                        if(client.ClientType == ClientType.Web)
                        {
                            SendJsonMessage(client, error.ToJsonMessage());
                        }
                        else
                        {
                            SendMessage(client, error);
                        }

                        ClientInfo[] bannedClients = Clients.Where(x => x.Value.ClientAddress == client.ClientAddress).Select(x => x.Value).ToArray();
                        foreach (ClientInfo clients in bannedClients)
                        {
                            r = Rooms.Where(x => x.Value.ID == client.RoomId).First().Value;
                            File.AppendAllText("banned.txt", client.ClientAddress.ToString());
                            error = new Message("error", MessageType.Status);
                            error.SetStatusType(StatusType.ErrorDisconnect);
                            error.SetContent("You have been remove from this server because someone on your ip has been banned");
                            if (Clients.ContainsKey(client.GUID))
                            {
                                r.RemoveUser(client);
                                Clients.Remove(client.GUID);
                            }

                            if (client.ClientType == ClientType.Web)
                            {
                                SendJsonMessage(client, error.ToJsonMessage());
                            }
                            else
                            {
                                SendMessage(client, error);
                            }
                        }
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Unknown user: " + command[1]);
                        Console.ForegroundColor = ConsoleColor.White;
                    }
                }
                else if(command[0] == "unban")
                {
                    if (BanList.Contains(IPAddress.Parse(command[1])))
                    {
                        BanList.Remove(IPAddress.Parse(command[1]));
                        string file = "";
                        foreach (string line in File.ReadAllLines("banned.txt"))
                        {
                            if (line == command[1])
                            {
                                continue;
                            }
                            file += line + "\n";
                        }
                        File.WriteAllText("banned.txt", file);
                    }
                }
            }
        }

        // Listeners
        static void OnWebAccept(IAsyncResult ar)
        {
            TcpListener WebServer = (TcpListener)ar.AsyncState;
            TcpClient client = WebServer.EndAcceptTcpClient(ar);
            WebServer.BeginAcceptTcpClient(new AsyncCallback(OnWebAccept), WebServer);

            try
            {
                IPAddress addr = IPAddress.Parse(client.Client.RemoteEndPoint.ToString().Split(':')[0]);
                if (BanList.Contains(addr))
                {
                    client.GetStream().Close();
                    return;
                }

                if (X509 == null)
                {
                    NetworkStream stream = client.GetStream();
                    JsonMessageHelpers.HandleHandshake(stream);

                    JsonMessage json = JsonMessageHelpers.GetJsonMessage(stream);

                    if (json.Content != sPassword)
                    {
                        JsonMessage h = new JsonMessage(json.Name, MessageType.Status);
                        h.SetStatusType(StatusType.ErrorDisconnect);
                        h.SetContent("Incorrect Password");
                        JsonMessageHelpers.SetJsonMessage(stream, h);
                        stream.Close();
                        stream.Dispose();
                        client.Close();
                        client.Dispose();
                        return;
                    }

                    if (json.MessageType == MessageType.Initialize && Clients.Any(x => x.Value.Name == json.Name) == false)
                    {
                        ClientInfo c = new ClientInfo(json.Name, stream, ClientType.Web, addr);
                        Clients.Add(c.GUID, c);
                        ThreadPool.QueueUserWorkItem(WebWorker, c);
                    }
                    else
                    {
                        JsonMessage h = new JsonMessage(json.Name, MessageType.Status);
                        h.SetStatusType(StatusType.ErrorDisconnect);
                        h.SetContent("User with the name " + json.Name + " already exists");
                        JsonMessageHelpers.SetJsonMessage(stream, h);
                        stream.Close();
                        stream.Dispose();
                        client.Close();
                        client.Dispose();
                    }
                }
                else
                {
                    SslStream stream = new SslStream(client.GetStream());
                    stream.AuthenticateAsServer(X509, false, SslProtocols.Default, true);

                    JsonMessageHelpers.HandleHandshake(stream);

                    JsonMessage json = JsonMessageHelpers.GetJsonMessage(stream);
                    if (json.Content != sPassword)
                    {
                        JsonMessage h = new JsonMessage(json.Name, MessageType.Status);
                        h.SetStatusType(StatusType.ErrorDisconnect);
                        h.SetContent("Incorrect Password");
                        JsonMessageHelpers.SetJsonMessage(stream, h);
                        stream.Close();
                        stream.Dispose();
                        client.Close();
                        client.Dispose();
                    }
                    else
                    {

                        if (json.MessageType == MessageType.Initialize && Clients.Any(x => x.Value.Name == json.Name) == false)
                        {
                            ClientInfo c = new ClientInfo(json.Name, stream, ClientType.Web, addr);
                            Clients.Add(c.GUID, c);
                            ThreadPool.QueueUserWorkItem(WebWorker, c);
                        }
                        else
                        {
                            JsonMessage h = new JsonMessage(json.Name, MessageType.Status);
                            h.SetStatusType(StatusType.ErrorDisconnect);
                            h.SetContent("User with the name " + json.Name + " already exists");
                            JsonMessageHelpers.SetJsonMessage(stream, h);
                            stream.Close();
                            stream.Dispose();
                            client.Close();
                            client.Dispose();
                        }
                    }
                }
            }
            catch
            {
                client.GetStream().Close();
                return;
            }
        }

        static void OnDesktopAccept(IAsyncResult ar)
        {
            TcpListener DesktopServer = (TcpListener)ar.AsyncState;
            TcpClient client = DesktopServer.EndAcceptTcpClient(ar);
            DesktopServer.BeginAcceptTcpClient(OnDesktopAccept, DesktopServer);

            try
            {
                IPAddress addr = IPAddress.Parse(client.Client.RemoteEndPoint.ToString().Split(':')[0]);
                if (BanList.Contains(addr))
                {
                    client.GetStream().Close();
                    return;
                }

                if (X509 == null)
                {
                    Message secure = new Message(HasPassword? "locked" : "unlocked", MessageType.Initialize);
                    secure.SetContent("");
                    NetworkStream stream = client.GetStream();
                    MessageHelpers.SetMessage(stream, secure);

                    Message m = MessageHelpers.GetMessage(stream);
                    if(m.MessageType == MessageType.Initialize)
                    {
                        m = new Message("Server", MessageType.Status);
                        m.SetStatusType(StatusType.ErrorDisconnect);
                        MessageHelpers.SetMessage(stream, m);
                        return;
                    }
                    if (m.Content != sPassword)
                    {
                        m = new Message("Server", MessageType.Status);
                        m.SetStatusType(StatusType.ErrorDisconnect);
                        m.SetContent("Incorrect Password");
                        MessageHelpers.SetMessage(stream, m);
                    }
                    else
                    {
                        if (Clients.Any(x => x.Value.Name == m.Name))
                        {
                            m = new Message("Server", MessageType.Status);
                            m.SetStatusType(StatusType.ErrorDisconnect);
                            m.SetContent($"User with name {m.Name} alread exsites");
                            MessageHelpers.SetMessage(stream, m);
                        }
                        else
                        {
                            ClientInfo c = new ClientInfo(m.Name, stream, ClientType.Desktop, addr);
                            Clients.Add(c.GUID, c);
                            ThreadPool.QueueUserWorkItem(DesktopWorker, c);
                        }
                    }
                }
                else
                {
                    Message secure = new Message(HasPassword ? "locked" : "unlocked", MessageType.Initialize);
                    secure.SetContent(X509.SubjectName.Name.Replace("CN=", ""));
                    MessageHelpers.SetMessage(client.GetStream(), secure);

                    SslStream stream = new SslStream(client.GetStream(), false);
                    stream.AuthenticateAsServer(X509, false, true);

                    Message m = MessageHelpers.GetMessage(stream);
                    if (m.MessageType != MessageType.Initionalize)
                    {
                        m = new Message("Server", MessageType.Status);
                        m.SetStatusType(StatusType.ErrorDisconnect);
                        MessageHelpers.SetMessage(stream, m);
                        return;
                    }
                    if (m.Content != sPassword)
                    {
                        m = new Message("Server", MessageType.Status);
                        m.SetStatusType(StatusType.ErrorDisconnect);
                        m.SetContent("Incorrect Password");
                        MessageHelpers.SetMessage(stream, m);
                    }
                    else
                    {
                        if (Clients.Any(x => x.Value.Name == m.Name))
                        {
                            m = new Message("Server", MessageType.Status);
                            m.SetStatusType(StatusType.ErrorDisconnect);
                            m.SetContent($"User with name {m.Name} alread exsites");
                            MessageHelpers.SetMessage(stream, m);
                        }
                        else
                        {
                            ClientInfo c = new ClientInfo(m.Name, stream, ClientType.Desktop, addr);
                            Clients.Add(c.GUID, c);
                            ThreadPool.QueueUserWorkItem(DesktopWorker, c);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message + " in " + e.TargetSite + " at line " + GetLineNumber(e).ToString());
                client.GetStream().Close();
                return;
            }
        }

        // Line Getter
        static int GetLineNumber(Exception e)
        {
            var st = new StackTrace(e, true);
            var frame = st.GetFrame(0);
            return frame.GetFileLineNumber();
        }

        // Workers to handle incoming/outgoing data
        static void WebWorker(object state)
        {
            ClientInfo client = (ClientInfo)state;

            Room r = Rooms.Where(x => x.Value.ID == DefaultRoomID).First().Value;

            JsonMessage rooms = new JsonMessage("Server", MessageType.Request);
            rooms.SetContent(Rooms.SerializeRooms());
            rooms.SetRequestType(RequestType.Rooms);
            SendJsonMessage(client, rooms);

            JsonMessage connect = new JsonMessage(client.Name, MessageType.Status);
            connect.SetStatusType(StatusType.Connected);
            SendToAll(client, connect);

            OnUserConnectCallback?.Invoke(client);

            if (!r.AddUser(client))
            {
                try
                {
                    r = Rooms.Where(x => !x.Value.IsFull).First().Value;
                    r.AddUser(client);
                }
                catch
                {
                    JsonMessage error = new JsonMessage("error", MessageType.Status);
                    error.SetStatusType(StatusType.ErrorDisconnect);
                    error.SetContent("No avaible rooms");
                    if (Clients.ContainsKey(client.GUID))
                    {
                        r.RemoveUser(client);
                        Clients.Remove(client.GUID);
                    }

                    if (client.IsSecure)
                    {
                        if (client.SStream != null)
                        {
                            JsonMessageHelpers.SetJsonMessage(client.SStream, error);
                            client.SStream.Close();
                        }
                    }
                    else
                    {
                        client.Stream.Close();
                        if (client.Stream != null)
                        {
                            JsonMessageHelpers.SetJsonMessage(client.Stream, error);
                            client.Stream.Close();
                        }
                    }
                    return;
                }
            }

            while (true)
            {
                try
                {
                    JsonMessage m = client.IsSecure ? JsonMessageHelpers.GetJsonMessage(client.SStream) : JsonMessageHelpers.GetJsonMessage(client.Stream);
                    m.SetContent(m.Content.Replace("<", "&lt;").Replace(">", "&gt;"));

                    // Send to bots for proccesing
                    var results = OnJsonMessageSentCallback?.GetInvocationList().Select(x => x.DynamicInvoke(m, client)).ToArray();
                    if(results.Any(x => (MessageState)x == MessageState.Terminate))
                    {
                        continue;
                    }

                    if (m.Name != client.Name)
                    {
                        m.SetName(client.Name);
                    }

                    if (m.MessageType == MessageType.Message)
                    {
                        r.AddMesssage(m.ToMessage());
                    }
                    if (m.MessageType == MessageType.Status)
                    {
                        switch (m.StatusType)
                        {
                            case StatusType.ChangeRoom:
                                if (int.TryParse(m.Content, out int Result))
                                {
                                    Room old = r;
                                    r = Rooms.Where(x => x.Value.ID == Result).First().Value;
                                    if (!r.IsFull)
                                    {
                                        OnUserChangeRoomCallback?.Invoke(client, old, r);
                                        old.RemoveUser(client);
                                        r.AddUser(client);
                                        client.SetRoomID(r.ID);
                                        JsonMessage message = new JsonMessage("Server", MessageType.Message);
                                        message.SetContent($"Move to room {r.Name}({r.ID})");
                                        message.SetColor(NColor.FromRGB(0, 255, 0));
                                        SendJsonMessage(client, message);
                                        break;
                                    }
                                    else
                                    {
                                        JsonMessage message = new JsonMessage("Server", MessageType.Message);
                                        message.SetContent($"Room {r.Name}({r.ID}) is full");
                                        message.SetColor(NColor.FromRGB(0, 255, 0));
                                        SendJsonMessage(client, message);
                                        break;
                                    }
                                }
                                else
                                {
                                    JsonMessage message = new JsonMessage("Server", MessageType.Message);
                                    message.SetContent($"Unknown Room");
                                    message.SetColor(NColor.FromRGB(0, 255, 0));
                                    SendJsonMessage(client, message);
                                    break;
                                }
                            case StatusType.Disconnecting:
                                if (client.IsSecure)
                                {
                                    client.SStream.Close();
                                }
                                else
                                {
                                    client.Stream.Close();
                                }

                                r.RemoveUser(client);
                                Clients.Remove(client.GUID);

                                JsonMessage json = new JsonMessage(client.Name, MessageType.Status);
                                json.SetStatusType(StatusType.Disconnected);

                                SendToAll(client, json, true);
                                OnUserDisconnectCallback?.Invoke(client);
                                break;
                            default:
                                break;
                        }
                        continue;
                    }
                    else if (m.MessageType == MessageType.Whisper)
                    {
                        SendToAll(client, m, true);
                        continue;
                    }

                    SendToAll(client, m);
                }
                catch (Exception e)
                {
                    OnUserDisconnectCallback?.Invoke(client);
                    JsonMessage error = new JsonMessage("error", MessageType.Status);
                    error.SetStatusType(StatusType.ErrorDisconnect);
                    error.SetContent(e.Message);
                    if (Clients.ContainsKey(client.GUID))
                    {
                        r.RemoveUser(client);
                        Clients.Remove(client.GUID);
                    }

                    if (client.IsSecure)
                    {
                        if (client.SStream != null)
                        {
                            JsonMessageHelpers.SetJsonMessage(client.SStream, error);
                            client.SStream.Close();
                        }
                    }
                    else
                    {
                        client.Stream.Close();
                        if (client.Stream != null)
                        {
                            JsonMessageHelpers.SetJsonMessage(client.Stream, error);
                            client.Stream.Close();
                        }
                    }
                    break;
                }
            }
        }

        static void DesktopWorker(object state)
        {
            ClientInfo client = (ClientInfo)state;

            Room r = Rooms.Where(x => x.Value.ID == DefaultRoomID).First().Value;

            Message rooms = new Message("Server", MessageType.Request);
            rooms.SetContent(Rooms.SerializeRooms());
            rooms.SetRequestType(RequestType.Rooms);
            SendMessage(client, rooms);

            Message connect = new Message(client.Name, MessageType.Status);
            connect.SetStatusType(StatusType.Connected);
            SendToAll(client, connect);
            OnUserConnectCallback?.Invoke(client);

            if (!r.AddUser(client))
            {
                try
                {
                    r = Rooms.Where(x => !x.Value.IsFull).First().Value;
                    r.AddUser(client);
                }
                catch
                {
                    Message error = new Message("error", MessageType.Status);
                    error.SetStatusType(StatusType.ErrorDisconnect);
                    error.SetContent("No avaible rooms");
                    if (Clients.ContainsKey(client.GUID))
                    {
                        r.RemoveUser(client);
                        Clients.Remove(client.GUID);
                    }

                    if (client.IsSecure)
                    {
                        if (client.SStream != null)
                        {
                            MessageHelpers.SetMessage(client.SStream, error);
                            client.SStream.Close();
                        }
                    }
                    else
                    {
                        client.Stream.Close();
                        if (client.Stream != null)
                        {
                            MessageHelpers.SetMessage(client.Stream, error);
                            client.Stream.Close();
                        }
                    }
                    return;
                }
            }

            while (true)
            {
                try
                {
                    Message m = client.IsSecure ? MessageHelpers.GetMessage(client.SStream) : MessageHelpers.GetMessage(client.Stream);
                    m.SetContent(m.Content.Replace("<", "&lt;").Replace(">", "&gt;"));

                    // Send to bots for proccesing
                    var results = OnMessageSentCallback?.GetInvocationList().Select(x => x.DynamicInvoke(m, client)).ToArray();
                    if (results.Any(x => (MessageState)x == MessageState.Terminate))
                    {
                        continue;
                    }

                    if (m.Name != client.Name)
                    {
                        m.SetName(client.Name);
                    }

                    if (m.MessageType == MessageType.Message)
                    {
                        r.AddMesssage(m);
                    }
                    if (m.MessageType == MessageType.Status)
                    {
                        switch (m.StatusType)
                        {
                            case StatusType.ChangeRoom:
                                if (int.TryParse(m.Content, out int Result))
                                {
                                    Room old = r;
                                    r = Rooms.Where(x => x.Value.ID == Result).First().Value;
                                    if (!r.IsFull)
                                    {
                                        OnUserChangeRoomCallback?.Invoke(client, old, r);
                                        r.AddUser(client);
                                        client.SetRoomID(r.ID);
                                        Message message = new Message("Server", MessageType.Message);
                                        message.SetContent($"Move to room {r.Name}({r.ID})");
                                        message.SetColor(NColor.FromRGB(0, 255, 0));
                                        SendMessage(client, message);
                                        break;
                                    }
                                    else
                                    {
                                        Message message = new Message("Server", MessageType.Message);
                                        message.SetContent($"Room {r.Name}({r.ID}) is full");
                                        message.SetColor(NColor.FromRGB(0, 255, 0));
                                        SendMessage(client, message);
                                        break;
                                    }
                                }
                                else
                                {
                                    Message message = new Message("Server", MessageType.Message);
                                    message.SetContent($"Unknown Room");
                                    message.SetColor(NColor.FromRGB(0, 255, 0));
                                    SendMessage(client, message);
                                    break;
                                }
                            case StatusType.Disconnecting:
                                if (client.IsSecure)
                                {
                                    client.SStream.Close();
                                }
                                else
                                {
                                    client.Stream.Close();
                                }

                                r.RemoveUser(client);
                                Clients.Remove(client.GUID);

                                Message json = new Message(client.Name, MessageType.Status);
                                json.SetStatusType(StatusType.Disconnected);

                                SendToAll(client, json, true);
                                OnUserDisconnectCallback?.Invoke(client);
                                break;
                            default:
                                break;
                        }
                        continue;
                    }
                    else if (m.MessageType == MessageType.Whisper)
                    {
                        SendToAll(client, m, true);
                        continue;
                    }

                    SendToAll(client, m);
                }
                catch (Exception e)
                {
                    OnUserDisconnectCallback?.Invoke(client);
                    Message error = new Message("error", MessageType.Status);
                    error.SetStatusType(StatusType.ErrorDisconnect);
                    error.SetContent(e.Message);
                    if (Clients.ContainsKey(client.GUID))
                    {
                        r.RemoveUser(client);
                        Clients.Remove(client.GUID);
                    }

                    if (client.IsSecure)
                    {
                        if (client.SStream != null)
                        {
                            MessageHelpers.SetMessage(client.SStream, error);
                            client.SStream.Close();
                        }
                    }
                    else
                    {
                        client.Stream.Close();
                        if (client.Stream != null)
                        {
                            MessageHelpers.SetMessage(client.Stream, error);
                            client.Stream.Close();
                        }
                    }
                    break;
                }
            }
        }

        // Helpers
        static void SendJsonMessage(ClientInfo client, JsonMessage m)
        {
            if (client.IsSecure)
            {
                JsonMessageHelpers.SetJsonMessage(client.SStream, m);
                return;
            }
            JsonMessageHelpers.SetJsonMessage(client.Stream, m);
        }

        static void SendMessage(ClientInfo client, Message m)
        {
            if (client.IsSecure)
            {
                MessageHelpers.SetMessage(client.SStream, m);
                return;
            }
            MessageHelpers.SetMessage(client.Stream, m);
        }

        // Server Configuration loading
        static void LoadConfig()
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("Loading Configuration...");
            XmlDocument settings = new XmlDocument();
            settings.Load("config.xml");
            XmlNode roomsNode = settings.SelectSingleNode("Config/Rooms");
            XmlNodeList rooms = roomsNode.SelectNodes("Room");
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            if (roomsNode.Attributes[0].InnerText == "true")
            {
                Console.WriteLine("Overriden Default Room(s)");
                Rooms = new Dictionary<string, Room>();
            }
            
            if(!int.TryParse(roomsNode.Attributes[1].InnerText, out DefaultRoomID))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Failed to read defaultRoomID, reverting to default (Room id 0)");
            }

            foreach (XmlNode node in rooms)
            {
                try
                {
                    XmlAttributeCollection attr = node.Attributes;
                    Console.WriteLine("Generating Room " + node.InnerText);
                    Room m = new Room(node.InnerText, int.Parse(attr[0].InnerText), int.Parse(attr[1].InnerText), int.Parse(attr[2].InnerText));
                    Rooms[m.GUID] = m;
                }
                catch
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Failed to Generate Room " + node.InnerText);
                    Console.ForegroundColor = ConsoleColor.DarkCyan;
                }
            }
            string webPort = settings.SelectSingleNode("Config/GeneralSettings/WebPort").InnerText;
            if (webPort != "")
            {
                if (!int.TryParse(webPort, out WebPort))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Failed to read WebPort, reverting to default port (8911)");
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.DarkCyan;
                    Console.WriteLine("Set WebPort to " + WebPort);
                }
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.DarkCyan;
                Console.WriteLine("WebPort set to default (8911)");
            }
            string desktopPort = settings.SelectSingleNode("Config/GeneralSettings/DesktopPort").InnerText;
            if (desktopPort != "")
            {
                if (!int.TryParse(desktopPort, out DesktopPort))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Failed to read DesktopPort, reverting to default port (8910)");
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.DarkCyan;
                    Console.WriteLine("Set DesktopPort to " + DesktopPort);
                }
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.DarkCyan;
                Console.WriteLine("DesktopPort set to default (8910)");
            }
            string ip = settings.SelectSingleNode("Config/GeneralSettings/ServerIp").InnerText;
            if (ip != "")
            {
                if (!IPAddress.TryParse(ip, out IPAddress))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Failed to read ServerIp, reverting to default (Automatic " + GetLocalIPAddress() + ")");
                    IPAddress = IPAddress.Parse(GetLocalIPAddress());
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.DarkCyan;
                    Console.WriteLine("Set ServerIp to " + IPAddress.ToString());
                }
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.DarkCyan;
                Console.WriteLine("Automatically setting ip to " + GetLocalIPAddress());
                IPAddress = IPAddress.Parse(GetLocalIPAddress());
            }
            try
            {
                if (!bool.TryParse(settings.SelectSingleNode("Config/GeneralSettings/WebPort").Attributes[0].InnerText, out WebActive))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Failed to read WebPort active attribute");
                }
                if (!bool.TryParse(settings.SelectSingleNode("Config/GeneralSettings/DesktopPort").Attributes[0].InnerText, out DesktopActive))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Failed to read WebPort active attribute");
                }
            }
            catch
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Failed to read active states");
            }
            string sslPath = settings.SelectSingleNode("Config/GeneralSettings/PfxCertificate").InnerText;
            if (sslPath != "")
            {
                try
                {
                    X509 = new X509Certificate2(sslPath, settings.SelectSingleNode("Config/GeneralSettings/PfxCertificate").Attributes[0].InnerText);
                    Console.ForegroundColor = ConsoleColor.DarkCyan;
                    Console.WriteLine("Loaded Pfx Certificate, starting ssl");
                }
                catch (Exception e)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Failed to load Pfx Certificate, reverting to non-ssl");
                    Console.WriteLine(e.Message);
                }
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.DarkCyan;
                Console.WriteLine("No Pfx Certificate provided, reverting to non-ssl");
            }

            if (Rooms.Count == 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("[Server not started] Please add atleast one room to the server or set overrideDefault to false. Press enter to quit.");
                Console.ReadLine();
                return;
            }
            if (!DesktopActive && !WebActive)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("[Server not started] Please set atleast one server to active (Web or Desktop). Press enter to quit.");
                Console.ReadLine();
                return;
            }
            string unhash = settings.SelectSingleNode("Config/GeneralSettings/ServerPassword").InnerText;
            SHA256 sha = SHA256.Create();
            byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(unhash));
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < hash.Length; i++)
            {
                builder.Append(hash[i].ToString("x2"));
            }
            sPassword = builder.ToString();
            if (unhash != "")
            {
                HasPassword = true;
                Console.ForegroundColor = ConsoleColor.DarkCyan;
                Console.WriteLine("Password Set");
            }

            try
            {
                string[] ips = File.ReadAllLines("banned.txt");
                BanList.AddRange(ips.Select(x => IPAddress.Parse(x)));

                Console.ForegroundColor = ConsoleColor.DarkCyan;
                Console.WriteLine("Banned Loaded");
            }
            catch
            {
                Console.ForegroundColor = ConsoleColor.DarkCyan;
                Console.WriteLine("Banned Not Loaded");
            }

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("Complete!");
            Console.ForegroundColor = ConsoleColor.White;
        }

        // Server Bot loading
        static void LoadBots()
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("Loading Bots...");

            BotHandler = new BotHandler();
            BotHandler.OnReqestClients += () => { return Clients; };
            BotHandler.OnUpdateClient += (c) => { Clients[c.GUID] = c; };
            BotHandler.OnUpdateRoom += (r) => { Rooms[r.GUID] = r; };
            BotHandler.OnRequestRooms += () => { return Rooms; }; 

            foreach (string dll in Directory.GetDirectories(@"Bots"))
            {
                string path = Directory.GetParent(dll).FullName;
                path = path + @"\" + new DirectoryInfo(dll).Name + @"\" + new DirectoryInfo(dll).Name + ".dll";
                if (!File.Exists(path)) { continue; }

                if (!File.Exists(path.Replace(".dll", ".pdb")))
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("No .pdb file found for \n" + path);
                    Console.WriteLine("Exact location of error is impossible to determine");
                    Console.ForegroundColor = ConsoleColor.White;
                }
                Assembly DLL = Assembly.LoadFrom(path);
                Type[] types = DLL.GetExportedTypes();
                foreach (Type type in types)
                {
                    Bot bot = (Bot)Attribute.GetCustomAttribute(type, typeof(Bot));
                    if (bot != null)
                    {
                        Console.ForegroundColor = ConsoleColor.Blue;
                        Console.WriteLine($"{bot.Name} By {bot.Creator}");
                        Console.ForegroundColor = ConsoleColor.Gray;
                        Console.WriteLine(bot.Desc);
                        Console.ForegroundColor = ConsoleColor.Blue;
                        Console.WriteLine($"Version: {bot.Version}");
                        Console.ForegroundColor = ConsoleColor.White;
                        dynamic c = Activator.CreateInstance(type, BotHandler);
                        try
                        {
                            try
                            {
                                OnJsonMessageSentCallback += (m, s) => { return c.OnJsonMessage(m, s); };
                                OnMessageSentCallback += (m, s) => { return c.OnMessage(m, s); };
                                OnUserChangeRoomCallback += (cl, o, r) => { c.OnUserChangeRoom(cl, o, r); };
                                OnUserConnectCallback += (cl) => { c.OnUserConnect(cl); };
                                OnUserDisconnectCallback += (cl) => { c.OnUserDisconnect(cl); };
                            }
                            catch (RuntimeBinderException e)
                            {
                                Console.ForegroundColor = ConsoleColor.DarkRed;
                                Console.WriteLine("Failed to load " + path);
                                Console.WriteLine("(" + e.Message + ") thrown in " + e.TargetSite.Name + " at line " + GetLineNumber(e));
                                Console.ForegroundColor = ConsoleColor.White;
                                continue;
                            }
                            catch (Exception e)
                            {
                                Console.ForegroundColor = ConsoleColor.DarkRed;
                                Console.WriteLine("Failed to load " + path);
                                Console.WriteLine("(" + e.Message + ") thrown in " + e.TargetSite.Name + " at line " + GetLineNumber(e));
                                Console.ForegroundColor = ConsoleColor.White;
                                continue;
                            }
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine("Sucessfully loaded!");
                            Console.ForegroundColor = ConsoleColor.White;
                        }
                        catch (Exception e)
                        {
                            Console.ForegroundColor = ConsoleColor.DarkRed;
                            Console.WriteLine("Failed to load " + path);
                            Console.WriteLine("Method Init Missing or Incorrect or other error has occured");
                            Console.WriteLine("(" + e.Message + ") thrown in " + e.TargetSite.Name + " at line " + GetLineNumber(e));
                            Console.ForegroundColor = ConsoleColor.White;
                            continue;
                        }
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.DarkMagenta;
                        Console.WriteLine("Failed to load " + path);
                        Console.WriteLine("File doesnt contain Bot Attribute/Code");
                        Console.ForegroundColor = ConsoleColor.White;
                    }
                }
            }
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("Complete!");
            Console.ForegroundColor = ConsoleColor.White;
        }

        static string GetLocalIPAddress()
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

        // Helpers
        static async void SendToAll(ClientInfo sender, Message m, bool ignoreUserRoom = false)
        {
            List<Task> tasks = new List<Task>();
            foreach (ClientInfo client in Clients.Where(x => (x.Value.RoomId == sender.RoomId && !ignoreUserRoom) || ignoreUserRoom).Select(x => x.Value).ToArray())
            {
                if(client.GUID == sender.GUID)
                {
                    continue;
                }
                if (m.EndPoint == "")
                {
                    if (client.ClientType == ClientType.Web)
                    {
                        if (client.IsSecure)
                        {
                            tasks.Add(JsonMessageHelpers.SetJsonMessageAsync(client.SStream, m.ToJsonMessage()));
                        }
                        else
                        {
                            tasks.Add(JsonMessageHelpers.SetJsonMessageAsync(client.Stream, m.ToJsonMessage()));
                        }
                    }
                    else
                    {
                        if (client.IsSecure)
                        {
                            tasks.Add(MessageHelpers.SetMessageAsync(client.SStream, m));
                        }
                        else
                        {
                            tasks.Add(MessageHelpers.SetMessageAsync(client.Stream, m));
                        }
                    }
                }
                else if (client.Name == m.EndPoint)
                {
                    if (client.ClientType == ClientType.Web)
                    {
                        if (client.IsSecure)
                        {
                            tasks.Add(JsonMessageHelpers.SetJsonMessageAsync(client.SStream, m.ToJsonMessage()));
                        }
                        else
                        {
                            tasks.Add(JsonMessageHelpers.SetJsonMessageAsync(client.Stream, m.ToJsonMessage()));
                        }
                    }
                    else
                    {
                        if (client.IsSecure)
                        {
                            tasks.Add(MessageHelpers.SetMessageAsync(client.SStream, m));
                        }
                        else
                        {
                            tasks.Add(MessageHelpers.SetMessageAsync(client.Stream, m));
                        }
                    }
                    return;
                }
            }

            while (tasks.Any())
            {
                tasks.Remove(await Task.WhenAny(tasks));
            }
        }

        static async void SendToAll(ClientInfo sender, JsonMessage m, bool ignoreUserRoom = false)
        {
            List<Task> tasks = new List<Task>();
            foreach (ClientInfo client in Clients.Where(x => (x.Value.RoomId == sender.RoomId && !ignoreUserRoom) || ignoreUserRoom).Select(x => x.Value).ToArray())
            {
                if (client.GUID == sender.GUID)
                {
                    continue;
                }
                if (m.EndPoint == "")
                {
                    if (client.ClientType == ClientType.Web)
                    {
                        if (client.IsSecure)
                        {
                            tasks.Add(JsonMessageHelpers.SetJsonMessageAsync(client.SStream, m));
                        }
                        else
                        {
                            tasks.Add(JsonMessageHelpers.SetJsonMessageAsync(client.Stream, m));
                        }
                    }
                    else
                    {
                        if (client.IsSecure)
                        {
                            tasks.Add(MessageHelpers.SetMessageAsync(client.SStream, m.ToMessage()));
                        }
                        else
                        {
                            tasks.Add(MessageHelpers.SetMessageAsync(client.Stream, m.ToMessage()));
                        }
                    }
                }
                else if (client.Name == m.EndPoint)
                {
                    if (client.ClientType == ClientType.Web)
                    {
                        if (client.IsSecure)
                        {
                            tasks.Add(JsonMessageHelpers.SetJsonMessageAsync(client.SStream, m));
                        }
                        else
                        {
                            tasks.Add(JsonMessageHelpers.SetJsonMessageAsync(client.Stream, m));
                        }
                    }
                    else
                    {
                        if (client.IsSecure)
                        {
                            tasks.Add(MessageHelpers.SetMessageAsync(client.SStream, m.ToMessage()));
                        }
                        else
                        {
                            tasks.Add(MessageHelpers.SetMessageAsync(client.Stream, m.ToMessage()));
                        }
                    }
                }
            }

            while (tasks.Any())
            {
                tasks.Remove(await Task.WhenAny(tasks));
            }
        }
    }
}
