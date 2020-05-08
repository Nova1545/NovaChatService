using ChatLib.DataStates;
using ChatLib.Extras;
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
		NetworkStream stream;
		About aboutBox = new About();
		Settings settings = new Settings();
		Random rnd = new Random();
		Color color;

		public Tcp_Client()
		{
			InitializeComponent();
			print("Welcome to the Nova Chat Client. Please enter an IP address above and click 'Connect' to begin.", Chat);
			print("Press 'Delete' when focused in this box to clear it.", Chat);
			color = Color.FromArgb(rnd.Next(256), rnd.Next(256), rnd.Next(256));
		}

		private void Listen(NetworkStream stream)
		{
			try
			{
				while (true)
				{
					ChatLib.Message m = Helpers.GetMessage(stream);
					if (m.MessageType == MessageType.Message)
					{
						print(m.Name + ": " + m.Content, Chat, m.Color, true);
					}
                    else if(m.MessageType == MessageType.Status && m.Content == "disconnect")
                    {
                        tcpClient.Close();
                        break;
                    }
                    else
					{
						print(m.Name + " " + m.Content, Log, Color.Orange, true);
					}
					stream.Flush();
				}
			}
			catch (Exception e)
			{
				print("Disconnected from server.", Log, Color.Red, true);
				ChangeConnectionInputState(true);
			}
		}

		private void SendMessage()
		{
			try
			{
				string name = nameBox.Text;

				if (chatBox.Text.StartsWith("/msg"))
				{
					string[] text = chatBox.Text.Split('"', '"');
					try
					{
						Helpers.SetMessage(stream, new ChatLib.Message(name, text[3], MessageType.Message, color, text[1]));
						print(name + ": " + "Message privately sent to " + text[1], Chat, Color.Green);
					}
					catch
					{
						print("Couldnt run command", Chat, Color.Red);
					}
				}
				else
				{
					print(name + ": " + chatBox.Text, Chat, color);
					Helpers.SetMessage(stream, new ChatLib.Message(name, chatBox.Text, MessageType.Message, color));
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
                            Helpers.SetMessage(stream, new ChatLib.Message(nameBox.Text, "disconnect", MessageType.Status));
                            //tcpClient.Close();
							ChangeConnectionInputState(true);
							return;
						}
					}
					
					print("Connecting... ", Log, true);
					tcpClient = new TcpClient(IPBox.Text, 8910);
					stream = tcpClient.GetStream();

					// Send name
					Helpers.SetMessage(stream, new ChatLib.Message(nameBox.Text, "name", MessageType.Info));

					ChangeConnectionInputState(false);
					print("Successfully connected to " + IPBox.Text, Log, Color.LimeGreen, true);
					this.Listen(stream);
				}
				catch (Exception ex)
				{
					print("Connection failed -> " + ex.Message, Log, Color.Red, true);
				}
			});

			t.IsBackground = true;
			t.Start();
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

		public void print(string text, RichTextBox output, bool invoke = false)
		{
			if (invoke)
			{
				output.Invoke(new MethodInvoker(() => output.AppendText(text + "\n")));
				output.Invoke(new MethodInvoker(() => output.ScrollToCaret()));
				return;
			}

			output.AppendText(text + "\n");
			output.ScrollToCaret();
		}

		public void print(string text, RichTextBox output, Color color, bool invoke=false)
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
			File.WriteAllText("log.txt", DateTime.Now.ToString() + "\n" + Log.Text);
		}

		private void openFileDialog1_FileOk(object sender, System.ComponentModel.CancelEventArgs e)
		{
			try
			{
                byte[] bytes = File.ReadAllBytes(openFileDialog1.FileName);
                FileInfo info = new FileInfo(openFileDialog1.FileName);
                Helpers.SetMessage(stream, new ChatLib.Message(nameBox.Text, bytes, MessageType.Transfer));
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
