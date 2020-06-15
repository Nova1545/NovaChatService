using System;
using System.Windows.Forms;

namespace Client
{
    public partial class Help : Form
    {
        public Help()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void CommandList_SelectedIndexChanged(object sender, EventArgs e)
        {

        }
    }
}
