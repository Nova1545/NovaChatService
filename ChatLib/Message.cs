using System;
using System.Runtime.Serialization;
using ChatLib.DataStates;
using System.Drawing;

namespace ChatLib
{
    [Serializable]
    public class Message
    {
        // General Message info
        public string Name { get; private set; }
        public string Content { get; private set; }
        public MessageType MessageType { get; private set; }
        public Color Color { get; private set; }
        public string EndPoint { get; private set; }

        // File info
        public FileType FileType { get; private set; }
        public int FileLength { get; private set; }
        public string Filename { get; private set; }

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

        public Message(string name, string filename, int fileLength, MessageType type, FileType fileType, string endpoint = "")
        {
            Name = name;
            FileLength = fileLength;
            MessageType = type;
            FileType = fileType;
            Filename = filename;
            EndPoint = endpoint;
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
