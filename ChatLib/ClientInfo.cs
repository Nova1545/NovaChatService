using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using ChatLib.DataStates;

namespace ChatLib.Extras
{
    public struct ClientInfo
    {
        public string Name { get; private set; }
        public NetworkStream Stream { get; private set; }
        public ClientType ClientType { get; private set; }

        public ClientInfo(string name, NetworkStream stream, ClientType clientType) : this()
        {
            Name = name;
            Stream = stream;
            ClientType = clientType;
        }
    }
}
