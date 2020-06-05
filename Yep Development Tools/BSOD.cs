using System;
using System.Runtime.InteropServices;

namespace Yep_Development_Tools
{
    /// <summary>
    /// Holds the methods needed to invoke a blue screen.
    /// </summary>
    public static class BSOD
    {
        [DllImport("ntdll.dll")]
        private static extern uint RtlAdjustPrivilege(int Privilege, bool bEnablePrivilege, bool IsThreadPrivilege, out bool PreviousValue);

        [DllImport("ntdll.dll")]
        private static extern uint NtRaiseHardError(uint ErrorStatus, uint NumberOfParameters, uint UnicodeStringParameterMask, IntPtr Parameters, uint ValidResponseOption, out uint Response);

        /// <summary>
        /// Immediately blue screens the computer. Use responsibly.
        /// </summary>
        public static void Invoke()
        {
            RtlAdjustPrivilege(19, true, false, out _);
            NtRaiseHardError(0xc0000022, 0, 0, IntPtr.Zero, 6, out _);
        }
    }
}
