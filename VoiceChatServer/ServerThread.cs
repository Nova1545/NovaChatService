using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace VoiceChatServer
{
    public class ServerThread
    {
        private TcpClient Client;
        public byte[] ReadBuffer = new byte[1024];
        public string Name;

        public delegate void OnDataReceived(byte[] data);
        public event OnDataReceived OnDataReceivedCallback;

        public ServerThread(TcpClient client, string name)
        {
            Client = client;
            Name = name;
        }

        public void Receive(IAsyncResult ar)
        {
            try
            {
                int bytesRead = Client.Client.EndReceive(ar);
                if (bytesRead > 0)
                {
                    byte[] data = new byte[bytesRead];
                    Array.Copy(ReadBuffer, 0, data, 0, bytesRead);

                    OnDataReceivedCallback?.Invoke(data);

                    Client.Client.BeginReceive(ReadBuffer, 0, ReadBuffer.Length, SocketFlags.None, Receive, Client.Client);
                }
            }
            catch
            {

            }
        }

        public void Send(byte[] data)
        {
            try
            {
                NetworkStream ns = Client.GetStream();
                ns.Write(data, 0, data.Length);
            }
            catch { }
        }
    }
}
