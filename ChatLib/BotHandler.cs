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

        public void TUpdateClient(ClientInfo client)
        {
            OnUpdateClient(client);
        }

        public void TUpdateRoom(Room room)
        {
            OnUpdateRoom(room);
        }

        public Dictionary<string, ClientInfo> TRequestClients()
        {
            return OnReqestClients();
        }
    }
}
