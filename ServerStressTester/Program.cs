using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ChatLib;
using ChatLib.DataStates;
using ChatLib.Extras;

namespace ServerStressTester
{
    class Program
    {
        static CancellationTokenSource cts = new CancellationTokenSource();
        static void Main(string[] args)
        {
            int threads = int.Parse(Console.ReadLine());
            for (int i = 0; i < threads; i++)
            {
                Console.WriteLine("Worker Created");
                Thread.Sleep(50);
                ThreadPool.QueueUserWorkItem(TestThread, null);
            }

            Console.WriteLine("Waiting for [Enter] to end test");
            Console.ReadLine();
            cts.Cancel();
            Console.ReadLine();
        }

        static void TestThread(object d)
        {
            TcpClient client = new TcpClient("novastudios.tk", 8910);
            Message secure = MessageHelpers.GetMessage(client.GetStream());
            User user = null;
            if (secure.Content != "")
            {
                SslStream ssl = new SslStream(client.GetStream(), false);
                ssl.AuthenticateAsClient(secure.Content);

                user = new User(Guid.NewGuid().ToString(), ssl);
                user.Init();
            }
            else
            {
                user = new User(Guid.NewGuid().ToString(), client.GetStream());
                user.Init();
            }

            user.OnMessageStatusReceivedCallback += (message) => 
            {  
                if(message.StatusType == StatusType.ErrorDisconnect)
                {
                    Console.WriteLine(user.Name + " Error: " + message.Content);
                }
            };

            Random rnd = new Random();

            Console.WriteLine("Worker Running");
            while (!cts.IsCancellationRequested)
            {
                user.CreateMessage(RandomString(rnd, 20), NColor.GenerateRandomColor());
                Thread.Sleep(rnd.Next(750, 1000));
            }
            user.CreateStatus(StatusType.Disconnecting);
            try
            {
                user.Close();
            }
            catch { }
            Console.WriteLine("Worker Quit");
        }

        public static string RandomString(Random random, int size, bool lowerCase = false)
        {
            var builder = new StringBuilder(size);

            // Unicode/ASCII Letters are divided into two blocks
            // (Letters 65–90 / 97–122):   
            // The first group containing the uppercase letters and
            // the second group containing the lowercase.  

            // char is a single Unicode character  
            char offset = lowerCase ? 'a' : 'A';
            const int lettersOffset = 26; // A...Z or a..z: length = 26  

            for (var i = 0; i < size; i++)
            {
                var @char = (char)random.Next(offset, offset + lettersOffset);
                builder.Append(@char);
            }

            return lowerCase ? builder.ToString().ToLower() : builder.ToString();
        }
    }
}
