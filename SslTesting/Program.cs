using ChatLib;
using ChatLib.DataStates;
using ChatLib.Extras;
using ChatLib.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SslTesting
{
    class Program
    {
        static X509Certificate2 X509 = null;
        static void Main(string[] args)
        {
            X509 = new X509Certificate2("certificate.pfx", "");

            //Message message = new Message("Enter a name", MessageType.Initionalize);

            //MemoryStream ms = new MemoryStream();
            //new BinaryFormatter().Serialize(ms, message);

            //Console.WriteLine(ms.Length);
            //foreach (byte b in ms.ToArray())
            //{
            //    Console.Write(b + "-");
            //}

            TcpListener server = new TcpListener(IPAddress.Parse("10.0.0.86"), 8910);
            server.Start();

            TcpClient client = server.AcceptTcpClient();
            Console.WriteLine("Clinet!");

            SslStream stream = new SslStream(client.GetStream(), false);
            stream.AuthenticateAsServer(X509, false, SslProtocols.Default, true);
            //NetworkStream stream = client.GetStream();

            Message m = GetMessage(stream);
            Console.WriteLine(m.Name + " " + m.MessageType);

            m = new Message("Server", MessageType.Message);
            m.SetContent("Hello!");
            SetMessage(stream, m);

            Console.ReadLine();
            client.Close();
        }

        public static Message GetMessage(SslStream stream)
        {
            MemoryStream ms = new MemoryStream();
            byte[] len = new byte[4];
            int total = 0;
            while (total < 4)
            {
                int readcount = stream.Read(len, 0, len.Length);
                total += readcount;
                Console.WriteLine(total);
            }

            int dataLen = BitConverter.ToInt32(len, 0);
            Console.WriteLine("Got " + dataLen);
            byte[] bytes = new byte[dataLen];
            byte[] buffer = new byte[1024];
            ms = new MemoryStream();
            total = 0;
            while (total < dataLen)
            {
                int readcount = stream.Read(buffer, 0, buffer.Length);
                total += readcount;
                ms.Write(buffer, 0, readcount);
            }

            bytes = ms.ToArray();
            try
            {
                return (Message)new BinaryFormatter().Deserialize(new MemoryStream(bytes));
            }
            catch
            {
                return new Message("test", MessageType.Message);
            }
        }

        public static void SetMessage(SslStream stream, Message message)
        {
            MemoryStream ms = new MemoryStream();
            new BinaryFormatter().Serialize(ms, message);
            byte[] dataBytes = ms.ToArray();
            byte[] dataLen = BitConverter.GetBytes(dataBytes.Length);
            try
            {
                stream.Write(dataLen, 0, 4);
                stream.Write(dataBytes, 0, dataBytes.Length);
            }
            catch { }
        }
    }
}
