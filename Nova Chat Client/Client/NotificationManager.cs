using System;
using System.Collections.Generic;
using System.Linq;
using System.Media;
using System.Text;
using System.Threading.Tasks;

namespace Client
{
    class NotificationManager
    {
        SoundPlayer player = new SoundPlayer();
        public void ShowNotification(string message)
        {
            player.Stream = Properties.Resources.Notification;
            player.Play();
        }
    }
}
