using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace Client
{
    public static class RegOps
    {
        public static int WriteSetting(string setting, object value, RegistryValueKind type)
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

        public static void ResetSettings()
        {
            WriteSetting("ShowLog", 1, RegistryValueKind.DWord);
            WriteSetting("NotificationType", "Both", RegistryValueKind.String);
        }

        public static int ReadSettings(ObservableDictionary<string, object> dict)
        {
            int returnCode;

            RegistryKey key = Registry.CurrentUser.OpenSubKey("Software\\NovaStudios\\NovaChatClient\\Settings", true);

            if (key == null)
            {
                key = Registry.CurrentUser.CreateSubKey("Software\\NovaStudios\\NovaChatClient\\Settings", true);
                ResetSettings();
            }

            try
            {
                dict.Add("ShowLog", int.Parse(key.GetValue("ShowLog").ToString()) == 1);
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
