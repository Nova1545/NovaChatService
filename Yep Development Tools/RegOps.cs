using Microsoft.Win32;
using System;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;

namespace Yep_Development_Tools
{
    /// <summary>
    /// <para>Contains methods for all your registry needs.</para>
    /// <para>Uses P/Invoke to manipulate the registry since the Win32 library is too weak.</para>
    /// </summary>
    public static class RegOps
    {
        #region P/Invoke Imports

        [DllImport("Advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern int RegOpenKeyEx(UIntPtr hKey, string lpSubKey, int ulOptions, int samDesired, out UIntPtr phkResult);

        /// <summary>
        /// <para>Create an int first and set it to 0 -> int size = 0;</para>
        /// <para>Then create a pointer like this using the int variable -> IntPtr ptr = Marshal.AllocHGlobal(size);</para>
        /// <para>Then call the function with the following parameters -> RegQueryValueEx(hkey, name, 0, IntPtr.Zero, ptr, ref size));</para>
        /// <para>Finally, get the key's value (presumably a string) like this -> Marshal.PtrToStringUni(ptr, size / sizeof(char)).TrimEnd('\0');</para>
        /// </summary>
        /// <param name="hKey"></param>
        /// <param name="lpValueName"></param>
        /// <param name="lpReserved"></param>
        /// <param name="type"></param>
        /// <param name="lpData"></param>
        /// <param name="lpcbData"></param>
        /// <returns></returns>
        [DllImport("advapi32.dll", CharSet = CharSet.Unicode)]
        private static extern int RegQueryValueEx(UIntPtr hKey, string lpValueName, int lpReserved, IntPtr type, IntPtr lpData, ref int lpcbData);


        [DllImport("Advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern int RegCloseKey(UIntPtr hKey);


        [DllImport("Shlwapi.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern int SHDeleteKey(UIntPtr hKey, string pszSubKey);

        
        [DllImport("Advapi32", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int RegSetKeySecurity(UIntPtr hKey, SECURITY_INFORMATION SecurityInformation, IntPtr pSecurityDescriptor);

        [DllImport("advapi32.dll", SetLastError = false)]
        private static extern int RegCreateKeyEx(IntPtr hKey, 
            string lpSubKey, 
            IntPtr Reserved, 
            string lpClass, 
            RegOption dwOptions, 
            RegSAM samDesired, 
            ref SECURITY_ATTRIBUTES lpSecurityAttributes, 
            out IntPtr phkResult, 
            out RegResult lpdwDisposition);

        [DllImport("advapi32.dll", SetLastError = true)]
        static extern bool ConvertStringSecurityDescriptorToSecurityDescriptor(
          string StringSecurityDescriptor,
          uint StringSDRevision,
          out IntPtr SecurityDescriptor,
          out UIntPtr SecurityDescriptorSize
        );

        #endregion

#pragma warning disable CS1591

        #region Enums
        [Flags]
        public enum RegOption
        {
            NonVolatile = 0x0,
            Volatile = 0x1,
            CreateLink = 0x2,
            BackupRestore = 0x4,
            OpenLink = 0x8
        }

        [Flags]
        public enum RegSAM
        {
            QueryValue = 0x0001,
            SetValue = 0x0002,
            CreateSubKey = 0x0004,
            EnumerateSubKeys = 0x0008,
            Notify = 0x0010,
            CreateLink = 0x0020,
            WOW64_32Key = 0x0200,
            WOW64_64Key = 0x0100,
            WOW64_Res = 0x0300,
            Read = 0x00020019,
            Write = 0x00020006,
            Execute = 0x00020019,
            AllAccess = 0x000f003f
        }

        public enum RegResult
        {
            CreatedNewKey = 0x00000001,
            OpenedExistingKey = 0x00000002
        }

        public enum SECURITY_INFORMATION : uint {
            OWNER_SECURITY_INFORMATION = 0x00000001, 
            GROUP_SECURITY_INFORMATION = 0x00000002, 
            DACL_SECURITY_INFORMATION = 0x00000004, 
            SACL_SECURITY_INFORMATION = 0x00000008, 
            PROTECTED_DACL_SECURITY_INFORMATION = 0x80000000, 
            PROTECTED_SACL_SECURITY_INFORMATION = 0x40000000, 
            UNPROTECTED_DACL_SECURITY_INFORMATION = 0x20000000, 
            UNPROTECTED_SACL_SECURITY_INFORMATION = 0x10000000 };

        #endregion

        #region Constants

        public const uint HKEY_CLASSES_ROOT = 0x80000000;
        public const uint HKEY_LOCAL_MACHINE = 0x80000002;
        public const uint HKEY_USERS = 0x80000003;
        public const uint HKEY_CURRENT_USER = 0x80000001;

        public const int READ_CONTROL = 0x00020000;
        public const int SYNCHRONIZE = 0x00100000;

        public const int STANDARD_RIGHTS_READ = READ_CONTROL;
        public const int STANDARD_RIGHTS_WRITE = READ_CONTROL;

        public const int KEY_QUERY_VALUE = 0x0001;
        public const int KEY_SET_VALUE = 0x0002;
        public const int KEY_CREATE_SUB_KEY = 0x0004;
        public const int KEY_ENUMERATE_SUB_KEYS = 0x0008;
        public const int KEY_NOTIFY = 0x0010;
        public const int KEY_CREATE_LINK = 0x0020;
        public const int KEY_READ = ((STANDARD_RIGHTS_READ |
                                                           KEY_QUERY_VALUE |
                                                           KEY_ENUMERATE_SUB_KEYS |
                                                           KEY_NOTIFY)
                                                          &
                                                          (~SYNCHRONIZE));

        public const int KEY_WRITE = ((STANDARD_RIGHTS_WRITE |
                                                           KEY_SET_VALUE |
                                                           KEY_CREATE_SUB_KEY)
                                                          &
                                                          (~SYNCHRONIZE));
        public const int KEY_WOW64_64KEY = 0x0100;
        public const int KEY_WOW64_32KEY = 0x0200;

        public const int DELETE = 0x00010000;

        #endregion

        #region Structs

        [StructLayout(LayoutKind.Sequential)]
        public struct SECURITY_DESCRIPTOR
        {
            public byte revision;
            public byte size;
            public short control;
            public IntPtr owner;
            public IntPtr group;
            public IntPtr sacl;
            public IntPtr dacl;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SECURITY_ATTRIBUTES
        {
            public int nLength;
            public IntPtr lpSecurityDescriptor;
            public int bInheritHandle;
        }

        #endregion

#pragma warning restore CS1591

        /// <summary>
        /// <para>Changes the owner of the specified key.</para>
        /// <para>Not yet implemented</para>
        /// </summary>
        /// <param name="rootKey"></param>
        /// <param name="subKey"></param>
        /// <param name="keyName"></param>
        /// <param name="owner"></param>
        /// <param name="group"></param>
        public static void ChangeKeyOwnership(uint rootKey, string subKey, string keyName, WindowsBuiltInRole owner, WindowsBuiltInRole group)
        {
            UIntPtr keyHandle;
            IntPtr dataPtr = IntPtr.Zero;

            int handleReturn = RegOpenKeyEx((UIntPtr)rootKey, subKey, 0, KEY_WRITE | KEY_WOW64_64KEY, out keyHandle);

            if (handleReturn == 0)
            {
                SECURITY_DESCRIPTOR securityInfo = new SECURITY_DESCRIPTOR();
                securityInfo.owner = (IntPtr)owner;
                securityInfo.group = (IntPtr)group;

                IntPtr securityInfoPtr = IntPtr.Zero;

                Marshal.StructureToPtr(securityInfo, securityInfoPtr, false);

                RegSetKeySecurity(keyHandle, SECURITY_INFORMATION.OWNER_SECURITY_INFORMATION, securityInfoPtr);
            }
        }

        /// <summary>
        /// <para>Changes the security permissions of the specified key.</para>
        /// <para>Not yet implemented</para>
        /// </summary>
        /// <param name="rootKey"></param>
        /// <param name="subKey"></param>
        /// <param name="keyName"></param>
        public static void ChangeKeyACL(uint rootKey, string subKey, string keyName)
        {
            UIntPtr keyHandle;
            
            int handleReturn = RegOpenKeyEx((UIntPtr)rootKey, subKey, 0, KEY_READ | KEY_WRITE | KEY_WOW64_64KEY, out keyHandle);

            if (handleReturn == 0)
            {
                IntPtr securityInfoPtr = IntPtr.Zero;

                /*ConvertStringSecurityDescriptorToSecurityDescriptor("O:BA" +
                    "G:BA" +
                    "D:AI" +
                    "S:AI", out securityInfoPtr);*/

                //Marshal.StructureToPtr(securityInfo, securityInfoPtr, false);

                RegSetKeySecurity(keyHandle, SECURITY_INFORMATION.DACL_SECURITY_INFORMATION, securityInfoPtr);
            }
        }

        /// <summary>
        /// Creates a key at the specified path.
        /// </summary>
        public static void CreateKey()
        {
            
        }

        /// <summary>
        /// Opens the specified path and deletes the key or subkey.
        /// </summary>
        /// <param name="rootKey"></param>
        /// <param name="path"></param>
        /// <param name="key"></param>
        public static void DeleteKey(uint rootKey, string path, string key)
        {
            UIntPtr keyHandle;
            int nStatus1 = RegOpenKeyEx((UIntPtr)rootKey, path, 0, DELETE | KEY_READ | KEY_WRITE | KEY_WOW64_64KEY, out keyHandle);
            if (nStatus1 == 0)
            {
                SHDeleteKey(keyHandle, key);
                RegCloseKey(keyHandle);
            }
        }

        /// <summary>
        /// <para>Opens the specified key, reads it, and returns the value as a string.</para>
        /// <para>If the key's size is greater than 1024 bytes, then you need to specify a custom buffer size that will fit the size of the key. Otherwise, jumbled information will be returned to you.</para>
        /// <para>Returns "Error" if there was an issue accessing the key</para>
        /// </summary>
        /// <param name="rootKey"></param>
        /// <param name="subKey"></param>
        /// <param name="keyName"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        public static string ReadKey(uint rootKey, string subKey, string keyName, int size = 1024)
        {
            UIntPtr keyHandle;
            IntPtr dataPtr = IntPtr.Zero;

            int handleReturn = RegOpenKeyEx((UIntPtr)rootKey, subKey, 0, KEY_READ | KEY_WOW64_64KEY, out keyHandle);

            if (handleReturn == 0)
            {
                dataPtr = Marshal.AllocHGlobal(size);

                RegQueryValueEx(keyHandle, keyName, 0, IntPtr.Zero, dataPtr, ref size);

                string keyValue = Marshal.PtrToStringUni(dataPtr, size / sizeof(char)).TrimEnd('\0');

                Marshal.FreeHGlobal(dataPtr);

                return keyValue;
            }
            else
            {
                Marshal.FreeHGlobal(dataPtr);

                return "Error";
            }
        }
    }

    /// <summary>
    /// Simplifies the Win32 library's methods for manipulating the registry.
    /// </summary>
    public static class ManagedRegOps
    {
        /// <summary>
        /// <para>Opens the specified key, reads it, and returns the value as a string.</para>
        /// <para>Returns "Error" if there was an issue accessing the key</para>
        /// </summary>
        /// <param name="path"></param>
        /// <param name="keyName"></param>
        /// <returns></returns>
        public static string ReadKey(string path, string keyName)
        {
            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(path))
            {
                if (key != null)
                {
                    return key.GetValue(keyName).ToString();
                }
                else
                {
                    return "Error";
                }
            }

        }

        /// <summary>
        /// Creates a key at the specified path.
        /// </summary>
        public static void CreateKey(string path, string key, object value, RegistryValueKind type, RegistryHive hive)
        {
            RegistryKey regKey;

            switch (hive)
            {
                case RegistryHive.CurrentUser:
                    regKey = Registry.CurrentUser.OpenSubKey(path, true);
                    if (regKey == null)
                    {
                        regKey = Registry.CurrentUser.CreateSubKey(path, true);
                    }
                    break;

                case RegistryHive.LocalMachine:
                    regKey = Registry.LocalMachine.OpenSubKey(path, true);
                    if (regKey == null)
                    {
                        regKey = Registry.LocalMachine.CreateSubKey(path, true);
                    }
                    break;

                case RegistryHive.ClassesRoot:
                    regKey = Registry.ClassesRoot.OpenSubKey(path, true);
                    if (regKey == null)
                    {
                        regKey = Registry.ClassesRoot.CreateSubKey(path, true);
                    }
                    break;

                case RegistryHive.Users:
                    regKey = Registry.Users.OpenSubKey(path, true);
                    if (regKey == null)
                    {
                        regKey = Registry.Users.CreateSubKey(path, true);
                    }
                    break;

                default:
                    regKey = Registry.CurrentUser.OpenSubKey(path, true);
                    if (regKey == null)
                    {
                        regKey = Registry.CurrentUser.CreateSubKey(path, true);
                    }
                    break;
            }

            try
            {
                regKey.SetValue(key, value, type);
            }
            catch
            {
                Console.WriteLine("Write Failed");
            }

            regKey.Dispose();
        }
    }

}
