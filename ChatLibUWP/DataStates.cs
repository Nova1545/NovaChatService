using System;

namespace ChatLib.DataStates
{
    public enum MessageType { Message, Initialize, Status, Whisper, Transfer, Infomation, Request }

    public enum StatusType { Connected, Disconnected, ErrorDisconnect, Disconnecting, ChangeRoom }

    public enum ClientType { Web, Desktop, Android, IOS }

    public enum InfomationType { ConnectedUsers, ServerUptime, ConnectTime, MessagesSent, Rooms }

    public enum RequestType { Rooms, Users }

    public enum MessageState { Fine, Terminate }

    [Flags]
    public enum Perms { Kick, Ban, Mute, Move, None }

    public enum RevokedPerms { Banned, Muted, None }
}
