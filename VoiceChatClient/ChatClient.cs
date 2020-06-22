using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace VoiceChatClient
{
    public class ChatClient
    {
        static byte[] ByteBuffer;

        static NetworkStream stream;

        static BufferedWaveProvider buffer = new BufferedWaveProvider(new WaveFormat(44100, 2));

        static MixingWaveProvider32 mixer = new MixingWaveProvider32();

        static DirectSoundOut sOut = new DirectSoundOut();

        static void Main(string[] args)
        {
            TcpClient client = new TcpClient("10.0.0.86", 8910);
            stream = client.GetStream();

            WaveInEvent source = new WaveInEvent();
            source.WaveFormat = new WaveFormat(44100, WaveIn.GetCapabilities(source.DeviceNumber).Channels);
            source.DataAvailable += Source_DataAvailable;
            source.StartRecording();

            buffer.DiscardOnBufferOverflow = true;
            sOut.Init(mixer);
            sOut.Volume = 1.0f;
            sOut.Play();

            ByteBuffer = new byte[1024];
            stream.BeginRead(ByteBuffer, 0, ByteBuffer.Length, Read, stream);

            Console.ReadLine();
        }

        private static void Source_DataAvailable(object sender, WaveInEventArgs e)
        {
            try
            {
                stream.Write(e.Buffer, 0, e.BytesRecorded);
            }
            catch { }
        }

        static void Read(IAsyncResult ar)
        {
            NetworkStream ns = (NetworkStream)ar.AsyncState;
            try
            {
                int bytesCount = ns.EndRead(ar);
                if(bytesCount > 0)
                {
                    byte[] data = new byte[bytesCount];
                    Array.Copy(ByteBuffer, 0, data, 0, bytesCount);

                    buffer.AddSamples(data, 0, bytesCount);

                }
                ns.BeginRead(ByteBuffer, 0, ByteBuffer.Length, Read, ns);
            }
            catch { }
        }
    }
}
