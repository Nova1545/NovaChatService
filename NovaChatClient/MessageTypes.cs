using System;
using Windows.UI;
using ChatLib.Extras;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml;

namespace NovaChatClient
{
    public class ChatDataTemplateSelector : DataTemplateSelector
    {
        public DataTemplate UserMessageTemplate { get; set; }
        public DataTemplate StatusMessageTemplate { get; set; }

        protected override DataTemplate SelectTemplateCore(object item)
        {
            if (item is UserMessage)
            {
                return UserMessageTemplate;
            }
            else if (item is StatusMessage)
            {
                return StatusMessageTemplate;
            }

            return base.SelectTemplateCore(item);
        }

        protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
        {
            return SelectTemplateCore(item);
        }

    }
    public class UserMessage
    {
        public string Name { get; private set; }
        public string Message { get; private set; }
        public DateTime Date { get; private set; }

        public Brush Color { get; private set; }

        public Visibility IsPrivateMessage { get; private set; }

        public UserMessage(string Name, string Message, DateTime Date, NColor Color, bool IsPrivateMessage = false)
        {
            this.Name = Name;
            this.Message = Message;
            this.Date = Date;
            this.Color = new SolidColorBrush(Windows.UI.Color.FromArgb(255, Color.R, Color.G, Color.B));

            if (IsPrivateMessage)
            {
                this.IsPrivateMessage = Visibility.Visible;
            }
            else
            {
                this.IsPrivateMessage = Visibility.Collapsed;
            }
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
