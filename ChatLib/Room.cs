using System;
using System.Collections.Generic;

namespace ChatLib.Extras
{
    public struct Room
    {
        public string Name { get; private set; }
        public int ID { get; private set; }
        public int UserLimit { get; private set; }
        public int MaxBufferSize { get; private set; }
        public List<Message> Buffer { get; private set; }
        public Dictionary<string, ClientInfo> Clients { get; private set; }

        public string GUID { get; private set; }

        public Room(string name, int id, int userLimit = -1, int maxBufferSize = 10) : this()
        {
            Name = name;
            ID = id;
            UserLimit = userLimit;
            MaxBufferSize = maxBufferSize;
            Buffer = new List<Message>();
            Clients = new Dictionary<string, ClientInfo>();
            GUID = Guid.NewGuid().ToString();
        }

        public void AddMesssage(Message m)
        {
            Buffer.Add(m);
            if (Buffer.Count > MaxBufferSize)
            {
                Buffer.RemoveRange(0, Buffer.Count - MaxBufferSize);
            }
        }

        public bool AddUser(ClientInfo client)
        {
            if (Clients.ContainsKey(client.Name))
            {
                return true;
            }
            if(Clients.Count + 1 <= UserLimit || UserLimit == -1)
            {
                Clients.Add(client.Name, client);
                return true;
            }
            return false;
        }

        public void RemoveUser(ClientInfo client)
        {
            Clients.Remove(client.Name);
        }

        public override string ToString()
        {
            string i = $"Name: {Name} ID: {ID} UserLimit: {UserLimit} MaxBufferSize: {MaxBufferSize} Users: \n";
            foreach (KeyValuePair<string, ClientInfo> client in Clients)
            {
                i += client.ToString() + "\n";
            }
            return i;
        }
    }
}
