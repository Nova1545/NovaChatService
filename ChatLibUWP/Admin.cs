using ChatLib.DataStates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace ChatLib.Administrator
{
    public struct Admin
    {
        public string Username { get; private set; }
        public string Password { get; private set; }
        public Perms Perms { get; private set; }

        public Admin(string username, string password, Perms perms = Perms.None) : this()
        {
            Username = username;
            Perms = perms;

            SHA256 sha = SHA256.Create();
            byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(password));
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < hash.Length; i++)
            {
                builder.Append(hash[i].ToString("x2"));
            }
            Password = builder.ToString();
        }
    }
}
