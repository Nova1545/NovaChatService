using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatLib.Testing
{
    [Serializable]
    public class TestMessage
    {
        public string Content { get; private set; }
        public string Name { get; private set; }

        public TestMessage(string content, string name)
        {
            Content = content;
            Name = name;
        }
    }
}
