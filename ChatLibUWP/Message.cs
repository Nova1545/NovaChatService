using System;
using ChatLib.DataStates;
using ChatLib.Extras;

namespace ChatLib
{
    [Serializable]
    public class Message
    {
        // General Message info
        public string Name { get; private set; }
        public string Content { get; private set; }
        public MessageType MessageType { get; private set; }
        public NColor Color { get; private set; }
        public string EndPoint { get; private set; }

        //Status info
        public StatusType StatusType { get; private set; }

        // File transfer data
        public byte[] FileContents { get; private set; }
        public string Filename { get; private set; }

        // Other
        public InfomationType InfomationType { get; private set; }
        public RequestType RequestType { get; private set; }

        // Other Information
        private readonly string GUID;

        public Message(string name, MessageType messageType, string endPoint = "")
        {
            Name = name;
            MessageType = messageType;
            EndPoint = endPoint;
            GUID = Guid.NewGuid().ToString();
            Content = "";
        }

        public void SetName(string name) => Name = name;

        public void SetContent(string content) => Content = content;

        public void SetColor(NColor color) => Color = color;

        public void SetStatusType(StatusType statusType) => StatusType = statusType;

        public void SetEndpoint(string endPoint) => EndPoint = endPoint;

        public void SetFileContents(byte[] fileContents) => FileContents = fileContents;

        public void SetFilename(string filename) => Filename = filename;

        public void SetInformationType(InfomationType infomationType) => InfomationType = infomationType;

        public void SetRequestType(RequestType requestType) => RequestType = requestType;
    }
}
