using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using ChatLib.Extras;
using ChatLib.DataStates;
using System.Drawing;
using System.Net.Security;
using ChatLib;

namespace Client
{
    public class User
    {
        public string Name { get; private set; }
        public bool AutoSend { get; private set; }
        public NetworkStream Stream { get; private set; }

        // Message Received Callbacks
        public delegate void OnMessageAnyReceived(Message message);
        public event OnMessageAnyReceived OnMessageAnyReceivedCallback;

        public delegate void OnMessageStatusReceived(Message message);
        public event OnMessageStatusReceived OnMessageStatusReceivedCallback;

        public delegate void OnMessageTransferReceived(Message message);
        public event OnMessageTransferReceived OnMessageTransferReceivedCallback;

        public delegate void OnMessageReceived(Message message);
        public event OnMessageReceived OnMessageReceivedCallback;

        public delegate void OnMessageWisperReceived(Message message);
        public event OnMessageWisperReceived OnMessageWisperReceivedCallback;

        public delegate void OnMessageInitReceived(Message message);
        public event OnMessageInitReceived OnMessageInitReceivedCallback;

        public delegate void OnMesssageInformationReceived(Message message);
        public event OnMesssageInformationReceived OnMesssageInformationReceivedCallback;

        public delegate void OnError(Exception exception);
        public event OnError OnErrorCallback;

        // If you choose to not use auto send
        private Message m;

        private bool Active;
        
        public User(string name, NetworkStream stream, bool autoSend = true)
        {
            Name = name;
            AutoSend = autoSend;
            Stream = stream;
            Active = true;
            ThreadPool.QueueUserWorkItem(Listen, Stream);
        }

        public bool Send()
        {
            if (m != null)
            {
                MessageHelpers.SetMessage(Stream, m);
                m = null;
                return true;
            }
            else
            {
                return false;
            }
        }

        public void CreateMessage(string content, Color color)
        {
            m = new Message(Name, MessageType.Message);
            m.SetContent(content);
            m.SetColor(color);
            if (AutoSend)
            {
                MessageHelpers.SetMessage(Stream, m);
                m = null;
            }
        }

        public void CreateWisper(string content, Color color, string endpoint)
        {
            m = new Message(Name, MessageType.Wisper, endpoint);
            m.SetContent(content);
            m.SetColor(color);
            if (AutoSend)
            {
                MessageHelpers.SetMessage(Stream, m);
                m = null;
            }
        }

        public void CreateTransfer(byte[] fileContents, string filename, Color color, string endpoint="")
        {
            m = new Message(Name, MessageType.Transfer, endpoint);
            m.SetFileContents(fileContents);
            m.SetFilename(filename);
            m.SetColor(color);
            if (AutoSend)
            {
                MessageHelpers.SetMessage(Stream, m);
                m = null;
            }
        }

        public void CreateStatus(StatusType status, string content = "")
        {
            m = new Message(Name, MessageType.Status);
            m.SetStatusType(status);
            m.SetContent(content);
            if (AutoSend)
            {
                MessageHelpers.SetMessage(Stream, m);
                m = null;
            }
        }

        public void ForwardMessage(Message m)
        {
            MessageHelpers.SetMessage(Stream, m);
        }

        public void CreateInformation(InfomationType infomationType)
        {
            m = new Message(Name, MessageType.Infomation);
            m.SetInformationType(infomationType);
            if (AutoSend)
            {
                MessageHelpers.SetMessage(Stream, m);
                m = null;
            }
        }

        public void Init()
        {
            MessageHelpers.SetMessage(Stream, new Message(Name, MessageType.Initionalize));
        }

        public void Close()
        {
            Active = false;
            Stream.Close();
            Stream.Dispose();
        }

        private void Listen(object stream)
        {
            try
            {
                NetworkStream nStream = (NetworkStream)stream;
                while (Active)
                {
                    Message m = MessageHelpers.GetMessage(nStream);
                    switch (m.MessageType)
                    {
                        case MessageType.Message:
                            OnMessageReceivedCallback?.Invoke(m);
                            break;
                        case MessageType.Status:
                            OnMessageStatusReceivedCallback?.Invoke(m);
                            break;
                        case MessageType.Transfer:
                            OnMessageTransferReceivedCallback?.Invoke(m);
                            break;
                        case MessageType.Wisper:
                            OnMessageWisperReceivedCallback?.Invoke(m);
                            break;
                        case MessageType.Initionalize:
                            OnMessageInitReceivedCallback?.Invoke(m);
                            break;
                        case MessageType.Infomation:
                            OnMesssageInformationReceivedCallback?.Invoke(m);
                            break;
                    }
                    OnMessageAnyReceivedCallback?.Invoke(m);
                }
            }
            catch (Exception e)
            {
                OnErrorCallback?.Invoke(e);
            }
        }
    }
}
