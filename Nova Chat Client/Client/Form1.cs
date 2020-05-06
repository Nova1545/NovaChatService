using ChatLib.DataStates;
using ChatLib.Extras;
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
                    ChatLib.Message message = Helpers.GetMessage(stream);
                    if (message.MessageType == MessageType.Message)
                    {
                        print_Invoke(message.Name + ": " + message.Content, Chat, message.Color);
                    }
                    else if(message.MessageType == MessageType.Transfer)
                    {
                        string filename = Path.GetRandomFileName() + (message.FileType == FileType.PNG ? ".png" : ".jpg");
                        File.WriteAllBytes(filename, Helpers.GetFileStream(stream));
                        print_Invoke(message.Name + " Sent : " + "file://" + (new FileInfo(Application.ExecutablePath).DirectoryName + @"\" + filename).Replace(@"\", "/"), Chat, color);
                    }
                    else
                    {
                        print_Invoke(message.Name + " " + message.Content, Log, Color.Red);
                    }
					stream.Flush();
				}
			}
			catch (Exception e)
			{
				print_Invoke("The connection was lost.", Log, Color.Red);
                File.WriteAllText("crash.txt", e.Message);
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
                else if (chatBox.Text.StartsWith("/send"))
                {
                    Helpers.SetMessage(stream, new ChatLib.Message(name, MessageType.Transfer, FileType.PNG));
                    Helpers.SetFileStream(stream, File.ReadAllBytes("input.png"));
                    print("Sent File!", Chat, Color.Green);
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
					print_Invoke("Connecting... ", Log);
					tcpClient = new TcpClient(IPBox.Text, 8910);
					stream = tcpClient.GetStream();

                    // Send name
                    Helpers.SetMessage(stream, new ChatLib.Message(nameBox.Text, "name", MessageType.Info));

                    print_Invoke("Successfully connected to " + IPBox.Text, Log, Color.LimeGreen);
					this.Listen(stream);
				}
				catch (Exception ex)
				{
					print_Invoke("Connection attmept failed -> " + ex.Message, Log, Color.Red);
				}
			});

			t.IsBackground = true;
			t.Start();
		}

		private void button1_Click(object sender, EventArgs e)
		{
			Connect();
		}

        #region Stuff I Dont Care About

        /// <summary>
        /// Prints the given message to the given RichTextBox
        /// </summary>
        /// <param name="text"></param>
        /// <param name="output"></param>
        private void print(string text, RichTextBox output)
		{
			output.AppendText(text + "\n");
			output.ScrollToCaret();
		}

		/// <summary>
		///  Prints the given message to the given RichTextBox in the specified color
		/// </summary>
		/// <param name="text"></param>
		/// <param name="output"></param>
		/// <param name="color"></param>
		private void print(string text, RichTextBox output, Color color)
		{
			output.AppendText(text + "\n", color);
			output.ScrollToCaret();
		}

		/// <summary>
		/// Prints the given message to the given RichTextBox (Cross Thread)
		/// </summary>
		/// <param name="text"></param>
		/// <param name="output"></param>
		private void print_Invoke(string text, RichTextBox output)
		{
			output.Invoke(new MethodInvoker(() => output.AppendText(text + "\n")));
			output.Invoke(new MethodInvoker(() => output.ScrollToCaret()));
		}

		/// <summary>
		/// Prints the given message to the given RichTextBox in the specified color (Cross Thread)
		/// </summary>
		/// <param name="text"></param>
		/// <param name="output"></param>
		/// <param name="color"></param>
		private void print_Invoke(string text, RichTextBox output, Color color)
		{
			output.Invoke(new MethodInvoker(() => output.AppendText(text + "\n", color)));
			output.Invoke(new MethodInvoker(() => output.ScrollToCaret()));
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
            File.WriteAllText("info.txt", new Uri(e.LinkText).AbsolutePath);
            Process.Start(new Uri(e.LinkText).AbsolutePath);
        }
    }

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
