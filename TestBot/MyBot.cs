using System.Collections.Generic;
using System.Linq;
using ChatLib;
using ChatLib.Extras;
using ChatLib.Extentions;
using ChatLib.DataStates;
using System.Drawing;
using System.IO;
using System;
using ChatLib.Json;

namespace TestBot
{
    [Bot("Test Bot", creator: "Aiden Wilkins (Nova Studios)", desc: "Test bot created for testing (duh)")]
    public class MyBot
    {
        BotHandler handler;
        CommandHandler command;

        public void Init(BotHandler handler)
        {
            this.handler = handler;
            command = new CommandHandler("/", ' ');
            command.CreateCommand("hello", Hello);
        }

        public Message OnMessage(ClientInfo sender, Message message)
        {
            command.ProcessMessage(message, sender);
            return message;
        }

        public JsonMessage OnJsonMessage(ClientInfo sender, JsonMessage message)
        {
            command.ProcessJsonMessage(message, sender);
            return message;
        }

        public void Hello(string[] parameters, ClientInfo sender)
        {
            Message m = new Message("Server", MessageType.Message);
            m.SetContent("Hello " + sender.Name);
            handler.TRequestClients().SendToAll(m, new List<ClientInfo>());
        }

        public void OnUserChangeRoom(ClientInfo client, Room oldRoom, Room newRoom)
        {
            Dictionary<string, ClientInfo> clients = handler.TRequestClients();
            foreach (KeyValuePair<string, ClientInfo> info in clients)
            {
                if(info.Value.GUID != client.GUID)
                {
                    if(info.Value.RoomId == newRoom.ID)
                    {
                        Message m = new Message("Server", MessageType.Message);
                        m.SetContent(client.Name + " Joined your room");
                        m.SetColor(Color.Orange);
                        if (info.Value.ClientType == ClientType.Web)
                        {
                            JsonMessageHelpers.SetJsonMessage(info.Value.Stream, m.ToJsonMessage());
                        }
                        else
                        {
                            MessageHelpers.SetMessage(info.Value.Stream, m);
                        }
                    }
                    else if(info.Value.RoomId == oldRoom.ID)
                    {
                        Message m = new Message("Server", MessageType.Message);
                        m.SetContent(client.Name + " Left your room");
                        m.SetColor(Color.Orange);
                        if(info.Value.ClientType == ClientType.Web)
                        {
                            JsonMessageHelpers.SetJsonMessage(info.Value.Stream, m.ToJsonMessage());
                        }
                        else
                        {
                            MessageHelpers.SetMessage(info.Value.Stream, m);
                        }
                    }
                }
            }
        }

        public void OnUserConnect(ClientInfo client)
        {
            Message m = new Message("Test Bot", MessageType.Message);
            m.SetColor(Color.Blue);
            m.SetContent("Welcome " + client.Name);
            handler.TRequestClients().SendToAll(m, new List<ClientInfo>() { client });
        }

        public void OnUserDisconnect(ClientInfo client)
        {

        } 
    }
}
