using System;
using System.Net.Sockets;
using ChatLib.DataStates;

namespace ChatLib.Extras
{
    public struct ClientInfo
    {
        public string Name { get; private set; }
        public NetworkStream Stream { get; private set; }
        public ClientType ClientType { get; private set; }
        public DateTime ConnectTime { get; private set; }
        public int RoomId { get; private set; }
        public string GUID { get; private set; }

        public ClientInfo(string name, NetworkStream stream, ClientType clientType) : this()
        {
            Name = name;
            Stream = stream;
            ClientType = clientType;
            ConnectTime = DateTime.UtcNow;
            RoomId = 0;
            GUID = Guid.NewGuid().ToString();
        }

        public void SetRoomID(int id) => RoomId = id;

        public void SetName(string name) => Name = name;

        public override string ToString()
        {
            return $"Name: {Name} ClientType: {ClientType} ConnectTime: {ConnectTime.ToString()} RoomId: {RoomId}";
        }
    }
}
