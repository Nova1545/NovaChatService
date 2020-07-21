using ChatLib.DataStates;
using ChatLib.Extras;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatLib
{
    public class BotAb
    {
        public BotHandler BotHandler;
        public CommandHandler CommandHandler;

        public BotAb(BotHandler bot, string commandPrefix = "/", char delimiter = ' ')
        {
            BotHandler = bot;
            CommandHandler = new CommandHandler(commandPrefix, delimiter);
        }

        public virtual MessageState OnMessage(Message message, ClientInfo sender)
        {
            CommandHandler.ProcessMessage(message, sender);
            return MessageState.Fine;
        }

        public virtual MessageState OnJsonMessage(JsonMessage message, ClientInfo sender)
        {
            CommandHandler.ProcessJsonMessage(message, sender);
            return MessageState.Fine;
        }

        public virtual void OnUserChangeRoom(ClientInfo client, Room oldRoom, Room newRoom) { }
        public virtual void OnUserConnect(ClientInfo client) { }
        public virtual void OnUserDisconnect(ClientInfo client) { }
    }
}
