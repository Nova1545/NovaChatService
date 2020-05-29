using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NAudio;
using NAudio.Wave;

namespace VCClient
{
    class Program
    {
        static UdpClient client = new UdpClient();
        static BufferedWaveProvider buffer = new BufferedWaveProvider(new WaveFormat(44100, 2));
        static DirectSoundOut sOut = new DirectSoundOut();

        static void Main(string[] args)
        {
            IPAddress serverIP = IPAddress.Parse("10.0.0.86");
            IPEndPoint server = new IPEndPoint(serverIP, 11000);

            client.Connect(server);

            WaveInEvent source = new WaveInEvent();
            source.WaveFormat = new WaveFormat(44100, WaveIn.GetCapabilities(source.DeviceNumber).Channels);
            source.DataAvailable += Source_DataAvailable;
            source.StartRecording();

            buffer.DiscardOnBufferOverflow = true;
            sOut.Init(buffer);
            sOut.Volume = 1.0f;
            sOut.Play();

            //while (true)
            //{
            //    byte[] data = client.Receive(ref server);
            //    Console.WriteLine(server.Address);
            //    buffer.AddSamples(data, 0, data.Length);
            //}

            client.BeginReceive(new AsyncCallback(Recevie), server);

            Console.ReadLine();
        }

        private static void Source_DataAvailable(object sender, WaveInEventArgs e)
        {
            //s.Send(e.Buffer);
            client.Send(e.Buffer, e.BytesRecorded);
        }

        static void Recevie(IAsyncResult ar)
        {
            IPEndPoint ep = (IPEndPoint)ar.AsyncState;

            byte[] data = client.EndReceive(ar, ref ep);

            buffer.AddSamples(data, 0, data.Length);

            client.BeginReceive(new AsyncCallback(Recevie), ep);
        }
    }
    public struct UdpState
    {
        public UdpClient u;
        public IPEndPoint e;
    }
}
