using ChatLib.DataStates;
using ChatLib.Extras;
using ChatLib.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ChatLib.Extentions
{
    public static class ExtentionHelpers
    {
        public static void SendToAll(this Dictionary<string, ClientInfo> values, Message message, List<ClientInfo> ignore)
        {
            foreach (KeyValuePair<string, ClientInfo> client in values)
            {
                if(client.Value.ClientType == ClientType.Web)
                {
                    JsonMessageHelpers.SetJsonMessage(client.Value.Stream, message.ToJsonMessage());
                }
                else
                {
                    MessageHelpers.SetMessage(client.Value.Stream, message);
                }
            }
        }

        public static void SendToAll(this Dictionary<string, ClientInfo> values, JsonMessage message, List<ClientInfo> ignore)
        {
            foreach (KeyValuePair<string, ClientInfo> client in values)
            {
                if (ignore.Select(x => x.GUID == client.Value.GUID).First())
                {
                    continue;
                }
                if (client.Value.ClientType == ClientType.Web)
                {
                    JsonMessageHelpers.SetJsonMessage(client.Value.Stream, message);
                }
                else
                {
                    MessageHelpers.SetMessage(client.Value.Stream, message.ToMessage());
                }
            }
        }

        public static void Send(this ClientInfo client, Message m)
        {
            if(client.ClientType == ClientType.Web)
            {
                if (client.IsSecure)
                {
                    JsonMessageHelpers.SetJsonMessage(client.SStream, m.ToJsonMessage());
                }
                else
                {
                    JsonMessageHelpers.SetJsonMessage(client.Stream, m.ToJsonMessage());
                }
            }
            else
            {
                if (client.IsSecure)
                {
                    MessageHelpers.SetMessage(client.SStream, m);
                }
                else
                {
                    MessageHelpers.SetMessage(client.Stream, m);
                }
            }
        }

        public static void Send(this ClientInfo client, JsonMessage m)
        {
            if (client.ClientType == ClientType.Web)
            {
                if (client.IsSecure)
                {
                    JsonMessageHelpers.SetJsonMessage(client.SStream, m);
                }
                else
                {
                    JsonMessageHelpers.SetJsonMessage(client.Stream, m);
                }
            }
            else
            {
                if (client.IsSecure)
                {
                    MessageHelpers.SetMessage(client.SStream, m.ToMessage());
                }
                else
                {
                    MessageHelpers.SetMessage(client.Stream, m.ToMessage());
                }
            }
        }
    }
}
