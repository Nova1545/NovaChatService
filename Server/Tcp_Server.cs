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

namespace Server
{
    class Tcp_Server
    {
        static Dictionary<string, ClientInfo> clients;
        static Dictionary<string, Room> Rooms;
        static List<dynamic> Bots;
        static BotHandler handler;

        static int WebPort = 8911;
        static int DesktopPort = 8910;

        static IPAddress iPAddress = null;
        static int TotalMessagesSent = 0;
        static DateTime startup;

        static void Main(string[] args)
        {
            clients = new Dictionary<string, ClientInfo>();
            Rooms = new Dictionary<string, Room>();

            Room m = new Room("Main", 0);
            Rooms[m.GUID] = m;
            m = new Room("Extra", 1);

            #region Config loading code
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
            foreach (XmlNode node in rooms)
            {
                try
                {
                    XmlAttributeCollection attr = node.Attributes;
                    Console.WriteLine("Generating Room " + node.InnerText);
                    m = new Room(node.InnerText, int.Parse(attr[0].InnerText), int.Parse(attr[1].InnerText), int.Parse(attr[2].InnerText));
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
            if (desktopPort != "") {
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
            if (ip != "") {
                if (!IPAddress.TryParse(ip, out iPAddress))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Failed to read ServerIp, reverting to default (Automatic " + GetLocalIPAddress() + ")");
                    iPAddress = IPAddress.Parse(GetLocalIPAddress());
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.DarkCyan;
                    Console.WriteLine("Set ServerIp to " + iPAddress.ToString());
                }
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.DarkCyan;
                Console.WriteLine("Automaticly setting ip to " + GetLocalIPAddress());
                iPAddress = IPAddress.Parse(GetLocalIPAddress());
            }
            if(Rooms.Count == 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Please add atleast one room to the server or set overrideDefault to false. Press enter to quit.");
                Console.ReadLine();
                return;
            }
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("Complete!");
            #endregion

            Bots = new List<dynamic>();
            handler = new BotHandler();
            handler.OnReqestClients += () => { return clients; };
            handler.OnUpdateClient += (c) => { clients[c.GUID] = c; };
            handler.OnUpdateRoom += (r) => { Rooms[r.GUID] = r; };

            startup = DateTime.UtcNow;

            // Bot Loader
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("Loading Bots...");
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
                        dynamic c = Activator.CreateInstance(type);
                        try
                        {
                            try
                            {
                                c.Init(handler);
                                Bots.Add(c);
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

            Thread web = new Thread(() => WebListener())
            {
                IsBackground = true
            };
            web.Start();

            Thread desktop = new Thread(() => ClientListener())
            {
                IsBackground = true
            };
            desktop.Start();

            Console.WriteLine("Type quit to stop server or help for a list of commands");
            while (true)
            {
                string[] command = Console.ReadLine().Split(' ');
                if(command[0] == "quit")
                {
                    break;
                }
                else if(command[0] == "log")
                {
                    if(command[1] == "users")
                    {
                        Console.WriteLine("Users:");
                        foreach (KeyValuePair<string, ClientInfo> client in clients)
                        {
                            Console.WriteLine(client.ToString());
                        }
                    }
                    //else if(command[1] == "rooms")
                    //{
                    //    Console.WriteLine("Rooms:");
                    //    foreach (Room room in Rooms)
                    //    {
                    //        Console.WriteLine(room.ToString());
                    //    }
                    //}
                }
                else if(command[0] == "help")
                {
                    Console.WriteLine("Coming soon");
                }
                //else if (command[0] == "addrm")
                //{
                //    Rooms.Add(new Room(command[1], int.Parse(command[2]), int.Parse(command[3]), int.Parse(command[4])));
                //    Console.WriteLine("Added room");
                //}
                //else if (command[0] == "removerm")
                //{
                //    Rooms.Remove(Rooms.Where(x => x.Name == command[1]).First());
                //    Console.WriteLine("Removed " + command[1]);
                //}
            }
        }

        static int GetLineNumber(Exception e)
        {
            var st = new StackTrace(e, true);
            var frame = st.GetFrame(0);
            return frame.GetFileLineNumber();
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

                if (json.MessageType == MessageType.Initionalize && clients.Any(x => x.Value.Name == json.Name) == false)
                {
                    ClientInfo c = new ClientInfo(json.Name, stream, ClientType.Web);
                    clients.Add(c.GUID, c);
                    ThreadPool.QueueUserWorkItem(HandleClientWebWorker, new object[2] { client, c });
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

        static void ClientListener()
        {
            TcpListener server = new TcpListener(iPAddress, 8910);
            server.Start();
            while (true)
            {
                TcpClient client = server.AcceptTcpClient();

                NetworkStream stream = client.GetStream();

                //SslStream ssl = new SslStream(client.GetStream(), false);
                //ssl.AuthenticateAsServer(X509, false, true);

                Message message = MessageHelpers.GetMessage(stream);
                if (message.MessageType == MessageType.Initionalize && clients.Any(x => x.Value.Name == message.Name) == false)
                {
                    ClientInfo c = new ClientInfo(message.Name, stream, ClientType.Desktop);
                    clients.Add(c.GUID, c);
                    ThreadPool.QueueUserWorkItem(HandleClientDesktopWorker, c);
                }
                else
                {
                    Message h = new Message(message.Name, MessageType.Status);
                    h.SetStatusType(StatusType.ErrorDisconnect);
                    h.SetContent("User with the name " + message.Name + " already exsists");
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
            ClientInfo c = (ClientInfo)token;
            NetworkStream stream = c.Stream;

            // Let bots know there is a new user
            foreach (dynamic b in Bots)
            {
                b.OnUserConnect(c);
            }

            // Tell all connected clients that we are connected
            Message cm = new Message(c.Name, MessageType.Status);
            cm.SetStatusType(StatusType.Connected);
            SendToAllClients(cm, null, -1);

            Room r = new Room();

            // Put user in none full room
            foreach (KeyValuePair<string, Room> room in Rooms)
            {
                if (!room.Value.AddUser(c))
                {
                    continue;
                }
                else
                {
                    r = room.Value;
                    break;
                }
            }
            if (r.Equals(default(Room)))
            {
                JsonMessage m = new JsonMessage("Server", MessageType.Status);
                m.SetStatusType(StatusType.ErrorDisconnect);
                m.SetContent("All avaible rooms are full");
                JsonMessageHelpers.SetJsonMessage(stream, m);
                return;
            }

            // Send The Server Message Buffer
            foreach (Message message in r.Buffer)
            {
                MessageHelpers.SetMessage(stream, message);
            }

            while (true)
            {
                try
                {
                    c = clients[c.GUID];
                    Message m = MessageHelpers.GetMessage(stream);
                    foreach (dynamic b in Bots)
                    {
                        m = b.OnMessage(c, m);
                    }

                    // Manage Message buffer
                    if (m.MessageType == MessageType.Message)
                    {
                        r.AddMesssage(m);
                    }
                    else if(m.MessageType == MessageType.Infomation)
                    {
                        MessageHelpers.SetMessage(stream, InformationHandler(m.InfomationType, c.GUID));
                        continue;
                    }
                    else if(m.MessageType == MessageType.Status && m.StatusType == StatusType.ChangeRoom)
                    {
                        try
                        {
                            ClientInfo cInfo = clients[c.GUID];
                            if (!r.AddUser(cInfo))
                            {
                                m = new Message("Server", MessageType.Message);
                                m.SetColor(Color.Aquamarine);
                                m.SetContent("The requested room is full");
                                MessageHelpers.SetMessage(stream, m);
                                continue;
                            }
                            r.RemoveUser(cInfo);

                            int id = int.Parse(m.Content);
                            cInfo.SetRoomID(id);
                            clients[c.GUID] = cInfo;
                            Room oldR = r;
                            r = Rooms.Where(x => x.Value.ID == id).First().Value;
                            r.AddUser(cInfo);

                            m = new Message("Server", MessageType.Message);
                            m.SetContent("Moved to Room " + r.Name + "(" + id + ")");
                            MessageHelpers.SetMessage(stream, m);

                            foreach (Message message in r.Buffer)
                            {
                                MessageHelpers.SetMessage(stream, message);
                            }
                            foreach (dynamic b in Bots)
                            {
                                b.OnUserChangeRoom(c, oldR, r);
                            }
                            continue;
                        }
                        catch
                        {
                            try
                            {
                                ClientInfo cInfo = clients[c.GUID];
                                Room oldR = r;
                                r = Rooms.Where(x => x.Value.Name == m.Content).First().Value;
                                cInfo.SetRoomID(r.ID);
                                r.AddUser(cInfo);
                                clients[c.GUID] = cInfo;

                                m = new Message("Server", MessageType.Message);
                                m.SetContent("Moved to Room " + r.Name);
                                MessageHelpers.SetMessage(stream, m);

                                foreach (Message message in r.Buffer)
                                {
                                    MessageHelpers.SetMessage(stream, message);
                                }
                                foreach (dynamic b in Bots)
                                {
                                    b.OnUserChangeRoom(c, oldR, r);
                                }
                                continue;
                            }
                            catch { continue; }
                        }
                    }
                    if (m.Name != c.Name)
                    {
                        m.SetName(c.Name);
                    }
                    if (m.StatusType == StatusType.Disconnecting && m.MessageType == MessageType.Status)
                    {
                        foreach (dynamic b in Bots)
                        {
                            b.OnUserDisconnect(c);
                        }
                        r.RemoveUser(clients[c.GUID]);
                        MessageHelpers.SetMessage(stream, m);
                        stream.Close();
                        clients.Remove(c.GUID);

                        Message d = new Message(c.Name, MessageType.Status);
                        d.SetStatusType(StatusType.Disconnected);

                        SendToAllClients(d, null, -1);

                        break;
                    }

                    foreach (KeyValuePair<string, ClientInfo> network in clients)
                    {
                        if (network.Key == c.GUID)
                        {
                            continue;
                        }
                        else
                        {
                            if (m.EndPoint == "")
                            {
                                SendToAllClients(m, null, clients[c.GUID].RoomId, c.GUID);
                                break;
                            }
                            else
                            {
                                if (network.Value.Name == m.EndPoint)
                                {
                                    MessageHelpers.SetMessage(network.Value.Stream, m);
                                }
                            }
                        }
                    }
                }
                catch (IOException)
                {
                    if (clients.ContainsKey(c.GUID))
                    {
                        foreach (dynamic b in Bots)
                        {
                            b.OnUserDisconnect(c);
                        }
                        r.RemoveUser(clients[c.GUID]);
                        clients[c.GUID].Stream.Close();
                        clients.Remove(c.GUID);
                    }
                    foreach (KeyValuePair<string, ClientInfo> network in clients)
                    {
                        Message d = new Message(network.Key == c.Name ? "You" : c.Name, MessageType.Status);
                        d.SetStatusType(StatusType.Disconnected);
                        MessageHelpers.SetMessage(network.Value.Stream, d);
                    }
                    break;
                }
                catch (Exception ex)
                {
                    if (clients.ContainsKey(c.GUID))
                    {
                        foreach (dynamic b in Bots)
                        {
                            b.OnUserDisconnect(c);
                        }
                        r.RemoveUser(clients[c.GUID]);
                        clients[c.GUID].Stream.Close();
                        clients.Remove(c.GUID);
                    }
                    Console.WriteLine(c.Name + " disconnected due to an error. Details: " + ex.Message);
                    foreach (KeyValuePair<string, ClientInfo> network in clients)
                    {
                        Message e = new Message(network.Key == c.Name ? "You" : c.Name, MessageType.Status);
                        e.SetStatusType(StatusType.ErrorDisconnect);
                        MessageHelpers.SetMessage(network.Value.Stream, e);
                    }
                    break;
                }
            }
        }

        private static void HandleClientWebWorker(object token)
        {
            object[] d = (object[])token;
            TcpClient client = (TcpClient)d[0];
            ClientInfo c = (ClientInfo)d[1];
            NetworkStream stream = c.Stream;

            // Let bots know there is a new user
            foreach (dynamic b in Bots)
            {
                b.OnUserConnect(c);
            }

            // Tell all connected clients that we are connected
            JsonMessage cm = new JsonMessage(c.Name, MessageType.Status);
            cm.SetStatusType(StatusType.Connected);
            SendToAllClients(null, cm, -1);
            Room r = new Room();

            // Put user in none full room
            foreach (KeyValuePair<string, Room> room in Rooms)
            {
                if (!room.Value.AddUser(c))
                {
                    continue;
                }
                else
                {
                    r = room.Value;
                    break;
                }
            }
            if (r.Equals(default(Room)))
            {
                JsonMessage m = new JsonMessage("Server", MessageType.Status);
                m.SetStatusType(StatusType.ErrorDisconnect);
                m.SetContent("All avaible rooms are full");
                JsonMessageHelpers.SetJsonMessage(stream, m);
                return;
            }

            // Send The Server Message Buffer
            foreach (Message message in r.Buffer)
            {
                JsonMessageHelpers.SetJsonMessage(stream, message.ToJsonMessage());
            }

            JsonMessage rooms = new JsonMessage("Server", MessageType.Request);
            rooms.SetRequestType(RequestType.Rooms);
            rooms.SetContent(Rooms.Serialize());

            JsonMessageHelpers.SetJsonMessage(stream, rooms);

            while (true)
            {
                try
                {
                    c = clients[c.GUID];
                    if (client.Available < 3)
                    {
                        Thread.Sleep(20);
                        continue;
                    }

                    JsonMessage m = JsonMessageHelpers.GetJsonMessage(stream, client.Available);
                    foreach (dynamic b in Bots)
                    {
                        m = b.OnJsonMessage(c, m);
                    }

                    if (m.MessageType == MessageType.Message)
                    {
                        TotalMessagesSent += 1;
                        r.AddMesssage(m.ToMessage());
                    }
                    if (m.MessageType == MessageType.Infomation)
                    {
                        JsonMessageHelpers.SetJsonMessage(stream, InformationHandler(m.InfomationType, c.GUID).ToJsonMessage());
                        continue;
                    }
                    else if (m.MessageType == MessageType.Status && m.StatusType == StatusType.ChangeRoom)
                    {
                        try
                        {
                            ClientInfo cInfo = clients[c.GUID];
                            if (!r.AddUser(cInfo))
                            {
                                m = new JsonMessage("Server", MessageType.Message);
                                m.SetColor(Color.Aquamarine);
                                m.SetContent("The requested room is full");
                                JsonMessageHelpers.SetJsonMessage(stream, m);
                                continue;
                            }

                            r.RemoveUser(cInfo);
                                
                            int id = int.Parse(m.Content);
                            cInfo.SetRoomID(id);
                            clients[c.GUID] = cInfo;
                            Room oldR = r;
                            r = Rooms.Where(x => x.Value.ID == id).First().Value;
                            r.AddUser(cInfo);

                            m = new JsonMessage("Server", MessageType.Message);
                            m.SetContent("Moved to Room " + id);
                            JsonMessageHelpers.SetJsonMessage(stream, m);

                            foreach (Message message in r.Buffer)
                            {
                                JsonMessageHelpers.SetJsonMessage(stream, message.ToJsonMessage());
                            }
                            foreach (dynamic b in Bots)
                            {
                                b.OnUserChangeRoom(c, oldR, r);
                            }
                            continue;
                        }
                        catch
                        {
                            try
                            {
                                ClientInfo cInfo = clients[c.GUID];
                                Room oldR = r;
                                r = Rooms.Where(x => x.Value.Name == m.Content).First().Value;
                                r.AddUser(cInfo);
                                cInfo.SetRoomID(r.ID);
                                clients[c.GUID] = cInfo;

                                m = new JsonMessage("Server", MessageType.Message);
                                m.SetContent("Moved to Room " + r.Name);
                                JsonMessageHelpers.SetJsonMessage(stream, m);

                                foreach (Message message in r.Buffer)
                                {
                                    JsonMessageHelpers.SetJsonMessage(stream, message.ToJsonMessage());
                                }
                                foreach (dynamic b in Bots)
                                {
                                    b.OnUserChangeRoom(c, oldR, r);
                                }
                                continue;
                            }
                            catch { continue; }
                        }
                    }
                    if (m.Name != c.Name)
                    {
                        m.SetName(c.Name);
                    }
                    if (m.StatusType == StatusType.Disconnecting && m.MessageType == MessageType.Status)
                    {
                        stream.Close();
                        stream.Dispose();
                        foreach (dynamic b in Bots)
                        {
                            b.OnUserDisconnect(c);
                        }
                        r.RemoveUser(clients[c.GUID]);
                        clients.Remove(c.GUID);

                        JsonMessage t = new JsonMessage(c.GUID, MessageType.Status);
                        t.SetStatusType(StatusType.Disconnected);

                        SendToAllClients(null, t, 0);
                        break;
                    }
                    foreach (KeyValuePair<string, ClientInfo> network in clients)
                    {
                        if (network.Key == c.GUID)
                        {
                            continue;
                        }
                        else
                        {
                            if (m.EndPoint == "")
                            {
                                SendToAllClients(null, m, clients[c.GUID].RoomId, c.GUID);
                                break;
                            }
                            else
                            {
                                if (network.Value.Name == m.EndPoint)
                                {
                                    JsonMessageHelpers.SetJsonMessage(network.Value.Stream, m);
                                }
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    JsonMessage error = new JsonMessage("error", MessageType.Status);
                    error.SetStatusType(StatusType.ErrorDisconnect);
                    error.SetContent(e.Message);
                    if (clients.ContainsKey(c.GUID))
                    {
                        foreach (dynamic b in Bots)
                        {
                            b.OnUserDisconnect(c);
                        }
                        r.RemoveUser(clients[c.GUID]);
                        clients.Remove(c.GUID);
                    }
                    if (stream != null)
                    {
                        JsonMessageHelpers.SetJsonMessage(stream, error);
                        stream.Close();
                    }
                    break;
                }
            }
        }

        public static async void SendToAllClients(Message message, JsonMessage json, int roomID, string sender = "")
        {
            List<Task> tasks = new List<Task>();
            foreach (KeyValuePair<string, ClientInfo> network in clients)
            {
                if(sender != "")
                {
                    if(network.Key == sender)
                    {
                        continue;
                    }
                }
                if (roomID != -1)
                {
                    if (network.Value.RoomId != roomID)
                    {
                        continue;
                    }
                }
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

        public static Message InformationHandler(InfomationType type, string name)
        {
            if(type == InfomationType.ConnectedUsers)
            {
                Message m = new Message("Server", MessageType.Infomation);
                m.SetColor(Color.Aquamarine);
                m.SetContent(clients.Count.ToString() + " Connected Clients");
                return m;
            }
            else if (type == InfomationType.ConnectTime)
            {
                TimeSpan connectedTime = DateTime.UtcNow - clients[name].ConnectTime;
                Message m = new Message("Server", MessageType.Infomation);
                m.SetColor(Color.Aquamarine);
                m.SetContent("You have been connected for " + connectedTime.ToString());
                return m;
            }
            else if(type == InfomationType.MessagesSent)
            {
                Message m = new Message("Server", MessageType.Infomation);
                m.SetColor(Color.Aquamarine);
                m.SetContent(TotalMessagesSent.ToString() + " Messages Sent");
                return m;
            }
            else if(type == InfomationType.ServerUptime)
            {
                TimeSpan connectedTime = DateTime.UtcNow - startup;
                Message m = new Message("Server", MessageType.Infomation);
                m.SetColor(Color.Aquamarine);
                m.SetContent("The server has been online for " + connectedTime.ToString());
                return m;
            }
            else if(type == InfomationType.Rooms)
            {
                Message m = new Message("Server", MessageType.Infomation);
                m.SetColor(Color.Aquamarine);
                string content = "\n";
                foreach (KeyValuePair<string, Room> room in Rooms)
                {
                    content += $"{room.Value.Name} ({room.Value.ID})\n";
                }
                m.SetContent(content + "use the /changeroom followed by an id or a name to change room");
                return m;
            }
            else
            {
                Message m = new Message("Server", MessageType.Infomation);
                m.SetColor(Color.Aquamarine);
                m.SetContent("Unknown");
                return m;
            }
        }
    }
}
