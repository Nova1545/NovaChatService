using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatLib
{
    [AttributeUsage(AttributeTargets.Class)]
    public class Bot : Attribute
    {
        private string name;
        private string desc;
        private string creator;
        private string version;

        public Bot(string name, string desc = "", string creator = "", string version = "1.0.0")
        {
            this.name = name;
            this.desc = desc;
            this.creator = creator;
            this.version = version;
        }

        public virtual string Name
        {
            get { return name; }
        }

        public virtual string Desc
        {
            get { return desc; }
        }

        public virtual string Creator
        {
            get { return creator; }
        }

        public virtual string Version
        {
            get { return version; }
        }
    }
}
