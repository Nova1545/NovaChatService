using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ChatLib;
using ChatLib.Json;
using ChatLib.Extras;
using ChatLib.DataStates;
using ChatLib.Extentions;

namespace BotTemplate
{
    [Bot("Template", "Template Bot", "You", "1.0.0")]
    public class TemplateBot
    {
        BotHandler handler;
        CommandHandler command;

        public void Init(BotHandler handler)
        {
            this.handler = handler;
            this.command = new CommandHandler("/", ' ');
        }

        public Message OnMessage(ClientInfo sender, Message message)
        {    
            
            return message;
        }

        public JsonMessage OnJsonMessage(ClientInfo sender, JsonMessage message)
        {     
            
            return message;
        }

        public void OnUserChangeRoom(ClientInfo client, Room oldRoom, Room newRoom)
        {
            
        }

        public void OnUserConnect(ClientInfo client)
        {

        }

        public void OnUserDisconnect(ClientInfo client)
        {

        }
    }
}
