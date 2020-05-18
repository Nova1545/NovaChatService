namespace ChatLib.DataStates
{
    public enum MessageType { Message, Initionalize, Status, Wisper, Transfer, Infomation }

    public enum StatusType { Connected, Disconnected, ErrorDisconnect, Disconnecting }

    public enum ClientType { Web, Desktop }

    public enum InfomationType { ConnectedUsers, ServerUptime, ConnectTime, MessagesSent }
}
