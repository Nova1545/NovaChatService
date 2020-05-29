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
    class Program
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

            if (!Clients.Contains(ep))
            {
                Clients.Add(ep);
            }
            foreach (IPEndPoint endPoint in Clients)
            {
                server.Send(data, data.Length, endPoint);
                //server.BeginSend(data, data.Length, endPoint, new AsyncCallback(Send), null);
            }

            IPEndPoint remote = new IPEndPoint(IPAddress.Any, 11000);
            server.BeginReceive(new AsyncCallback(Receive), remote);
        }

        static void Send(IAsyncResult ar)
        {
            server.EndSend(ar);
        }
    }
}
