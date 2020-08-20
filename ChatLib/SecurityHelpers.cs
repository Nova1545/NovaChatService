using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace ChatLib.Security
{
    public static class SecurityHelpers
    {
        public static string SecureToString(this SecureString sString)
        {
            IntPtr uString = IntPtr.Zero;
            try
            {
                uString = Marshal.SecureStringToGlobalAllocUnicode(sString);
                return Marshal.PtrToStringUni(uString);
            }
            finally
            {
                Marshal.ZeroFreeGlobalAllocUnicode(uString);
            }
        }

        public static SecureString StringToSecure(this string unsString)
        {
            SecureString ss = new SecureString();
            foreach (char c in unsString)
            {
                ss.AppendChar(c);
            }
            return ss;
        }
    }
}
