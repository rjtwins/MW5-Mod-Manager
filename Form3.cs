using System;
using System.Windows.Forms;

namespace MW5_Mod_Manager
{
    public partial class Form3 : Form
    {
        public Form3()
        {
            InitializeComponent();
        }

        //Copy txt to clipboard
        private void button1_Click(object sender, EventArgs e)
        {
            Clipboard.SetText(this.textBox1.Text);
        }
    }
}
