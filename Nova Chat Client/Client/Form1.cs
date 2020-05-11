using ChatLib.DataStates;
using ChatLib.Extras;
using ChatLib;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Windows.Forms;

namespace Client
{
	public partial class Tcp_Client : Form
	{
		TcpClient tcpClient;
		About aboutBox = new About();
		Settings settings = new Settings();
        User user;
		Random rnd = new Random();
		Color color;

		public Tcp_Client()
		{
			InitializeComponent();
			print("Welcome to the Nova Chat Client. Please enter an IP address above and click 'Connect' to begin.", Chat);
			print("Press 'Delete' when focused in this box to clear it.", Chat);
			color = Color.FromArgb(rnd.Next(256), rnd.Next(256), rnd.Next(256));
		}

		private void SendMessage()
		{
			try
			{
				if (chatBox.Text.StartsWith("/msg"))
				{
					string[] text = chatBox.Text.Split('"', '"');
					try
					{
                        user.CreateWisper(text[3], color, text[1]);
						print(nameBox.Text + ": " + "Message privately sent to " + text[1], Chat, color);
					}
					catch
					{
						print("Couldnt run command", Chat, Color.Red);
					}
				}
				else
				{
					print(nameBox.Text + ": " + chatBox.Text, Chat, color);
                    user.CreateMessage(chatBox.Text, color);
				}
			}
			catch (Exception ex)
			{
				print("Error Sending Message -> " + ex.Message, Log, Color.Red);
			}
			chatBox.Clear();
		}

		private void chatBox_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Enter)
			{
				SendMessage();
			}
		}

		private void sendButton_Click(object sender, EventArgs e)
		{
			SendMessage();
		}

		private void Connect()
		{
			if (IPBox.Text == "IP Address" || IPBox.Text.Length < 1)
			{
				IPBox.Text = "novastudios.tk";
			}

			Thread t = new Thread(delegate ()
			{
				try
				{
					if (tcpClient != null)
					{
						if (tcpClient.Connected)
						{
                            user.CreateStatus(StatusType.Disconnecting);
							ChangeConnectionInputState(true);
							return;
						}
					}
					
					print("Connecting... ", Log);
					tcpClient = new TcpClient(IPBox.Text, 8910);

                    // Send name
                    user = new User(nameBox.Text, tcpClient.GetStream());
                    user.Init();

                    // Setup Callbacks
                    user.OnMessageReceivedCallback += User_OnMessageReceivedCallback;
                    user.OnMessageStatusReceivedCallback += User_OnMessageStatusReceivedCallback;
                    user.OnMessageTransferReceivedCallback += User_OnMessageTransferReceivedCallback;
                    user.OnMessageWisperReceivedCallback += User_OnMessageWisperReceivedCallback;
                    user.OnErrorCallback += (e) => { print(e.Message, Log); };

					ChangeConnectionInputState(false);
					print("Successfully connected to " + IPBox.Text, Log, Color.LimeGreen);
				}
				catch (Exception ex)
				{
					print("Connection failed -> " + ex.Message, Log, Color.Red);
				}
			});

			t.IsBackground = true;
			t.Start();
		}

        private void User_OnMessageWisperReceivedCallback(ChatLib.Message message)
        {
            print("Private Message From " + message.Name + ": " + message.Content, Chat, message.Color);
        }

        private void User_OnMessageTransferReceivedCallback(ChatLib.Message message)
        {
            File.WriteAllBytes(message.Filename, message.FileContents);
            print(message.Name + ": file://" + new FileInfo(message.Filename).FullName.Replace(@"\", "/"), Chat, message.Color);
        }

        private void User_OnMessageStatusReceivedCallback(ChatLib.Message message)
        {
            if (message.StatusType == StatusType.Connected) {
                print(message.Name + " Connected", Log);
            }
            else if (message.StatusType == StatusType.Disconnected)
            {
                print(message.Name + " Disconnected", Log);
            }
            else if (message.StatusType == StatusType.Disconnecting)
            {
                user.Close();
                tcpClient.Close();
            }
            else
            {
                print(message.Name + " Disconnected With Error", Log);
            }
        }

        private void User_OnMessageReceivedCallback(ChatLib.Message message)
        {
            print(message.Name + ": " + message.Content, Chat, message.Color);
        }

        #region Stuff I Dont Care About


        #region SetttingsHandlers
        private void LoadSettings()
		{
			RegistryKey key = Registry.CurrentUser.OpenSubKey("Software\\NovaStudios\\NovaChatClient\\Settings", true);

			if (key == null)
			{
				key = Registry.CurrentUser.CreateSubKey("Software\\NovaStudios\\NovaChatClient\\Settings", true);
			}
			else
			{
				try
				{
					toggleLogVisibility(bool.Parse(key.GetValue("ShowLog").ToString()));
				}
				catch (Exception ex)
				{
					(Application.OpenForms["Tcp_Client"] as Tcp_Client).printToLog("Something went wrong while reading settings! Please report this error to the creator with the following: " + ex.Message, Color.Red);
				}
			}

			key.Dispose();
		}

		public void toggleLogVisibility(bool show)
		{
			if (!show)
			{
				Log.Visible = false;
				label2.Visible = false;
				tableLayoutPanel4.ColumnCount = 1;
				settings.toggleLog.Tag = "false";
			}
			else
			{
				Log.Visible = true;
				label2.Visible = true;
				tableLayoutPanel4.ColumnCount = 2;
				settings.toggleLog.Tag = "true";
			}
	}
		#endregion

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
			Log.AppendText(text + "\n", color);
			Log.ScrollToCaret();
		}

		public void printToLog(string text, Color color)
		{
			Log.AppendText(text + "\n", color);
			Log.ScrollToCaret();
		}

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

		#region EventHandlers
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
			settings.ShowDialog();
		}

		private void IPBox_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Enter)
			{
				Connect();
			}
		}

		private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
		{
			aboutBox.ShowDialog();
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
            if (user != null)
            {
                user.Close();
            }
		}

		private void openFileDialog1_FileOk(object sender, System.ComponentModel.CancelEventArgs e)
		{
			try
			{
                byte[] bytes = File.ReadAllBytes(openFileDialog1.FileName);
                FileInfo info = new FileInfo(openFileDialog1.FileName);
                user.CreateTransfer(bytes, info.Name, color);
                print("File Sent!", Chat, Color.Green);
            }
			catch (Exception ex)
			{
				print("Error Sending File ->" + ex.Message, Log, Color.Red);
			}
		}

		private void button1_Click_1(object sender, EventArgs e)
		{
			openFileDialog1.ShowDialog();
		}

        private void Tcp_Client_Load(object sender, EventArgs e)
        {
            LoadSettings();
        }
    }
    #endregion

    public static class RichTextBoxExtensions
	{
		public static void AppendText(this RichTextBox box, string text, Color color)
		{
			box.SelectionStart = box.TextLength;
			box.SelectionLength = 0;

			box.SelectionColor = color;
			box.AppendText(text);
			box.SelectionColor = box.ForeColor;
		}
	}
    #endregion
}
