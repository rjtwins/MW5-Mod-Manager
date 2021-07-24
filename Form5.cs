using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MW5_Mod_Manager
{
    public partial class Form5 : Form
    {
        Form1 MainForm;
        //TCPFileShare fileShare;

        public Form5(Form1 MainForm)
        {
            InitializeComponent();
            this.MainForm = MainForm;
            //this.fileShare = MainForm.fileShare;
            this.textBox1.Text = "127.0.0.1";
        }

        private void button1_Click(object sender, EventArgs e)
        {
            //enable the cancel button, disable the done button.
            button3.Enabled = true;
            button2.Enabled = false;
            this.progressBar1.Value = 0;
            this.label1.Text = "";
            //fileShare.prepareSentTCP(MainForm, this);
        }

        public void SetLabel1Text(string txt)
        {
            this.label1.Text = txt;
        }

        public string GetTxtBoxTxt()
        {
            return this.textBox1.Text;
        }

        public void SetTxtBoxTxt(string txt)
        {
            this.textBox1.Text = txt;
        }

        public void SetProgressBarPercentage(int value)
        {
            this.progressBar1.Value = value; 
        }

        private void button3_Click(object sender, EventArgs e)
        {
            //this.fileShare.CancelUpload();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.Close();
            this.Dispose();
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void progressBar1_Click(object sender, EventArgs e)
        {

        }
    }
}
