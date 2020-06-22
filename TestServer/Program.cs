using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ChatLib;
using ChatLib.DataStates;
using ChatLib.Extras;

namespace TestServer
{
    class Program
    {
        static X509Certificate2 X509 = null;

        static void Main(string[] args)
        {
            X509 = new X509Certificate2(@"A:\Git\NovaChatService\ServerV2\bin\Debug\certificate.pfx", "");

            TcpListener server = new TcpListener(IPAddress.Parse("10.0.0.86"), 8910);
            server.Start();

            server.BeginAcceptTcpClient(OnAccept, server);
            Console.ReadLine();
        }

        static void OnAccept(IAsyncResult ar)
        {
            TcpListener server = (TcpListener)ar.AsyncState;
            TcpClient client = server.EndAcceptTcpClient(ar);
            server.BeginAcceptTcpClient(OnAccept, ar.AsyncState);

            IPAddress addr = IPAddress.Parse(client.Client.RemoteEndPoint.ToString().Split(':')[0]);

            if (X509 != null)
            {
                Message secure = new Message("Server", MessageType.Initialize);
                secure.SetContent(X509.SubjectName.Name.Replace("CN=", ""));
                MessageHelpers.SetMessage(client.GetStream(), secure);

                SslStream stream = new SslStream(client.GetStream(), false);
                stream.AuthenticateAsServer(X509, false, true);

                Message m = MessageHelpers.GetMessage(stream);
                ThreadPool.QueueUserWorkItem(SecureDesktopWorker, new ClientInfo(m.Name, stream, ClientType.Desktop, addr));
            }
            else
            {
                NetworkStream stream = client.GetStream();
                Message secure = new Message("Server", MessageType.Initialize);
                secure.SetContent("");
                MessageHelpers.SetMessage(stream, secure);

                Message m = MessageHelpers.GetMessage(stream);
            }
        }

        static void SecureDesktopWorker(object state)
        {
            ClientInfo client = (ClientInfo)state;
            SslStream stream = client.SStream;

            Message m = new Message("Server", MessageType.Message);
            m.SetContent("Hello!");
            MessageHelpers.SetMessage(stream, m);

            while (true)
            {
                Message r = MessageHelpers.GetMessage(stream);

                MessageHelpers.SetMessage(stream, r);
            }
        }
    }
}

