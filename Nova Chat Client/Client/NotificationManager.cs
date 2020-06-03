using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Media;
using System.Text;
using System.Threading.Tasks;
using Yep_Development_Tools;

namespace Client
{
    class NotificationManager
    {
        public enum NotificationType
        {
            Disabled,
            ToastOnly,
            SoundOnly,
            Both
        }

        SoundPlayer player;
        ToastGenerator generator = new ToastGenerator();
        public NotificationType SelectedStyle { get; private set; }
        private string SoundLocation = "";

        string appName = "Nova Chat";

        public void SetSoundLocation(string path)
        {

        }

        public void SetNotififcationStyle(NotificationType style)
        {
            SelectedStyle = style;
        }

        public void ShowNotification(string sender, string message)
        {
            SelectedStyle = NotificationType.Both;

            switch (SelectedStyle)
            {
                case NotificationType.SoundOnly:
                    player = new SoundPlayer();

                    if (SoundLocation.Length <= 0)
                    {
                        player.Stream = Properties.Resources.Notification;
                    }
                    else
                    {
                        player.SoundLocation = SoundLocation;
                    }

                    player.Play();
                    player.Dispose();

                    break;

                case NotificationType.ToastOnly:
                    generator.MakeToast(appName, sender, message);
                    break;

                case NotificationType.Both:
                    generator.MakeToast(appName, sender, message);

                    player = new SoundPlayer();

                    if (SoundLocation.Length <= 0)
                    {
                        player.Stream = Properties.Resources.Notification;
                    }
                    else
                    {
                        player.SoundLocation = SoundLocation;
                    }

                    player.Play();
                    player.Dispose();
                    break;

                default:
                    // Do nothing
                    break;

            }
        }
    }
}
