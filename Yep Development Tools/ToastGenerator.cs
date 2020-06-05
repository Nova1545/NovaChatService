using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

namespace Yep_Development_Tools
{
    /// <summary>
    /// Toast generation in Win32 apps, simplified.
    /// </summary>
    public class ToastGenerator
    {
        /// <summary>
        /// The ToastNotification instance. Use this to subscribe to event handlers.
        /// </summary>
        public ToastNotification toastNotification;

        /// <summary>
        /// <para>Sends a toast notification with the given title and body. The App Name is what the header will appear as in the action center.</para>
        /// <para>Does not support images at the moment.</para>
        /// <para>(Only works on Windows 8/10)</para>
        /// </summary>
        /// <param name="appName"></param>
        /// <param name="title"></param>
        /// <param name="body"></param>
        public void MakeToast(string appName, string title, string body)
        {
            XmlDocument xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(string.Concat(new string[]
            {
                "<toast duration=\"short\"><visual><binding template=\"ToastText04\">" +
                "<text id=\"1\">",
                title,
                "</text><text id=\"2\">",
                body,
                "</text></binding></visual></toast>"
            }));
            toastNotification = new ToastNotification(xmlDocument);
            ToastNotificationManager.CreateToastNotifier(appName).Show(toastNotification);
        }
    }
}
