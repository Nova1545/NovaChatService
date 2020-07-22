using System;
using Windows.UI;
using ChatLib.Extras;
using Windows.UI.Xaml.Media;

namespace NovaChatClient
{
    public class UserMessage
    {
        public string Name { get; private set; }
        public string Message { get; private set; }
        public DateTime Date { get; private set; }

        public Brush Color { get; private set; }

        public UserMessage(string Name, string Message, DateTime Date, NColor Color)
        {
            this.Name = Name;
            this.Message = Message;
            this.Date = Date;
            this.Color = new SolidColorBrush(Windows.UI.Color.FromArgb(255, Color.R, Color.G, Color.B));
        }
    }

    public class StatusMessage
    {
        public string Header { get; private set; }
        public string Message { get; private set; }

        public StatusMessage(string Header, string Message)
        {
            this.Header = Header;
            this.Message = Message;
        }
    }
}
