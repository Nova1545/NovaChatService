using ChatLib;
using ChatLib.DataStates;
using ChatLib.Extras;
using ChatLib.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SslTesting
{
    class Program
    {
        static X509Certificate2 X509 = null;
        static void Main(string[] args)
        {
            X509 = new X509Certificate2(@"C:\Users\aiden\OneDrive\Desktop\sslforfree\certificateChat.pfx", "");

            TcpListener server = new TcpListener(IPAddress.Parse("10.0.0.86"), 8911);
            server.Start();
            while (true)
            {
                TcpClient client = server.AcceptTcpClient();
                Console.WriteLine("Clinet!");

                SslStream stream = new SslStream(client.GetStream(), true);
                stream.AuthenticateAsServer(X509, false, SslProtocols.Default, true);

                while (client.Available < 3)
                {
                }

                //JsonMessageHelpers.HandleHandshake(client.GetStream(), client.Available);
                HandleHandshake(stream, client.Available);
                Run(stream, client);
            }
        }

        static void Run(SslStream stream, TcpClient client)
        {
            while (true)
            {
                if(client.Available < 3)
                {
                    continue;
                }
                JsonMessage message = GetJsonMessage(stream, client.Available);
                Console.WriteLine(message.Content);
                message.SetName("Server");
                message.SetColor(NColor.FromRGB(0, 255, 245));
                SetJsonMessage(stream, message);
            }
        }

        public static void HandleHandshake(SslStream stream, int length)
        {
            byte[] buffer = new byte[1024];

            int total = 0;
            MemoryStream ms = new MemoryStream();
            while (total < length)
            {
                int read = stream.Read(buffer, 0, buffer.Length);
                total += read;
                ms.Write(buffer, 0, read);

                if (Encoding.UTF8.GetString(ms.ToArray()).IndexOf("\r\n\r\n") != -1)
                {
                    break;
                }
            }

            byte[] bytes = ms.ToArray();

            string s = Encoding.UTF8.GetString(bytes);

            if (Regex.IsMatch(s, "^GET", RegexOptions.IgnoreCase))
            {
                Console.WriteLine("=====Handshaking from client=====\n{0}", s);

                // 1. Obtain the value of the "Sec-WebSocket-Key" request header without any leading or trailing whitespace
                // 2. Concatenate it with "258EAFA5-E914-47DA-95CA-C5AB0DC85B11" (a special GUID specified by RFC 6455)
                // 3. Compute SHA-1 and Base64 hash of the new value
                // 4. Write the hash back as the value of "Sec-WebSocket-Accept" response header in an HTTP response
                string swk = Regex.Match(s, "Sec-WebSocket-Key: (.*)").Groups[1].Value.Trim();
                string swka = swk + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
                byte[] swkaSha1 = System.Security.Cryptography.SHA1.Create().ComputeHash(Encoding.UTF8.GetBytes(swka));
                string swkaSha1Base64 = Convert.ToBase64String(swkaSha1);

                // HTTP/1.1 defines the sequence CR LF as the end-of-line marker
                byte[] response = Encoding.UTF8.GetBytes(
                    "HTTP/1.1 101 Switching Protocols\r\n" +
                    "Connection: Upgrade\r\n" +
                    "Upgrade: websocket\r\n" +
                    "Sec-WebSocket-Accept: " + swkaSha1Base64 + "\r\n\r\n");

                stream.Write(response, 0, response.Length);
            }
            else
            {
                Console.WriteLine("Handshake failed");
            }
            stream.Flush();
        }

        public static JsonMessage GetJsonMessage(SslStream stream, int length)
        {
            byte[] buffer = new byte[1024];

            Console.WriteLine(length);
            int total = 0;
            MemoryStream ms = new MemoryStream();
            while (total < length)
            {
                int read;
                read = stream.Read(buffer, 0, buffer.Length);
                total += read;
                Console.ForegroundColor = ConsoleColor.Blue;
                Console.WriteLine(total);
                ms.Write(buffer, 0, read);
            }

            Console.ForegroundColor = ConsoleColor.White;

            byte[] bytes = ms.ToArray();
            Console.WriteLine(bytes.Length);

            bool fin = (bytes[0] & 0b10000000) != 0,
                        mask = (bytes[1] & 0b10000000) != 0; // must be true, "All messages from the client to the server have this bit set"

            int opcode = bytes[0] & 0b00001111, // expecting 1 - text message
                msglen = bytes[1] - 128, // & 0111 1111
                offset = 2;

            if (msglen == 126)
            {
                // was ToUInt16(bytes, offset) but the result is incorrect
                msglen = BitConverter.ToUInt16(new byte[] { bytes[3], bytes[2] }, 0);
                offset = 4;
            }
            else if (msglen == 127)
            {
                Console.WriteLine("TODO: msglen == 127, needs qword to store msglen");
                // i don't really know the byte order, please edit this
                // msglen = BitConverter.ToUInt64(new byte[] { bytes[5], bytes[4], bytes[3], bytes[2], bytes[9], bytes[8], bytes[7], bytes[6] }, 0);
                // offset = 10;
            }

            if (msglen == 0)
            {
                Console.WriteLine("msglen == 0");
                return null;
            }
            else if (mask)
            {
                byte[] decoded = new byte[msglen];
                byte[] masks = new byte[4] { bytes[offset], bytes[offset + 1], bytes[offset + 2], bytes[offset + 3] };
                offset += 4;

                for (int i = 0; i < msglen; ++i)
                    decoded[i] = (byte)(bytes[offset + i] ^ masks[i % 4]);

                string json = Encoding.UTF8.GetString(decoded);
                return JsonSerialization.Deserialize(json);
            }
            else
            {
                Console.WriteLine("mask bit not set");
                return null;
            }
        }

        public static void SetJsonMessage(SslStream stream, JsonMessage json)
        {
            byte[] raw = Encoding.UTF8.GetBytes(JsonSerialization.Serialize(json));

            byte[] frame = new byte[10 + raw.Length];
            int indexStartData = -1;

            frame[0] = 129;

            if (raw.Length <= 125)
            {
                frame[1] = (byte)raw.Length;
                indexStartData = 2;
            }
            else if (raw.Length >= 126 && raw.Length <= 65535)
            {
                frame[1] = 126;
                frame[2] = (byte)((raw.Length >> 8) & 255);
                frame[3] = (byte)((raw.Length) & 255);

                indexStartData = 4;
            }
            else
            {
                frame[1] = 127;
                frame[2] = (byte)((raw.Length >> 56) & 255);
                frame[3] = (byte)((raw.Length >> 48) & 255);
                frame[4] = (byte)((raw.Length >> 40) & 255);
                frame[5] = (byte)((raw.Length >> 32) & 255);
                frame[6] = (byte)((raw.Length >> 24) & 255);
                frame[7] = (byte)((raw.Length >> 16) & 255);
                frame[8] = (byte)((raw.Length >> 8) & 255);
                frame[9] = (byte)((raw.Length) & 255);

                indexStartData = 10;
            }

            for (int i = 0; i < raw.Length; i++)
            {
                frame[i + indexStartData] = raw[i];
            }
            try
            {
                stream.Write(frame, 0, frame.Length);
            }
            catch { }
        }
    }
}
