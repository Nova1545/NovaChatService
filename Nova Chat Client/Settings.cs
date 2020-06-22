using ChatLib.Extras;
using Microsoft.Win32;
using System;
using System.Drawing;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;
using static Client.NotificationManager;

namespace Client
{
    public partial class Settings : Form
    {
        private Tcp_Client parent;
        private ObservableDictionary<string, object> settings;
        private NotificationManager notifications;
        private ObservableDictionary<string, Room> rooms;

        public Settings(Tcp_Client parent, ref ObservableDictionary<string, object> settingsDictionary, NotificationManager notifications, ObservableDictionary<string, Room> rooms)
        {
            InitializeComponent();
            this.parent = parent;
            this.settings = settingsDictionary;
            this.notifications = notifications;
            this.rooms = rooms;

            Task.Run(() =>
            {
                ColorSelector.Items.Clear();

                PropertyInfo[] colors = typeof(Color).GetProperties();

                for (int i = 0; i < colors.Length; i++)
                {
                    if (colors[i].PropertyType == typeof(Color))
                    {
                        MethodInfo getMethod = colors[i].GetGetMethod();
                        if ((getMethod != null) && ((getMethod.Attributes & (MethodAttributes.Static | MethodAttributes.Public)) == (MethodAttributes.Static | MethodAttributes.Public)))
                        {
                            object[] index = null;
                            ColorSelector.Items.Add((Color)colors[i].GetValue(null, index));
                        }
                    }
                }
            });
        }

        private void Settings_Load(object sender, EventArgs e)
        {
            ColorSelectorDisplay.Text = parent.GetFormattedTagColor();

            RoomSelector.Items.Clear();

            if (parent.IsConnected())
            {
                
            }

            if (bool.Parse(RegOps.GetSettingFromDict("ShowLog", settings).ToString()))
            {
                toggleLog.Text = "Hide Log";
            }
            else
            {
                toggleLog.Text = "Show Log";
            }

            if (parent.IsConnected())
            {
                ColorSelector.Enabled = true;
                ColorSelectorDisplay.Enabled = true;
                RoomSelector.Enabled = true;
            }
            else
            {
                ColorSelector.Enabled = false;
                ColorSelectorDisplay.Enabled = false;
                RoomSelector.Enabled = false;
            }

            if (settings.ContainsKey("NotificationStyle"))
            {
                notifications.SelectedStyle = (NotificationType)settings["NotificationStyle"];
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show("Are you sure you want to reset all settings?" +
                "\n\n" +
                "This action cannot be undone.", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

            if (result == DialogResult.Yes)
            {
                RegOps.ResetSettings(ref settings);
            }
        }

        #region Client Settings

        private void toggleLog_Click(object sender, EventArgs e)
        {
            if (toggleLog.Tag != null)
            {
                if (toggleLog.Tag.ToString() == "true")
                {
                    parent.SetLogVisibility(false);
                    toggleLog.Tag = "false";
                    toggleLog.Text = "Show Log";
                    RegOps.WriteSetting("ShowLog", 0, RegistryValueKind.DWord, ref settings);
                }
                else
                {
                    parent.SetLogVisibility(true);
                    toggleLog.Tag = "true";
                    toggleLog.Text = "Hide Log";
                    RegOps.WriteSetting("ShowLog", 1, RegistryValueKind.DWord, ref settings);
                }
            }
            else
            {
                parent.SetLogVisibility(true);
                toggleLog.Tag = "true";
                toggleLog.Text = "Hide Log";
                RegOps.WriteSetting("ShowLog", 1, RegistryValueKind.DWord, ref settings);
            }
        }

        #endregion

        #region Chat Settings

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            
        }

        private void ColorSelector_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (ColorSelector.SelectedItem != null)
            {
                NColor color = parent.ColorToNColor((Color)ColorSelector.SelectedItem);
                parent.ChangeTagColor(color.R, color.G, color.B);
                ColorSelectorDisplay.Text = parent.GetFormattedTagColor();
            }
        }

        private void ColorSelectorDisplay_Click(object sender, EventArgs e)
        {
            DialogResult result = colorDialog1.ShowDialog();
            if (result == DialogResult.OK)
            {
                var color = parent.ChangeTagColor(colorDialog1.Color.R, colorDialog1.Color.G, colorDialog1.Color.B);

                foreach (Color c in ColorSelector.Items)
                {
                    Console.WriteLine(c.ToKnownColor().ToString(), colorDialog1.Color.ToKnownColor().ToString());
                    if (c.ToKnownColor() == colorDialog1.Color.ToKnownColor())
                    {
                        ColorSelector.SelectedItem = c;
                        break;
                    }
                    ColorSelector.SelectedIndex = -1;
                }

                ColorSelectorDisplay.Text = color;
            }
        }

        private void RoomSelector_SelectedIndexChanged(object sender, EventArgs e)
        {
            parent.ChangeRoom(RoomSelector.Text);
        }

        #endregion

        #region Notification Buttons

        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            RegOps.WriteSetting("NotificationStyle", "Both", RegistryValueKind.String, ref settings);
        }

        private void radioButton4_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void radioButton2_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void radioButton3_CheckedChanged(object sender, EventArgs e)
        {

        }

        #endregion

        #region Sound Buttons

        private void radioButton5_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void radioButton6_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void button2_Click(object sender, EventArgs e)
        {

        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {

        }

        #endregion
    }
}
