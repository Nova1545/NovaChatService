using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Xml.Serialization;
using System.Threading;
using System.Collections;

namespace VoiceChatServer
{
    class ChatServer
    {
        static TcpListener Server;
        static List<ServerThread> Clients = new List<ServerThread>();

        static void Main(string[] args)
        {
            Server = new TcpListener(IPAddress.Parse("10.0.0.86"), 8910);

            try
            {
                Server.Start();

                while (true)
                {
                    TcpClient client = Server.AcceptTcpClient();

                    ServerThread st = new ServerThread(client, Guid.NewGuid().ToString());
                    st.OnDataReceivedCallback += St_OnDataReceivedCallback;

                    try
                    {
                        Clients.Add(st);
                        client.Client.BeginReceive(st.ReadBuffer, 0, st.ReadBuffer.Length, SocketFlags.None, st.Receive, client.Client);
                    }
                    catch { }
                }
            }
            catch { }
        }

        private static void St_OnDataReceivedCallback(byte[] data)
        {
            foreach (ServerThread sv in Clients)
            {
                try
                {
                    sv.Send(data);
                }
                catch { }
            }
        }
    }
}
