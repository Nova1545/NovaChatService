using ChatLib;
using ChatLib.DataStates;
using ChatLib.Extentions;
using ChatLib.Extras;
using ChatLib.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExampleBot
{
    [Bot("Test Bot", creator: "Aiden Wilkins (Nova Studios)", desc: "Test bot created for testing (duh)")]
    public class MyBot : BotAb
    {
        public MyBot(BotHandler bot) : base(bot)
        {
            CommandHandler.CreateCommand("ping", Ping);
            CommandHandler.CreateCommand("room", Users);
        }

        private void Users(string[] parameters, ClientInfo sender)
        {
            Room r = BotHandler.TRequestRoomByID(sender.RoomId);
            Message m = new Message("Test Bot", MessageType.Message);
            m.SetContent(r.ToString());
            sender.Send(m);
        }

        private void Ping(string[] parameters, ClientInfo sender)
        {
            Message m = new Message("Server", MessageType.Message);
            m.SetContent("Pong!");
            m.SetColor(NColor.FromRGB(0, 255, 21));

            sender.Send(m);
        }

        public override void OnUserChangeRoom(ClientInfo client, Room oldRoom, Room newRoom)
        {
            Dictionary<string, ClientInfo> clients = BotHandler.TRequestClients();
            foreach (KeyValuePair<string, ClientInfo> info in clients)
            {
                if (info.Value.GUID != client.GUID)
                {
                    if (info.Value.RoomId == newRoom.ID)
                    {
                        Message m = new Message("Server", MessageType.Message);
                        m.SetContent(client.Name + " Joined your room");
                        m.SetColor(NColor.Orange);
                        info.Value.Send(m);
                    }
                    else if (info.Value.RoomId == oldRoom.ID)
                    {
                        Message m = new Message("Server", MessageType.Message);
                        m.SetContent(client.Name + " Left your room");
                        m.SetColor(NColor.Orange);
                        info.Value.Send(m);
                    }
                }
            }
        }

        public override void OnUserConnect(ClientInfo client)
        {
            Message m = new Message("Test Bot", MessageType.Message);
            m.SetColor(NColor.Blue);
            m.SetContent("Welcome " + client.Name);
            BotHandler.TRequestClients().SendToAll(m, new List<ClientInfo>() { client });
        }
    }
}
