using System;
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

        //Ststus info
        public StatusType StatusType { get; private set; }

        // File transfer data
        public byte[] FileContents { get; private set; }
        public string Filename { get; private set; }

        // Other Information
        private readonly string MessageID;

        public Message(string name, MessageType messageType, string endPoint = "")
        {
            Name = name;
            MessageType = messageType;
            EndPoint = endPoint;
            MessageID = Guid.NewGuid().ToString();
        }

        public void SetName(string name) => Name = name;

        public void SetContent(string content) => Content = content;

        public void SetColor(Color color) => Color = color;

        public void SetStatusType(StatusType statusType) => StatusType = statusType;

        public void SetFileContents(byte[] fileContents) => FileContents = fileContents;

        public void SetFilename(string filename) => Filename = filename;
    }
}
