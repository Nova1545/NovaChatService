using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;
using ChatLib.Administrator;
using ChatLib.DataStates;
using ChatLib.Json;

namespace ChatLib.Extras
{
    public class ClientInfo
    {
        public string Name { get; private set; }
        public NetworkStream Stream { get; private set; }
        public SslStream SStream { get; private set; }
        public bool IsSecure { get; private set; }
        public ClientType ClientType { get; private set; }
        public DateTime ConnectTime { get; private set; }
        public int RoomId { get; private set; }
        public string GUID { get; private set; }
        public IPAddress ClientAddress { get; private set; }
        public Admin Admin { get; private set; }
        public bool Muted { get; private set; }
        public BlockingCollection<Message> Queue { get; private set; }

        public ClientInfo(string name, NetworkStream stream, ClientType clientType, IPAddress clientAddress)
        {
            Name = name;
            Stream = stream;
            ClientType = clientType;
            ConnectTime = DateTime.UtcNow;
            RoomId = 0;
            IsSecure = false;
            GUID = Guid.NewGuid().ToString();
            ClientAddress = clientAddress;
            Muted = false;

            Queue = new BlockingCollection<Message>();
            Task.Factory.StartNew(() =>
            {
                foreach (Message buffer in Queue.GetConsumingEnumerable())
                {
                    if (ClientType == ClientType.Web)
                    {
                        JsonMessageHelpers.SetJsonMessage(Stream, buffer.ToJsonMessage());
                    }
                    else
                    {
                        MessageHelpers.SetMessage(Stream, buffer);
                    }
                }
            }, TaskCreationOptions.LongRunning);
        }

        public ClientInfo(string name, SslStream stream, ClientType clientType, IPAddress clientAddress)
        {
            Name = name;
            SStream = stream;
            ClientType = clientType;
            ConnectTime = DateTime.UtcNow;
            RoomId = 0;
            IsSecure = true;
            GUID = Guid.NewGuid().ToString();
            ClientAddress = clientAddress;
            Muted = false;
            Queue = new BlockingCollection<Message>();
            Task.Factory.StartNew(() =>
            {
                foreach (Message buffer in Queue)
                {
                    if (ClientType == ClientType.Web)
                    {
                        JsonMessageHelpers.SetJsonMessage(SStream, buffer.ToJsonMessage());
                    }
                    else
                    {
                        MessageHelpers.SetMessage(SStream, buffer);
                    }

                    //MemoryStream ms = new MemoryStream();
                    //new BinaryFormatter().Serialize(ms, buffer);
                    //byte[] dataBytes = ms.ToArray();
                    //byte[] dataLen = BitConverter.GetBytes((Int32)dataBytes.Length);
                    //try
                    //{
                    //    stream.Write(dataLen, 0, 4);
                    //    stream.Write(dataBytes, 0, dataBytes.Length);
                    //}
                    //catch { }
                }
            }, TaskCreationOptions.LongRunning);
        }

        public void SetRoomID(int id) => RoomId = id;

        public void SetName(string name) => Name = name;

        public void SetAdmin(Admin admin) => Admin = admin;

        public void ToggleMute() => Muted = !Muted;

        public void AddToQueue(Message message) => Queue.Add(message);

        public override string ToString()
        {
            return $"Name: {Name} ClientType: {ClientType} ConnectTime (UTC): {ConnectTime.ToString()} RoomId: {RoomId}";
        }
    }
}
