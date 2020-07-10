using ChatLib.DataStates;
using ChatLib.Extras;
using System.Drawing;
using System.Windows.Forms;

namespace Client
{
    partial class Tcp_Client
    {
        private void LoadSettings()
        {
            RegOps.ReadSettings(ref settings);

            settings.PropertyChanged += Settings_PropertyChanged;

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

        private void FixClient()
        {
            try
            {
                tcpClient.Dispose();
                ChangeConnectionInputState(true);
                print("Client Fixed!", Log);
            }
            catch
            {

            }
        }

        public bool IsConnected()
        {
            if (tcpClient != null)
            {
                if (tcpClient.Connected)
                {
                    return true;
                }
            }
            return false;
        }

        public string GetFormattedTagColor()
        {
            return "(" + TagColor.R.ToString() + ", " + TagColor.G.ToString() + ", " + TagColor.B.ToString() + ")";
        }

        public bool IsFocused()
        {
            if (this.Focused)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

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

        public NColor GetTagColor()
        {
            return TagColor;
        }

        #region Print Methods

        public void print(string text, RichTextBox output)
        {
            if (output.InvokeRequired)
            {
                output.Invoke(new MethodInvoker(() => output.AppendText(text + "\n")));
                output.Invoke(new MethodInvoker(() => output.ScrollToCaret()));
                return;
            }

            output.AppendText(text + "\n");
            output.ScrollToCaret();
        }

        public void print(string text, RichTextBox output, Color color)
        {
            if (output.InvokeRequired)
            {
                output.Invoke(new MethodInvoker(() => output.AppendText(text + "\n", color)));
                output.Invoke(new MethodInvoker(() => output.ScrollToCaret()));
                return;
            }

            output.AppendText(text + "\n", color);
            output.ScrollToCaret();
        }

        public void printToLog(string text)
        {
            Log.AppendText(text + "\n", NColorToColor(TagColor));
            Log.ScrollToCaret();
        }

        public void PrintToLog(string text, Color color)
        {
            Log.AppendText(text + "\n", color);
            Log.ScrollToCaret();
        }

        #endregion

        #region UI Update Handlers

        private void ChangeConnectionInputState(bool state)
        {
            IPBox.Invoke(new MethodInvoker(() => IPBox.Enabled = state));
            nameBox.Invoke(new MethodInvoker(() => nameBox.Enabled = state));
            if (state)
            {
                connectButton.Invoke(new MethodInvoker(() => { connectButton.Text = "Connect"; }));
            }
            else
            {
                connectButton.Invoke(new MethodInvoker(() => { connectButton.Text = "Disconnect"; }));
            }
        }
        public void SetLogVisibility(bool show)
        {
            if (!show)
            {
                Log.Visible = false;
                label2.Visible = false;
                tableLayoutPanel4.ColumnCount = 1;
                SettingsWindow.toggleLog.Tag = "false";
            }
            else
            {
                Log.Visible = true;
                label2.Visible = true;
                tableLayoutPanel4.ColumnCount = 2;
                SettingsWindow.toggleLog.Tag = "true";
            }
        }

        #endregion

        #region Chat Methods

        public string ChangeTagColor(byte R, byte G, byte B)
        {
            if (R >= 0 && R <= 255 && R >= 0 && G <= 255 && R >= 0 && B <= 255)
            {
                this.TagColor = NColor.FromRGB(R, G, B);
                PrintToLog("Changing tag color to (" + R.ToString() + ", " + G.ToString() + ", " + B.ToString() + ")", Color.Teal);
                return "(" + R.ToString() + ", " + G.ToString() + ", " + B.ToString() + ")";
            }
            PrintToLog("Invalid Color", Color.Red);
            return "Invalid Color";
        }

        public void ChangeRoom(string room)
        {
            user.CreateStatus(StatusType.ChangeRoom, room);
        }

        #endregion
    }
}