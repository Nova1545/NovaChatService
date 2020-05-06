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
            stream.Read(bytes, 0, bytes.Length);
            MemoryStream serializationStream = new MemoryStream(bytes);
            return (Message)new BinaryFormatter().Deserialize(serializationStream);
        }

        public static void SetMessage(Stream stream, Message message)
        {
            MemoryStream ms = new MemoryStream();
            new BinaryFormatter().Serialize(ms, message);
            byte[] dataBytes = ms.ToArray();
            byte[] dataLen = BitConverter.GetBytes((Int32)dataBytes.Length);
            stream.Write(dataLen, 0, 4);
            stream.Write(dataBytes, 0, dataBytes.Length);
        }

        public static string GetExtension<T>(this T e) where T : IConvertible
        {
            if (e is Enum)
            {
                Type type = e.GetType();
                Array values = System.Enum.GetValues(type);

                foreach (int val in values)
                {
                    if (val == e.ToInt32(CultureInfo.InvariantCulture))
                    {
                        var memInfo = type.GetMember(type.GetEnumName(val));
                        var descriptionAttribute = memInfo[0]
                            .GetCustomAttributes(typeof(DescriptionAttribute), false)
                            .FirstOrDefault() as DescriptionAttribute;

                        if (descriptionAttribute != null)
                        {
                            return descriptionAttribute.Description;
                        }
                    }
                }
                return null; // could also return string.Empty
            }
            return null;
        }
    }
}
