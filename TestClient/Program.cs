using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using ChatLib;
using ChatLib.Extras;

namespace TestClient
{
    class Program
    {
        static void Main(string[] args)
        {
            TcpClient client = new TcpClient("10.0.0.86", 8910);

            Message d = MessageHelpers.GetMessage(client.GetStream());
            Console.WriteLine(d.Name);

            SslStream stream = new SslStream(client.GetStream());
            stream.AuthenticateAsClient("ftp.novastudios.tk");
            User u = new User("Test", stream);
            //u.Init();
            u.OnMessageAnyReceivedCallback += U_OnMessageAnyReceivedCallback;

            while (true)
            {
                //byte[] len = new byte[4];
                //int total = 0;
                //while (total < 4)
                //{
                //    total += stream.Read(len, 0, 4);
                //}
                //total = 0;
                //byte[] data = new byte[BitConverter.ToInt32(len, 0)];
                //while (total < BitConverter.ToInt32(len, 0))
                //{
                //    total += stream.Read(data, 0, BitConverter.ToInt32(len, 0));
                //}

                //Message m = (Message)new BinaryFormatter().Deserialize(new MemoryStream(data));   
                u.CreateMessage("Hello!", NColor.Orange);
            }
            Console.WriteLine("End");
            Console.ReadLine();
        }

        private static void U_OnMessageAnyReceivedCallback(Message message)
        {
            Console.WriteLine(message.Name + ": "/* + BitConverter.ToInt32(len, 0)*/);
        }
    }
}
