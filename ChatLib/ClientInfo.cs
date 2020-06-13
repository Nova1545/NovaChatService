using System;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using ChatLib.DataStates;

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
        }

        public void SetRoomID(int id) => RoomId = id;

        public void SetName(string name) => Name = name;

        public override string ToString()
        {
            return $"Name: {Name} ClientType: {ClientType} ConnectTime: {ConnectTime.ToString()} RoomId: {RoomId}";
        }
    }
}
