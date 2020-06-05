using System;
using System.Drawing;
using System.Net;
using System.Net.Sockets;
using System.Security.Principal;
using System.Windows.Forms;

namespace Yep_Development_Tools
{
    /// <summary>
    /// A series of helpful methods that removes the need for instantiating objects for certain tasks and allows for quick, clean code.
    /// </summary>
    public static class Tools
    {
        /// <summary>
        /// <para>Checks if the program has elevated privileges.</para>
        /// <para>Returns true if the program is elevated, else returns false.</para>
        /// </summary>
        /// <returns></returns>
        public static bool IsAdministrator()
        {
            return new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);
        }

        /// <summary>
        /// If you're used to Python and other languages that use print(), or if you're just too lazy to type Console.WriteLine(), then you can use this.
        /// </summary>
        /// <param name="text"></param>
        public static void Print(object text=null)
        {
            Console.WriteLine(text);
        }

        /// <summary>
        /// If you're too lazy to capitalize the P in the already simplified print method, then use this. Does the same thing.
        /// </summary>
        /// <param name="text"></param>
        public static void print(object text=null)
        {
            Console.WriteLine(text);
        }

        /// <summary>
        /// Returns a random integer between the minimum and maximum numbers, inclusive.
        /// </summary>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        public static int RandInt(int min, int max)
        {
            Random rng = new Random();
            return new Random().Next(min, max + 1);
        }

        /// <summary>
        /// Determines what text to display depending on the amount of space available in the given WinForm control.
        /// </summary>
        /// <param name="normal"></param>
        /// <param name="truncated"></param>
        /// <param name="control"></param>
        /// <param name="font"></param>
        /// <returns></returns>
        public static string DynamicText(string normal, string truncated, Control control, Font font)
        {
            if (TextRenderer.MeasureText(normal, font).Width < control.Width)
            {
                return normal;
            }
            else
            {
                return truncated;
            }
        }

        /// <summary>
        /// Returns the local IPv4 address of the currently connected network. If there are multiple, returns the one from the first adapter.
        /// </summary>
        /// <returns></returns>
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
