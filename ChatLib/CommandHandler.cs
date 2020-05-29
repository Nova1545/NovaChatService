using ChatLib.Extras;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatLib
{
    public class CommandHandler
    {
        public delegate void Function(string[] parameters, ClientInfo sender);
        Dictionary<string, Function> Commands;
        readonly string Prefix;
        readonly char ParameterDelimiter;

        public CommandHandler(string prefix, char parameterDelimiter)
        {
            Commands = new Dictionary<string, Function>();
            Prefix = prefix;
            ParameterDelimiter = parameterDelimiter;
        }

        public void CreateCommand(string command, Function function)
        {
            Commands.Add(command, function);
        }

        public bool ProcessMessage(Message message, ClientInfo sender)
        {
            if (message.Content.StartsWith(Prefix))
            {
                string command = message.Content.Replace(Prefix, "").Split(ParameterDelimiter)[0];
                if (Commands.ContainsKey(command))
                {
                    Commands[command](message.Content.Replace(Prefix, "").Split(ParameterDelimiter), sender);
                    return true;
                }
            }
            return false;
        }

        public bool ProcessJsonMessage(JsonMessage message, ClientInfo sender)
        {
            if (message.Content.StartsWith(Prefix))
            {
                string command = message.Content.Replace(Prefix, "").Split(ParameterDelimiter)[0];
                if (Commands.ContainsKey(command))
                {
                    Commands[command](message.Content.Replace(Prefix, "").Split(ParameterDelimiter), sender);
                    return true;
                }
            }
            return false;
        }
    }
}
