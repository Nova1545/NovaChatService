using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace ChatLib.Extras
{
    public static class Helpers
    {
        public static Message GetMessage(Stream stream)
        {
            byte[] len = new byte[4];
            stream.Read(len, 0, 4);
            int dataLen = BitConverter.ToInt32(len, 0);
            byte[] bytes = new byte[dataLen];
            int i = stream.Read(bytes, 0, bytes.Length);
            Console.WriteLine("Got " + i + " bytes");
            MemoryStream ms = new MemoryStream(bytes);
            return (Message)new BinaryFormatter().Deserialize(ms);
        }

        public static void SetMessage(Stream stream, Message message)
        {
            MemoryStream ms = new MemoryStream();
            new BinaryFormatter().Serialize(ms, message);
            byte[] dataBytes = ms.ToArray();
            Console.WriteLine("Sent " + dataBytes.Length + " bytes");
            byte[] dataLen = BitConverter.GetBytes((Int32)dataBytes.Length);
            stream.Write(dataLen, 0, 4);
            stream.Write(dataBytes, 0, dataBytes.Length);
        }

        public static void SetFileStream(Stream stream, byte[] file)
        {
            byte[] dataLen = BitConverter.GetBytes((Int32)file.Length);
            stream.Write(dataLen, 0, 4);
            stream.Write(file, 0, file.Length);
        }

        public static byte[] GetFileStream(Stream stream)
        {
            byte[] len = new byte[4];
            stream.Read(len, 0, 4);
            int dataLen = BitConverter.ToInt32(len, 0);
            byte[] bytes = new byte[dataLen];
            stream.Read(bytes, 0, bytes.Length);
            return bytes;
        } 
    }
}
