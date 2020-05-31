using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ChatLib.DataStates;
using System.Drawing;
using ChatLib.Extras;

namespace ChatLib
{
    public class JsonMessage
    {
        // General JsonMessage Info
        public string Name { get; private set; }
        public string Content { get; private set; }
        public MessageType MessageType { get; private set; }
        public NColor Color { get; private set; }
        public string EndPoint { get; private set; }

        // Status info
        public StatusType StatusType { get; private set; }

        // Other Information
        private readonly string GUID;
        public InfomationType InfomationType { get; private set; }
        public RequestType RequestType { get; private set; }

        public JsonMessage(string name, MessageType messageType, string endPoint = "")
        {
            Name = name;
            MessageType = messageType;
            EndPoint = endPoint;
            GUID = Guid.NewGuid().ToString();
        }

        public void SetName(string name) => Name = name;

        public void SetContent(string content) => Content = content;

        public void SetColor(NColor color) => Color = color;

        public void SetStatusType(StatusType statusType) => StatusType = statusType;

        public void SetEndpoint(string endPoint) => EndPoint = endPoint;

        public void SetInformationType(InfomationType infomationType) => InfomationType = infomationType;

        public void SetRequestType(RequestType requestType) => RequestType = requestType;

        public override string ToString()
        {
            return $"Name: {Name} Content: {Content} MessageType: {MessageType} Color: {Color.ToString()} Endpoint: {EndPoint} StatusType: {StatusType}";
        }
    }
}
