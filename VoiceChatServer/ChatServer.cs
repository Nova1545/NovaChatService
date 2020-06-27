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
using NAudio.Wave;
using NAudio;
using System.IO.Pipes;
using System.Threading.Tasks;

namespace VoiceChatServer
{
    class ChatServer
    {
        static TcpListener Server;
        static List<ServerThread> Clients = new List<ServerThread>();
        static DirectSoundOut sOut = new DirectSoundOut();

        static void Main(string[] args)
        {
            Server = new TcpListener(IPAddress.Parse("10.0.0.86"), 8910);

            Server.Start();

            bool x = true;

            Thread t = new Thread(() => St_OnDataReceivedCallback());
            t.Start();

            while (true)
            {
                TcpClient client = Server.AcceptTcpClient();

                ServerThread st = new ServerThread(client, Guid.NewGuid().ToString());
                //st.OnDataReceivedCallback += St_OnDataReceivedCallback;

                foreach (ServerThread thread in Clients)
                {
                    thread.Mixer.AddInputStream(st.Float);
                    Console.WriteLine(thread.Mixer.InputCount);
                }

                Clients.Add(st);

                foreach (ServerThread thread in Clients.Where(h => h.Name != st.Name).ToArray())
                {
                    st.Mixer.AddInputStream(thread.Float);
                }

                //if (x == false)
                //{
                //    sOut.Init(Clients[0].Mixer);
                //    sOut.Play();
                //}
                //x = false;

                try
                {
                    client.Client.BeginReceive(st.ReadBuffer, 0, st.ReadBuffer.Length, SocketFlags.None, st.Receive, client.Client);
                }
                catch { }
            }
        }

        static void St_OnDataReceivedCallback(/*byte[] data*/)
        {
            while (true)
            {
                try
                {
                    foreach (ServerThread server in Clients)
                    {
                        foreach (ServerThread thread in Clients.Where(h => h.Name != server.Name).ToArray())
                        {
                            
                        }
                    }
                }
                catch { }
            }
        }

        static void Write(IAsyncResult ar)
        {
            ((NetworkStream)ar.AsyncState).EndWrite(ar);
        }
    }
}
