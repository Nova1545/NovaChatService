using System;
using System.Windows.Forms;

namespace Client
{
    class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Tcp_Client());
        }
    }
}
