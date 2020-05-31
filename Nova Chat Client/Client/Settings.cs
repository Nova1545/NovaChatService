using Microsoft.Win32;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace Client
{
    public partial class Settings : Form
    {
        private Tcp_Client parent;
        public Settings(Tcp_Client parent)
        {
            InitializeComponent();
            this.parent = parent;
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
                    parent.printToLog("Something went wrong while saving settings! Please report this bug to the creator.", Color.Red);
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
                    parent.toggleLogVisibility(false);
                    toggleLog.Tag = "false";
                    toggleLog.Text = "Show Log";
                    WriteSetting("ShowLog", "false");
                }
                else
                {
                    parent.toggleLogVisibility(true);
                    toggleLog.Tag = "true";
                    toggleLog.Text = "Hide Log";
                    WriteSetting("ShowLog", "true");
                }
            }
            else
            {
                parent.toggleLogVisibility(true);
                toggleLog.Tag = "true";
                toggleLog.Text = "Hide Log";
                WriteSetting("ShowLog", "true");
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Good try", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            textBox1.Text = "(" + parent.color.R.ToString() + ", " + parent.color.G.ToString() + ", " + parent.color.B.ToString() + ")";
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            
        }
    }
}
