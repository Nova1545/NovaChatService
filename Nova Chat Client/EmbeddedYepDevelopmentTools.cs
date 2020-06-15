using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Client
{
    static class EmbeddedYepDevelopmentTools
    {
        public static void KillAll(string processName)
        {
            foreach (var process in Process.GetProcessesByName(processName))
            {
                process.Kill();
            }
        }

        public static void StartProcess(string path, string args = "", string startin = "", bool admin = false)
        {
            Process proc = new Process();
            proc.StartInfo.FileName = path;

            if (args.Length > 0)
            {
                proc.StartInfo.Arguments = args;
            }
            if (admin)
            {
                proc.StartInfo.Verb = "runas";
            }
            if (startin.Length > 0)
            {
                proc.StartInfo.WorkingDirectory = startin;
                proc.StartInfo.UseShellExecute = false;
            }
            proc.Start();
        }

        public static string GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            throw new Exception("No network adapters with an IPv4 address in the system!");
        }
    }
}
