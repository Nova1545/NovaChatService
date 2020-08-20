using System;
using System.Diagnostics;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;

namespace ChatLib.Extras
{
    public static class MessageHelpers
    {
        public static Message GetMessage(NetworkStream stream)
        {
            byte[] len = new byte[4];
            int total = 0;
            while (total < 4)
            {
                int read = stream.Read(len, 0, 4 - total);
                total += read;
            }
            total = 0;
            byte[] data = new byte[BitConverter.ToInt32(len, 0)];
            while (total < BitConverter.ToInt32(len, 0))
            {
                total += stream.Read(data, 0, BitConverter.ToInt32(len, 0));
            }

            return (Message)new BinaryFormatter().Deserialize(new MemoryStream(data));
        }

        public static void SetMessage(NetworkStream stream, Message message)
        {
            MemoryStream ms = new MemoryStream();
            new BinaryFormatter().Serialize(ms, message);
            byte[] dataBytes = ms.ToArray();
            byte[] dataLen = BitConverter.GetBytes((Int32)dataBytes.Length);
            try
            {
                stream.Write(dataLen, 0, 4);
                stream.Write(dataBytes, 0, dataBytes.Length);
            }
            catch { }
        }

        public static async Task SetMessageAsync(NetworkStream stream, Message message)
        {
            MemoryStream ms = new MemoryStream();
            new BinaryFormatter().Serialize(ms, message);
            byte[] dataBytes = ms.ToArray();
            byte[] dataLen = BitConverter.GetBytes(dataBytes.Length);
            try
            {
                stream.Write(dataLen, 0, 4);
                await stream.WriteAsync(dataBytes, 0, dataBytes.Length);
            }
            catch { }
        }

        public static Message GetMessage(SslStream stream)
        { 
            byte[] len = new byte[4];
            int total = 0;
            while (total < 4)
            {
                int read = stream.Read(len, 0, 4 - total);
                total += read;
            }
            total = 0;
            byte[] data = new byte[BitConverter.ToInt32(len, 0)];
            while (total < BitConverter.ToInt32(len, 0))
            {
                total += stream.Read(data, 0, BitConverter.ToInt32(len, 0));
            }

            return (Message)new BinaryFormatter().Deserialize(new MemoryStream(data));
        }

        public static void SetMessage(SslStream stream, Message message)
        {
            MemoryStream ms = new MemoryStream();
            new BinaryFormatter().Serialize(ms, message);
            byte[] dataBytes = ms.ToArray();
            byte[] dataLen = BitConverter.GetBytes((Int32)dataBytes.Length);
            try
            {
                stream.Write(dataLen, 0, 4);
                stream.Write(dataBytes, 0, dataBytes.Length);
            }
            catch { }
        }

        public static async Task SetMessageAsync(SslStream stream, Message message)
        {
            MemoryStream ms = new MemoryStream();
            new BinaryFormatter().Serialize(ms, message);
            byte[] dataBytes = ms.ToArray();
            byte[] dataLen = BitConverter.GetBytes((Int32)dataBytes.Length);
            try
            {
                await stream.WriteAsync(dataLen, 0, 4);
                await stream.WriteAsync(dataBytes, 0, dataBytes.Length);
            }
            catch { }
        }
    }
}
