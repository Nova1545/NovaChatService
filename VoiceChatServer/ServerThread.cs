using NAudio.Wave;
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
        public TcpClient Client;
        public byte[] ReadBuffer = new byte[1024];
        public string Name;

        public delegate void OnDataReceived(byte[] data);
        public event OnDataReceived OnDataReceivedCallback;

        public MixingWaveProvider32 Mixer;
        private BufferedWaveProvider Buffer;
        public Wave16ToFloatProvider Float;
        public WaveProviderToWaveStream Stream;

        public ServerThread(TcpClient client, string name)
        {
            Client = client;
            Name = name;

            Mixer = new MixingWaveProvider32();
            Buffer = new BufferedWaveProvider(new WaveFormat(44100, 2));
            Float = new Wave16ToFloatProvider(Buffer);
            Stream = new WaveProviderToWaveStream(Mixer);
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

                    //OnDataReceivedCallback?.Invoke(data);

                    Buffer.AddSamples(data, 0, data.Length);

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
