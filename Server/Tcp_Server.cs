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
            IPAddress iPAddress = IPAddress.Parse("10.0.0.86");

            server = new TcpListener(iPAddress, 8910);
            server.Start();

            clients = new Dictionary<string, NetworkStream>();

            while (true)
            {
                Console.WriteLine("Waiting for connection");
                TcpClient client = server.AcceptTcpClient();
                NetworkStream stream = client.GetStream();

                Message message = Helpers.GetMessage(stream);
                if (message.MessageType == MessageType.Info && !clients.ContainsKey(message.Name) && message.Content == "name")
                {
                    string name = message.Name;
                    clients.Add(name, stream);
                    Console.WriteLine(name + " Connected!");
                    ThreadPool.QueueUserWorkItem(HandleClientWorker, new object[2] { name, stream });
                }
                else
                {
                    client.Dispose();
                }
            }
        }

        private static void HandleClientWorker(object token)
        {
            object[] info = (object[])token;
            NetworkStream stream = (NetworkStream)info[1];
            string name = (string)info[0];
            foreach (KeyValuePair<string, NetworkStream> network in clients)
            {
                Helpers.SetMessage(network.Value, new Message(network.Key == name? "You" : name, "Connected", MessageType.Status));
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
                catch (IOException e)
                {
                    clients.Remove(name);
                    Console.WriteLine(name + " Disconnected");

                    foreach (KeyValuePair<string, NetworkStream> network in clients)
                    {
                        Helpers.SetMessage(network.Value, new Message(network.Key == name? "You" : name, "Disconnected", MessageType.Status));
                    }
                    break;
                }
            }
        }

    }
}
