using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ChatLib.DataStates;
using ChatLib;
using ChatLib.Json;
using ChatLib.Extras;
using Newtonsoft.Json;
using System.Drawing;
using System.Net.Sockets;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;

namespace Testing
{
    class Program
    {
        static void Main(string[] args)
        {
            string ip = "10.0.0.86";
            int port = 2650;

            var server = new TcpListener(IPAddress.Parse(ip), port);

            server.Start();
            Console.WriteLine("Server has started on {0}:{1}, Waiting for a connection...", ip, port);

            TcpClient client = server.AcceptTcpClient();
            Console.WriteLine("A client connected.");

            NetworkStream stream = client.GetStream();

            // enter to an infinite cycle to be able to handle every change in stream
            /*while (true)
            {*/
            while (!stream.DataAvailable) ;
            while (client.Available < 3) ; // match against "get"

            JsonMessageHelpers.HandleHandshake(stream, client.Available);

            while (client.Available < 3) ;

            JsonMessage json = JsonMessageHelpers.GetJsonMessage(stream, client.Available);
            Console.WriteLine(json.Name);

            ThreadPool.QueueUserWorkItem(user, new object[2] { client, json.Name});
            //}
            Console.ReadLine();

        }

        static void user(object d)
        {
            object[] b = (object[])d;
            TcpClient client = (TcpClient)b[0];
            string name = b[1].ToString();
            NetworkStream stream = client.GetStream();

            while (true)
            {
                if(client.Available < 3)
                {
                    continue;
                }
                JsonMessage data = JsonMessageHelpers.GetJsonMessage(stream, client.Available);
                Console.WriteLine(data.Content);

                JsonMessageHelpers.SetJsonMessage(stream, data);
            }
        }
    }
}
