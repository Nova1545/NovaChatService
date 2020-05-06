using Microsoft.Win32;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace Client
{
    public partial class Settings : Form
    {
        public Settings()
        {
            InitializeComponent();
        }

        private void Settings_Load(object sender, EventArgs e)
        {
            if (toggleLog.Tag.ToString() == "true")
            {
                toggleLog.Text = "Hide Log";
            }
            else
            {
                toggleLog.Text = "Show Log";
            }
        }

        private void WriteSetting(string setting, string value)
        {
            RegistryKey key = Registry.CurrentUser.OpenSubKey("Software\\NovaStudios\\NovaChatClient\\Settings", true);

                if (key == null)
                {
                    key = Registry.CurrentUser.CreateSubKey("Software\\NovaStudios\\NovaChatClient\\Settings", true);
                }

                try
                {
                    key.SetValue(setting, value, RegistryValueKind.String);
                }
                catch
                {
                    (Application.OpenForms["Tcp_Client"] as Tcp_Client).printToLog("Something went wrong while saving settings! Please report this bug to the creator.", Color.Red);
                }

            key.Dispose();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void toggleLog_Click(object sender, EventArgs e)
        {
            if (toggleLog.Tag != null)
            {
                if (toggleLog.Tag.ToString() == "true")
                {
                    (Application.OpenForms["Tcp_Client"] as Tcp_Client).toggleLogVisibility(false);
                    toggleLog.Tag = "false";
                    toggleLog.Text = "Show Log";
                    WriteSetting("ShowLog", "false");
                }
                else
                {
                    (Application.OpenForms["Tcp_Client"] as Tcp_Client).toggleLogVisibility(true);
                    toggleLog.Tag = "true";
                    toggleLog.Text = "Hide Log";
                    WriteSetting("ShowLog", "true");
                }
            }
            else
            {
                (Application.OpenForms["Tcp_Client"] as Tcp_Client).toggleLogVisibility(true);
                toggleLog.Tag = "true";
                toggleLog.Text = "Hide Log";
                WriteSetting("ShowLog", "true");
            }
        }
    }
}
