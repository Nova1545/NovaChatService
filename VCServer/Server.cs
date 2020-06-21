using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using NAudio.Wave;

namespace VCServer
{
    class Server
    {
        static List<IPEndPoint> Clients = new List<IPEndPoint>();

        static UdpClient server = new UdpClient(11000);

        static void Main(string[] args)
        {
            #region NO
            // Connect Managment
            //IPEndPoint connectIp = new IPEndPoint(IPAddress.Any, 6326);
            //UdpClient cU = new UdpClient(connectIp);
            //UdpState us = new UdpState();
            //us.e = connectIp;
            //us.u = cU;

            //cU.BeginReceive(new AsyncCallback(ReceiveConnect), us);


            //// Data Transmission
            //IPEndPoint e = new IPEndPoint(IPAddress.Any, 6325);
            //UdpClient u = new UdpClient(e);
            //UdpState s = new UdpState();
            //s.e = e;
            //s.u = u;
            //u.BeginReceive(new AsyncCallback(Receive), s);


            //Console.ReadLine();
            #endregion

            IPEndPoint remote = new IPEndPoint(IPAddress.Any, 11000);
            server.BeginReceive(new AsyncCallback(Receive), remote);

            Console.ReadLine();
        }


        static void Receive(IAsyncResult ar)
        {
            IPEndPoint ep = (IPEndPoint)ar.AsyncState;
            byte[] data = server.EndReceive(ar, ref ep);
            Console.WriteLine("Receving " + data.Length + " Port " + ep.Port);

            IPEndPoint remote = new IPEndPoint(IPAddress.Any, 11000);
            server.BeginReceive(new AsyncCallback(Receive), remote);

            if (!Clients.Contains(ep))
            {
                Clients.Add(ep);
            }
            SendToAll(data);
        }

        static async void SendToAll(byte[] data)
        {
            List<Task> tasks = new List<Task>();
            foreach (IPEndPoint endPoint in Clients)
            {
                tasks.Add(server.SendAsync(data, data.Length, endPoint));
            }
            try
            {
                while (tasks.Any())
                {
                    tasks.Remove(await Task.WhenAny());
                }
            }
            catch
            {

            }
        }

        static void Send(IAsyncResult ar)
        {
            server.EndSend(ar);
        }
    }
}
