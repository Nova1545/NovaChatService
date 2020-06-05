using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Yep_Development_Tools
{
    /// <summary>
    /// Methods for manipulating and querying processes.
    /// </summary>
    public static class ProcessUtils
    {
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        /// <summary>
        /// Gets the Process object of the current process in focus.
        /// </summary>
        /// <returns></returns>
        public static Process GetForegroundProcess()
        {
            uint processID = 0;
            IntPtr hWnd = GetForegroundWindow();
            uint threadID = GetWindowThreadProcessId(hWnd, out processID);
            Process fgProc = Process.GetProcessById(Convert.ToInt32(processID));
            return fgProc;
        }

        /// <summary>
        /// Gets the name of the current process in focus.
        /// </summary>
        /// <returns></returns>
        public static string GetForegroundProcessName()
        {
            uint processID = 0;
            IntPtr hWnd = GetForegroundWindow();
            uint threadID = GetWindowThreadProcessId(hWnd, out processID);
            Process fgProc = Process.GetProcessById(Convert.ToInt32(processID));
            return fgProc.ProcessName;
        }

        /// <summary>
        /// <para>Quickly start an external process.</para>
        /// </summary>
        /// <param name="path"></param>
        /// <param name="args"></param>
        /// <param name="startin"></param>
        /// <param name="admin"></param>
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

        /// <summary>
        /// Automatically quits the program if there is already an instance of this process.
        /// </summary>
        public static void RestrictOneProcess()
        {
            if (Process.GetProcessesByName(Path.GetFileNameWithoutExtension(Assembly.GetEntryAssembly().Location)).Count() > 1)
            {
                Environment.Exit(1);
            }
        }

        /// <summary>
        /// As the name suggests, kills all processes of the matching name.
        /// </summary>
        /// <param name="processName"></param>
        public static void KillAll(string processName)
        {
            foreach (var process in Process.GetProcessesByName(processName))
            {
                process.Kill();
            }
        }
    }
}
