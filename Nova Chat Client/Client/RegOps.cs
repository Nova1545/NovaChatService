using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace Client
{
    public static class RegOps
    {
        public static int WriteSetting(string setting, object value, RegistryValueKind type, ObservableDictionary<string, object> dict)
        {
            int returnCode;

            RegistryKey key = Registry.CurrentUser.OpenSubKey("Software\\NovaStudios\\NovaChatClient\\Settings", true);

            if (key == null)
            {
                key = Registry.CurrentUser.CreateSubKey("Software\\NovaStudios\\NovaChatClient\\Settings", true);
            }

            try
            {
                key.SetValue(setting, value, type);
                returnCode = 0;
            }
            catch
            {
                returnCode = 1;
            }

            key.Dispose();
            return returnCode;
        }

        public static void ResetSettings(ObservableDictionary<string, object> dict)
        {
            WriteSetting("ShowLog", 1, RegistryValueKind.DWord, dict);
            WriteSetting("NotificationType", "Both", RegistryValueKind.String, dict);
        }

        public static int ReadSettings(ObservableDictionary<string, object> dict)
        {
            int returnCode;

            RegistryKey key = Registry.CurrentUser.OpenSubKey("Software\\NovaStudios\\NovaChatClient\\Settings", true);

            if (key == null)
            {
                key = Registry.CurrentUser.CreateSubKey("Software\\NovaStudios\\NovaChatClient\\Settings", true);
                ResetSettings(dict);
            }

            try
            {
                if (key.GetValue("ShowLog") != null)
                {
                    dict.Add("ShowLog", int.Parse(key.GetValue("ShowLog").ToString()) == 1);
                }
                if (key.GetValue("ServerPath") != null)
                {
                    dict.Add("ServerPath", key.GetValue("ServerPath").ToString());
                }
                returnCode = 0;
            }
            catch
            {
                returnCode = 1;
            }

            key.Dispose();

            return returnCode;
        }
    }
}
