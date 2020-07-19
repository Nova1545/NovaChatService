using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NovaChatClient
{
    public class FormattedMessage
    {
        public string Name { get; private set; }
        public string Message { get; private set; }
        public DateTime Date { get; private set; }

        public FormattedMessage(string Name, string Message, DateTime Date)
        {
            this.Name = Name;
            this.Message = Message;
            this.Date = Date;
        }
    }
}
