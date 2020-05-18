using System;
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
            stream.Read(len, 0, 4);
            int dataLen = BitConverter.ToInt32(len, 0);
            byte[] bytes = new byte[dataLen];
            byte[] buffer = new byte[1024];
            if (dataLen > 1024)
            {
                MemoryStream ms = new MemoryStream();
                int total = 0;
                while(total < dataLen)
                {
                    int readcount = stream.Read(buffer, 0, buffer.Length);
                    total += readcount;
                    ms.Write(buffer, 0, readcount);
                }
                bytes = ms.ToArray();
            }
            else
            {
                stream.Read(bytes, 0, bytes.Length);
            }
            return (Message)new BinaryFormatter().Deserialize(new MemoryStream(bytes));
        }

        public static void SetMessage(NetworkStream stream, Message message)
        {
            MemoryStream ms = new MemoryStream();
            new BinaryFormatter().Serialize(ms, message);
            byte[] dataBytes = ms.ToArray();
            byte[] dataLen = BitConverter.GetBytes((Int32)dataBytes.Length);
            stream.Write(dataLen, 0, 4);
            stream.Write(dataBytes, 0, dataBytes.Length);
        }

        public static async Task SetMessageAsync(NetworkStream stream, Message message)
        {
            MemoryStream ms = new MemoryStream();
            new BinaryFormatter().Serialize(ms, message);
            byte[] dataBytes = ms.ToArray();
            byte[] dataLen = BitConverter.GetBytes((Int32)dataBytes.Length);
            stream.Write(dataLen, 0, 4);
            await stream.WriteAsync(dataBytes, 0, dataBytes.Length);
        }
    }
}
