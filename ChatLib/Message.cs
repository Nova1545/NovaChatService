using System;
using System.Runtime.Serialization;
using ChatLib.DataStates;
using System.Drawing;

namespace ChatLib
{
    [Serializable]
    public class Message
    {
        public string Name { get; private set; }
        public string Content { get; private set; }
        public MessageType MessageType { get; private set; }
        public Color Color { get; private set; }
        public string EndPoint { get; private set; }
        public byte[] FileContent { get; private set; }
        public FileType FileType { get; private set; }

        public Message(string name, string content, MessageType type, string endpoint = "")
        {
            Name = name;
            Content = content;
            MessageType = type;
            EndPoint = endpoint;
        }

        public Message(string name, string content, MessageType type, Color color, string endpoint = "")
        {
            Name = name;
            Content = content;
            MessageType = type;
            Color = color;
            EndPoint = endpoint;
        }

        public Message(string name, byte[] fileContent, MessageType type, FileType fileType)
        {
            Name = name;
            FileContent = fileContent;
            MessageType = type;
            FileType = fileType;
            EndPoint = "";
        }

        public Message(Message m)
        {
            Name = m.Name;
            Content = m.Content;
            MessageType = m.MessageType;
        }

        public void SetName(string name)
        {
            Name = name;
        }
    }
}
