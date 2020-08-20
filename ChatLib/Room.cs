using ChatLib.DataStates;
using System;
using System.Collections.Generic;
using ChatLib;
using ChatLib.Json;

namespace ChatLib.Extras
{
    public class Room
    {
        public string Name { get; private set; }
        public int ID { get; private set; }
        public int UserLimit { get; private set; }
        public int MaxBufferSize { get; private set; }
        public List<Message> Buffer { get; private set; }
        public Dictionary<string, ClientInfo> Clients { get; private set; }
        public bool IsFull { get; set; }
        public string GUID { get; private set; }

        public Room(string name, int id, int userLimit = -1, int maxBufferSize = 10)
        {
            Name = name;
            ID = id;
            UserLimit = userLimit;
            MaxBufferSize = maxBufferSize;
            Buffer = new List<Message>();
            Clients = new Dictionary<string, ClientInfo>();
            GUID = Guid.NewGuid().ToString();
            IsFull = false;
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
                IsFull = (Clients.Count >= UserLimit && UserLimit != -1) ? true : false;
                return false;
            }
            if(Clients.Count + 1 <= UserLimit || UserLimit == -1)
            {
                Clients.Add(client.GUID, client);
                IsFull = (Clients.Count >= UserLimit && UserLimit != -1) ? true : false;
                if(client.ClientType == ClientType.Web)
                {
                    if (client.IsSecure)
                    {
                        foreach (Message message in Buffer)
                        {
                            JsonMessageHelpers.SetJsonMessage(client.SStream, message.ToJsonMessage());
                        }
                    }
                    else
                    {
                        foreach (Message message in Buffer)
                        {
                            JsonMessageHelpers.SetJsonMessage(client.Stream, message.ToJsonMessage());
                        }
                    }
                }
                else
                {
                    List<Message> copy = new List<Message>(Buffer);
                    if (client.IsSecure)
                    {
                        foreach (Message message in copy)
                        {
                            MessageHelpers.SetMessage(client.SStream, message);
                        }
                    }
                    else
                    {
                        foreach (Message message in copy)
                        {
                            MessageHelpers.SetMessage(client.Stream, message);
                        }
                    }
                }
                return true;
            }
            IsFull = (Clients.Count >= UserLimit && UserLimit != -1) ? true : false;
            return false;
        }

        public void RemoveUser(ClientInfo client)
        {
            Clients.Remove(client.GUID);
            IsFull = (Clients.Count == UserLimit && UserLimit != -1) ? true : false;
        }

        public override string ToString()
        {
            string i = $"[GUID: {GUID}] Name: {Name} ID: {ID} UserLimit: {UserLimit} MaxBufferSize: {MaxBufferSize} IsFull: {IsFull} Users: \n";
            foreach (KeyValuePair<string, ClientInfo> client in Clients)
            {
                i += client.ToString() + "\n";
            }
            return i;
        }
    }
}
