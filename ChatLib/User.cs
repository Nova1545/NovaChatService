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
using System.Security.Cryptography;

namespace ChatLib
{
    public class User
    {
        public string Name { get; private set; }
        public bool AutoSend { get; private set; }
        public NetworkStream Stream { get; private set; }
        public SslStream SStream { get; private set; }
        public bool IsSecure { get; private set; }

        // Message Received Callbacks
        public delegate void OnMessageAnyReceived(Message message);
        public event OnMessageAnyReceived OnMessageAnyReceivedCallback;

        public delegate void OnMessageStatusReceived(Message message);
        public event OnMessageStatusReceived OnMessageStatusReceivedCallback;

        public delegate void OnMessageTransferReceived(Message message);
        public event OnMessageTransferReceived OnMessageTransferReceivedCallback;

        public delegate void OnMessageReceived(Message message);
        public event OnMessageReceived OnMessageReceivedCallback;

        public delegate void OnMessageWhisperReceived(Message message);
        public event OnMessageWhisperReceived OnMessageWhisperReceivedCallback;

        public delegate void OnMessageInitReceived(Message message);
        public event OnMessageInitReceived OnMessageInitReceivedCallback;

        public delegate void OnMessageRequestReceived(Message message);
        public event OnMessageRequestReceived OnMessageRequestReceivedCallback;

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
            SStream = null;
            Active = true;
            IsSecure = false;
            ThreadPool.QueueUserWorkItem(Listen, Stream);
        }

        public User(string name, SslStream sstream, bool autoSend = true)
        {
            Name = name;
            AutoSend = autoSend;
            SStream = sstream;
            Stream = null;
            Active = true;
            IsSecure = true;
            ThreadPool.QueueUserWorkItem(Listen, SStream);
        }

        public bool Send()
        {
            if (m != null)
            {
                if (IsSecure)
                {
                    MessageHelpers.SetMessage(SStream, m);
                }
                else
                {
                    MessageHelpers.SetMessage(Stream, m);
                }
                m = null;
                return true;
            }
            else
            {
                return false;
            }
        }

        public void CreateMessage(string content, NColor color)
        {
            m = new Message(Name, MessageType.Message);
            m.SetContent(content);
            m.SetColor(color);
            if (AutoSend)
            {
                if (IsSecure)
                {
                    MessageHelpers.SetMessage(SStream, m);
                }
                else
                {
                    MessageHelpers.SetMessage(Stream, m);
                }
                m = null;
            }
        }

        public void CreateWhisper(string content, NColor color, string endpoint)
        {
            m = new Message(Name, MessageType.Whisper, endpoint);
            m.SetContent(content);
            m.SetColor(color);
            if (AutoSend)
            {
                if (IsSecure)
                {
                    MessageHelpers.SetMessage(SStream, m);
                }
                else
                {
                    MessageHelpers.SetMessage(Stream, m);
                }
                m = null;
            }
        }

        public void CreateTransfer(byte[] fileContents, string filename, NColor color, string endpoint="")
        {
            m = new Message(Name, MessageType.Transfer, endpoint);
            m.SetFileContents(fileContents);
            m.SetFilename(filename);
            m.SetColor(color);
            if (AutoSend)
            {
                if (IsSecure)
                {
                    MessageHelpers.SetMessage(SStream, m);
                }
                else
                {
                    MessageHelpers.SetMessage(Stream, m);
                }
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
                if (IsSecure)
                {
                    MessageHelpers.SetMessage(SStream, m);
                }
                else
                {
                    MessageHelpers.SetMessage(Stream, m);
                }
                m = null;
            }
        }

        public void ForwardMessage(Message m)
        {
            if (IsSecure)
            {
                MessageHelpers.SetMessage(SStream, m);
            }
            else
            {
                MessageHelpers.SetMessage(Stream, m);
            }
        }

        public void CreateInformation(InfomationType infomationType)
        {
            m = new Message(Name, MessageType.Infomation);
            m.SetInformationType(infomationType);
            if (AutoSend)
            {
                if (IsSecure)
                {
                    MessageHelpers.SetMessage(SStream, m);
                }
                else
                {
                    MessageHelpers.SetMessage(Stream, m);
                }
                m = null;
            }
        }

        public void Init(string password = "")
        {
            if (IsSecure)
            {
                Message m = new Message(Name, MessageType.Initialize);
                SHA256 sha = SHA256.Create();
                byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(password));
                sha.Dispose();
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < hash.Length; i++)
                {
                    builder.Append(hash[i].ToString("x2"));
                }
                m.SetContent(builder.ToString());
                MessageHelpers.SetMessage(SStream, m);
            }
            else
            {
                Message m = new Message(Name, MessageType.Initialize);
                SHA256 sha = SHA256.Create();
                byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(password));
                sha.Dispose();
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < hash.Length; i++)
                {
                    builder.Append(hash[i].ToString("x2"));
                }
                m.SetContent(builder.ToString());
                MessageHelpers.SetMessage(Stream, m);
            }
        }

        public void Close()
        {
            Active = false;
            if (IsSecure)
            {
                SStream.Close();
                SStream.Dispose();
            }
            else
            {
                Stream.Close();
                Stream.Dispose();
            }
        }

        private void Listen(object stream)
        {
            try
            {
                if (IsSecure)
                {
                    while (Active)
                    {
                        SslStream nStream = (SslStream)stream;
                        Message m = MessageHelpers.GetMessage(nStream);
                        Console.WriteLine(m.MessageType);
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
                            case MessageType.Whisper:
                                OnMessageWhisperReceivedCallback?.Invoke(m);
                                break;
                            case MessageType.Initialize:
                                OnMessageInitReceivedCallback?.Invoke(m);
                                break;
                            case MessageType.Request:
                                OnMessageRequestReceivedCallback?.Invoke(m);
                                break;
                            case MessageType.Infomation:
                                OnMesssageInformationReceivedCallback?.Invoke(m);
                                break;
                        }
                        OnMessageAnyReceivedCallback?.Invoke(m);
                    }
                }
                else
                {
                    NetworkStream nStream = (NetworkStream)stream;
                    while (Active)
                    {
                        Console.WriteLine("Hey! " + Active);
                        Message m = MessageHelpers.GetMessage(nStream);
                        Console.WriteLine("Message");
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
                            case MessageType.Whisper:
                                OnMessageWhisperReceivedCallback?.Invoke(m);
                                break;
                            case MessageType.Initialize:
                                OnMessageInitReceivedCallback?.Invoke(m);
                                break;
                            case MessageType.Request:
                                OnMessageRequestReceivedCallback?.Invoke(m);
                                break;
                            case MessageType.Infomation:
                                OnMesssageInformationReceivedCallback?.Invoke(m);
                                break;
                        }
                        Console.WriteLine("All");
                        OnMessageAnyReceivedCallback?.Invoke(m);
                        Console.WriteLine("All Complete");
                    }
                }
            }
            catch (Exception e)
            {
                OnErrorCallback?.Invoke(e);
            }
        }
    }
}
