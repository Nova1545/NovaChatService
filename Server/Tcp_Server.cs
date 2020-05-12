using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading;
using ChatLib;
using ChatLib.DataStates;
using ChatLib.Extras;

namespace Server
{
    class Tcp_Server
    {

        static Dictionary<string, NetworkStream> clients;
        static List<Message> buffer;
        Random rnd = new Random();
        static void Main(string[] args)
        {
            Console.WriteLine(GetLocalIPAddress());
            TcpListener server = null;
            IPAddress iPAddress = IPAddress.Parse(GetLocalIPAddress()) ;

            server = new TcpListener(iPAddress, 8910);
            server.Start();

            clients = new Dictionary<string, NetworkStream>();
            buffer = new List<Message>();

            while (true)
            {
                Console.WriteLine("Waiting for Connection...");
                TcpClient client = server.AcceptTcpClient();
                NetworkStream stream = client.GetStream();

                Message message = Helpers.GetMessage(stream);
                if (message.MessageType == MessageType.Initionalize && !clients.ContainsKey(message.Name))
                {
                    string name = message.Name;
                    clients.Add(name, stream);
                    Console.WriteLine(name + " connected!");
                    ThreadPool.QueueUserWorkItem(HandleClientWorker, new object[2] { name, stream });
                }
                else
                {
                    Message h = new Message(message.Name, MessageType.Status);
                    h.SetStatusType(StatusType.Disconnecting);
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

        private static void HandleClientWorker(object token)
        {
            object[] info = (object[])token;
            NetworkStream stream = (NetworkStream)info[1];
            string name = (string)info[0];

            // Tell all connected clients that we are connected
            foreach (KeyValuePair<string, NetworkStream> network in clients)
            {
                Message m = new Message(name, MessageType.Status);
                m.SetStatusType(StatusType.Connected);
                Helpers.SetMessage(network.Value, m);
            }

            // Send The Server Message Buffer
            foreach (Message message in buffer)
            {
                Helpers.SetMessage(stream, message);
            }

            while (true)
            {
                try
                {
                    Message m = Helpers.GetMessage(stream);

                    // Manage Message buffer
                    buffer.Add(m);
                    if(buffer.Count > 10)
                    {
                        buffer.RemoveRange(0, buffer.Count - 10);
                    }

                    if(m.Name != name)
                    {
                        m.SetName(name);
                    }
                    if(m.StatusType == StatusType.Disconnecting && m.MessageType == MessageType.Status)
                    {
                        Helpers.SetMessage(stream, m);
                        clients[name].Close();
                        clients.Remove(name);
                        Console.WriteLine(name + " disconnected");

                        Message d = new Message(name, MessageType.Status);
                        d.SetStatusType(StatusType.Disconnected);

                        foreach (KeyValuePair<string, NetworkStream> network in clients)
                        {
                            Helpers.SetMessage(network.Value, d);
                        }
                        break;
                    }

                    foreach (KeyValuePair<string, NetworkStream> network in clients)
                    {
                        if (network.Key == name)
                        {
                            continue;
                        }
                        else
                        {
                            if (m.EndPoint == "")
                            {
                                Helpers.SetMessage(network.Value, m);
                            }
                            else
                            {
                                if (network.Key == m.EndPoint)
                                {
                                    Helpers.SetMessage(network.Value, m);
                                }
                            }
                        }
                    }
                }
                catch (IOException)
                {
                    clients.Remove(name);
                    Console.WriteLine(name + " disconnected");

                    foreach (KeyValuePair<string, NetworkStream> network in clients)
                    {
                        Message d = new Message(network.Key == name ? "You" : name, MessageType.Status);
                        d.SetStatusType(StatusType.Disconnected);
                        Helpers.SetMessage(network.Value, d);
                    }
                    break;
                }
                catch (Exception ex)
                {
                    foreach (KeyValuePair<string, NetworkStream> network in clients)
                    {
                        Message e = new Message(network.Key == name ? "You" : name, MessageType.Status);
                        e.SetStatusType(StatusType.ErrorDisconnect);
                        Helpers.SetMessage(network.Value, e);
                    }
                    
                    clients.Remove(name);
                    Console.WriteLine(name + " disconnected due to an error. Details: " + ex.Message);
                    break;
                }
            }
        }
    }
}
