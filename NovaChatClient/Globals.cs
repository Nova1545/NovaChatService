using ChatLib.Extras;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace NovaChatClient
{
    public static class Globals
    {
        public static string Username { get; set; }
        public static string Address { get; set; }
        public static int Port { get; set; }
        public static SecureString AdminPassword { get; set; }
        public static NColor Color { get; set; }
    }
}
