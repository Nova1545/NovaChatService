using ChatLib.Extras;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatLib
{
    public class BotHandler
    {
        public delegate void UpdateClient(ClientInfo client);
        public event UpdateClient OnUpdateClient;

        public delegate void UpdateRoom(Room room);
        public event UpdateRoom OnUpdateRoom;

        public delegate Dictionary<string, ClientInfo> RequestClients();
        public event RequestClients OnReqestClients;

        public delegate Dictionary<string, Room> RequestRooms();
        public event RequestRooms OnRequestRooms;

        public void TUpdateClient(ClientInfo client)
        {
            OnUpdateClient(client);
        }

        public void TUpdateRoom(Room room)
        {
            OnUpdateRoom(room);
        }

        public Dictionary<string, Room> TRequestRooms()
        {
            return OnRequestRooms();
        }

        public Room TRequestRoomByID(int id)
        {
            return OnRequestRooms().First(x => x.Value.ID == id).Value;
        }

        public Room TRequestRoomByName(string name)
        {
            return OnRequestRooms().First(x => x.Value.Name == name).Value;
        }

        public Dictionary<string, ClientInfo> TRequestClients()
        {
            return OnReqestClients();
        }
        
        public ClientInfo TRequestClientByName(string name)
        {
            return OnReqestClients().First(x => x.Value.Name == name).Value;
        }

        public ClientInfo TRequestClientByGUID(string guid)
        {
            return OnReqestClients().First(x => x.Value.GUID == guid).Value;
        }
    }
}
