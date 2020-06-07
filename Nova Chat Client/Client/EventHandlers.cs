using ChatLib.DataStates;
using ChatLib.Extras;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using Yep_Development_Tools;

namespace Client
{
    partial class Tcp_Client
    {
        #region Casts
        public Color NColorToColor(NColor color)
        {
            return Color.FromArgb(color.R, color.G, color.B);
        }

        public NColor ColorToNColor(Color color)
        {
            return NColor.FromRGB(color.R, color.G, color.B);
        }

        #endregion


        private void chatBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                SendMessage();
            }
        }

        private void sendButton_Click(object sender, EventArgs e)
        {
            SendMessage();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Connect();
        }

        private void nameBox_Leave(object sender, EventArgs e)
        {
            if (nameBox.Text.Length < 1)
            {
                nameBox.Text = "Enter a Name";
            }
        }

        private void nameBox_Enter(object sender, EventArgs e)
        {
            if (nameBox.Text == "Enter a Name")
            {
                nameBox.Text = "";
            }
        }

        private void ChatBox_Enter(object sender, EventArgs e)
        {
            if (chatBox.Text == "Message")
            {
                chatBox.Text = "";
            }
        }

        private void ChatBox_Leave(object sender, EventArgs e)
        {
            if (chatBox.Text.Length < 1)
            {
                chatBox.Text = "Message";
            }
        }

        private void IPBox_Enter(object sender, EventArgs e)
        {
            if (IPBox.Text == "IP Address")
            {
                IPBox.Text = "";
            }
        }

        private void IPBox_Leave(object sender, EventArgs e)
        {
            if (IPBox.Text.Length < 1)
            {
                IPBox.Text = "IP Address";
            }
        }

        private void Log_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                Log.Clear();
            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void settingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SettingsWindow.ShowDialog();
        }

        private void IPBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                Connect();
            }
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void Chat_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                Chat.Clear();
            }
        }

        private void clearHistoryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Chat.Clear();
            Log.Clear();
        }

        private void Chat_LinkClicked(object sender, LinkClickedEventArgs e)
        {
            Process.Start(new Uri(e.LinkText).AbsolutePath.Replace("/", @"\").Replace("%20", " "));
        }

        private void Tcp_Client_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (tcpClient != null)
            {
                if (tcpClient.Connected && user != null)
                {
                    user.CreateStatus(StatusType.Disconnecting);
                    ChangeConnectionInputState(true);
                }
            }
            if (user != null)
            {
                user.Close();
            }
        }

        private void openFileDialog1_FileOk(object sender, System.ComponentModel.CancelEventArgs e)
        {
            new Thread(() =>
            {
                try
                {
                    FileInfo info = new FileInfo(openFileDialog1.FileName);
                    if (info.Length > 10485760)
                    {
                        MessageBox.Show("File larger than 10 megabytes", "Upload Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                    byte[] bytes = File.ReadAllBytes(openFileDialog1.FileName);
                    user.CreateTransfer(bytes, info.Name, TagColor);
                    print("File Sent!", Chat, Color.Green);
                }
                catch (Exception ex)
                {
                    print("Error Sending File ->" + ex.Message, Log, Color.Red);
                }
            }).Start();
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            openFileDialog1.ShowDialog();
        }

        private void Tcp_Client_Load(object sender, EventArgs e)
        {
            if (debug)
            {
                debugToolStripMenuItem.Visible = true;
            }

            LoadSettings();
        }

        private void LoadSettings()
        {
            RegOps.ReadSettings(ref settings);

            this.settings.CollectionChanged += Settings_CollectionChanged;

            if (settings.ContainsKey("ShowLog"))
            {
                if ((bool)settings["ShowLog"])
                {
                    SetLogVisibility(true);
                }
                else
                {
                    SetLogVisibility(false);
                }
            }

            if (settings.ContainsKey("ServerPath"))
            {
                ServerPath = settings["ServerPath"].ToString();
            }
        }

        private void aboutToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            AboutBox.ShowDialog();
        }

        private void fixClientToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FixClient();
        }

        private void commandsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            HelpWindow.ShowDialog();
        }

        private void startAnotherInstanceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ProcessUtils.StartProcess(Assembly.GetExecutingAssembly().Location, "/debug");
        }

        private void startServerInstanceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (File.Exists(ServerPath))
            {
                ProcessUtils.StartProcess(ServerPath, startin: Directory.GetParent(ServerPath).FullName);
            }
            else
            {
                MessageBox.Show("Unable to locate server binary. Please set the file location.", "File Not Found", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void killAllInstancesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ProcessUtils.KillAll(Process.GetCurrentProcess().ProcessName);
        }

        private void Settings_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Replace)
            {
                notifications.SetNotificationStyle((NotificationManager.NotificationType)RegOps.GetSettingFromDict("NotificationStyle", settings));
            }
        }

        private void setPathToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.InitialDirectory = Directory.GetParent(Assembly.GetExecutingAssembly().Location).FullName;
                DialogResult dialogResult = dialog.ShowDialog();

                if (dialogResult == DialogResult.OK)
                {
                    ServerPath = dialog.FileName;
                    RegOps.WriteSetting("ServerPath", dialog.FileName, RegistryValueKind.String, ref settings);
                }
            }
        }
    }
}
