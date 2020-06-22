using Microsoft.Win32;

namespace Client
{
    public static class RegOps
    {
        public static int WriteSetting(string setting, object value, RegistryValueKind type, ref ObservableDictionary<string, object> dict)
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
                AddSettingToDict(setting, value, ref dict);
                returnCode = 0;
            }
            catch
            {
                returnCode = 1;
            }

            key.Dispose();
            return returnCode;
        }

        public static void ResetSettings(ref ObservableDictionary<string, object> dict)
        {
            WriteSetting("ShowLog", 1, RegistryValueKind.DWord, ref dict);
            WriteSetting("NotificationType", "Both", RegistryValueKind.String, ref dict);
        }

        public static object GetSettingFromDict(string key, ObservableDictionary<string, object> dict)
        {
            if (dict.ContainsKey(key))
            {
                return dict[key];
            }
            else
            {
                return null;
            }
        }

        public static void AddSettingToDict(string key, object value, ref ObservableDictionary<string, object> dict)
        {
            if (dict.ContainsKey(key))
            {
                dict[key] = value;
            }
            else
            {
                dict.Add(key, value);
            }
        }

        public static int ReadSettings(ref ObservableDictionary<string, object> dict)
        {
            int returnCode;

            RegistryKey key = Registry.CurrentUser.OpenSubKey("Software\\NovaStudios\\NovaChatClient\\Settings", true);

            if (key == null)
            {
                key = Registry.CurrentUser.CreateSubKey("Software\\NovaStudios\\NovaChatClient\\Settings", true);
                ResetSettings(ref dict);
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
                if (key.GetValue("NotificationType") != null)
                {
                    switch (key.GetValue("NotificationType"))
                    {
                        case "Disabled":
                            dict.Add("NotificationType", NotificationManager.NotificationType.Disabled);
                            break;
                        case "ToastOnly":
                            dict.Add("NotificationType", NotificationManager.NotificationType.ToastOnly);
                            break;
                        case "SoundOnly":
                            dict.Add("NotificationType", NotificationManager.NotificationType.SoundOnly);
                            break;
                        case "Both":
                            dict.Add("NotificationType", NotificationManager.NotificationType.Both);
                            break;
                        default:
                            dict.Add("NotificationType", NotificationManager.NotificationType.Both);
                            break;
                    }
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
