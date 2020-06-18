using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Client
{
    public partial class TextInput : Form
    {
        public TextInput()
        {
            InitializeComponent();
            AcceptButton = button1;
            CancelButton = button2;
        }

        public string Password => textBox1.Text;
    }
}
