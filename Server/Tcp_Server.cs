using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Diagnostics;
using System.IO;
using System.Threading;
using ChatLib;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using ChatLib.DataStates;
using ChatLib.Extras;

namespace Server
{
    class Tcp_Server
    {

        static Dictionary<string, NetworkStream> clients;
        Random rnd = new Random();
        static void Main(string[] args)
        {

            TcpListener server = null;
            IPAddress iPAddress = IPAddress.Parse(GetLocalIPAddress()) ;

            server = new TcpListener(iPAddress, 8910);
            server.Start();

            clients = new Dictionary<string, NetworkStream>();

            while (true)
            {
                Console.WriteLine("Waiting for Connection...");
                TcpClient client = server.AcceptTcpClient();
                NetworkStream stream = client.GetStream();

                Message message = Helpers.GetMessage(stream);
                if (message.MessageType == MessageType.Info && !clients.ContainsKey(message.Name) && message.Content == "name")
                {
                    string name = message.Name;
                    clients.Add(name, stream);
                    Console.WriteLine(name + " connected!");
                    ThreadPool.QueueUserWorkItem(HandleClientWorker, new object[2] { name, stream });
                }
                else
                {
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
            foreach (KeyValuePair<string, NetworkStream> network in clients)
            {
                Helpers.SetMessage(network.Value, new Message(network.Key == name? "You" : name, "connected", MessageType.Status));
            }
            while (true)
            {
                try
                {
                    Message m = Helpers.GetMessage(stream);
                    if(m.Name != name)
                    {
                        m.SetName(name);
                    }
                    if(m.Content == "disconnect" && m.MessageType == MessageType.Status)
                    {
                        Helpers.SetMessage(stream, m);
                        clients[name].Close();
                        clients.Remove(name);
                        Console.WriteLine(name + " disconnected");

                        foreach (KeyValuePair<string, NetworkStream> network in clients)
                        {
                            Helpers.SetMessage(network.Value, new Message(network.Key == name ? "You" : name, "disconnected", MessageType.Status));
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
                        Helpers.SetMessage(network.Value, new Message(network.Key == name? "You" : name, "disconnected", MessageType.Status));
                    }
                    break;
                }
                catch
                {
                    clients.Remove(name);
                    Console.WriteLine(name + " disconnected due to an error");

                    foreach (KeyValuePair<string, NetworkStream> network in clients)
                    {
                        Helpers.SetMessage(network.Value, new Message(network.Key == name ? "You" : name, "disconnected due to an error", MessageType.Status));
                    }
                    break;
                }
            }
        }
    }
}
