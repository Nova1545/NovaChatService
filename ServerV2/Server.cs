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

        static int WebPort = 8911;
        static int DefaultRoomID = 0;
        static int DesktopPort = 8910;
        static bool WebActive = false;
        static bool DesktopActive = false;
        static IPAddress IPAddress = null;
        static X509Certificate2 X509 = null;
        static string sPassword = "";

        static void Main(string[] args)
        {
            Clients = new Dictionary<string, ClientInfo>();
            Rooms = new Dictionary<string, Room>();
            BanList = new List<IPAddress>();

            LoadConfig();
            Console.WriteLine(X509.SubjectName.Name.Replace("CN=", ""));

            if (WebActive)
            {
                TcpListener WebServer = new TcpListener(IPAddress, WebPort);
                WebServer.Start();
                WebServer.BeginAcceptTcpClient(new AsyncCallback(OnWebAccept), WebServer);
            }

            if (DesktopActive)
            {
                TcpListener DesktopServer = new TcpListener(IPAddress, DesktopPort);
                DesktopServer.Start();
                DesktopServer.BeginAcceptTcpClient(new AsyncCallback(OnDesktopAccept), DesktopServer);
            }

            bool run = true;
            while (run)
            {
                string[] command = Console.ReadLine().Split(' ');
                if (command[0] == "rooms")
                {
                    foreach (KeyValuePair<string, Room> room in Rooms)
                    {
                        Console.WriteLine(room.ToString());
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

                        if (client.IsSecure)
                        {
                            if (client.SStream != null)
                            {
                                if (client.ClientType == ClientType.Web)
                                {
                                    JsonMessageHelpers.SetJsonMessage(client.SStream, error.ToJsonMessage());
                                }
                                else
                                {
                                    MessageHelpers.SetMessage(client.SStream, error);
                                }
                                client.SStream.Close();
                            }
                        }
                        else
                        {
                            if (client.Stream != null)
                            {
                                if (client.ClientType == ClientType.Web)
                                {
                                    JsonMessageHelpers.SetJsonMessage(client.Stream, error.ToJsonMessage());
                                }
                                else
                                {
                                    MessageHelpers.SetMessage(client.Stream, error);
                                }
                                client.Stream.Close();
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

                    if (json.MessageType == MessageType.Initionalize && Clients.Any(x => x.Value.Name == json.Name) == false)
                    {
                        ClientInfo c = new ClientInfo(json.Name, stream, ClientType.Web, addr);
                        Clients.Add(c.GUID, c);
                        ThreadPool.QueueUserWorkItem(WebWorker, c);
                    }
                    else
                    {
                        JsonMessage h = new JsonMessage(json.Name, MessageType.Status);
                        h.SetStatusType(StatusType.ErrorDisconnect);
                        h.SetContent("User with the name " + json.Name + " already exsists");
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

                        if (json.MessageType == MessageType.Initionalize && Clients.Any(x => x.Value.Name == json.Name) == false)
                        {
                            ClientInfo c = new ClientInfo(json.Name, stream, ClientType.Web, addr);
                            Clients.Add(c.GUID, c);
                            ThreadPool.QueueUserWorkItem(WebWorker, c);
                        }
                        else
                        {
                            JsonMessage h = new JsonMessage(json.Name, MessageType.Status);
                            h.SetStatusType(StatusType.ErrorDisconnect);
                            h.SetContent("User with the name " + json.Name + " already exsists");
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
            DesktopServer.BeginAcceptTcpClient(new AsyncCallback(OnDesktopAccept), DesktopServer);

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

                    Message secure = new Message("Server", MessageType.Initionalize);
                    secure.SetContent("");
                    MessageHelpers.SetMessage(stream, secure);

                    Message json = MessageHelpers.GetMessage(stream);
                    if (json.Content != sPassword)
                    {
                        Message h = new Message(json.Name, MessageType.Status);
                        h.SetStatusType(StatusType.ErrorDisconnect);
                        h.SetContent("Incorrect Password");
                        MessageHelpers.SetMessage(stream, h);
                        stream.Close();
                        stream.Dispose();
                        client.Close();
                        client.Dispose();
                        return;
                    }

                    if (json.MessageType == MessageType.Initionalize && Clients.Any(x => x.Value.Name == json.Name) == false)
                    {
                        ClientInfo c = new ClientInfo(json.Name, stream, ClientType.Web, addr);
                        Clients.Add(c.GUID, c);
                        ThreadPool.QueueUserWorkItem(DesktopWorker, c.GUID);
                    }
                    else
                    {
                        Message h = new Message(json.Name, MessageType.Status);
                        h.SetStatusType(StatusType.ErrorDisconnect);
                        h.SetContent("User with the name " + json.Name + " already exsists");
                        MessageHelpers.SetMessage(stream, h);
                        stream.Close();
                        stream.Dispose();
                        client.Close();
                        client.Dispose();
                    }
                }
                else
                {
                    //Message secure = new Message("Server", MessageType.Initionalize);
                    //secure.SetContent(X509.SubjectName.Name.Replace("CN=", ""));
                    //MessageHelpers.SetMessage(client.GetStream(), secure);
                    //Console.WriteLine("Sent secure status");

                    SslStream stream = new SslStream(client.GetStream());
                    stream.AuthenticateAsServer(X509, false, SslProtocols.Default, true);

                    Message json = MessageHelpers.GetMessage(stream);
                    if (json.Content != sPassword)
                    {
                        Message h = new Message(json.Name, MessageType.Status);
                        h.SetStatusType(StatusType.ErrorDisconnect);
                        h.SetContent("Incorrect Password");
                        MessageHelpers.SetMessage(stream, h);
                        stream.Close();
                        stream.Dispose();
                        client.Close();
                        client.Dispose();
                        return;
                    }

                    if (json.MessageType == MessageType.Initionalize && Clients.Any(x => x.Value.Name == json.Name) == false)
                    {
                        ClientInfo c = new ClientInfo(json.Name, stream, ClientType.Web, addr);
                        Clients.Add(c.GUID, c);
                        ThreadPool.QueueUserWorkItem(DesktopWorker, c.GUID);
                        Console.WriteLine("Starting worker");
                    }
                    else
                    {
                        Message h = new Message(json.Name, MessageType.Status);
                        h.SetStatusType(StatusType.ErrorDisconnect);
                        h.SetContent("User with the name " + json.Name + " already exsists");
                        MessageHelpers.SetMessage(stream, h);
                        stream.Close();
                        stream.Dispose();
                        client.Close();
                        client.Dispose();
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
                    JsonMessage m = client.IsSecure ? JsonMessageHelpers.GetJsonMessage(client.SStream) : JsonMessageHelpers.GetJsonMessage(client.Stream);

                    m.SetContent(m.Content.Replace("<", "&lt;").Replace(">", "&gt;"));

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
                                    r = Rooms.Where(x => x.Value.ID == Result).First().Value;
                                    if (!r.IsFull)
                                    {
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
                Console.WriteLine("Automaticly setting ip to " + GetLocalIPAddress());
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
