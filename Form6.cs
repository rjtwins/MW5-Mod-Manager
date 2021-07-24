//using System;
//using System.Collections.Generic;
//using System.ComponentModel;
//using System.Data;
//using System.Drawing;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using System.Windows.Forms;

//namespace MW5_Mod_Manager
//{
//    public partial class Form6 : Form
//    {
//        TCPFileShare FileShare;
//        public Form6(TCPFileShare FileShare)
//        {
//            InitializeComponent();
//            this.FileShare = FileShare;
//        }

//        Cancel button
//        private void button3_Click(object sender, EventArgs e)
//        {
//            this.FileShare.StopDownloadOrUnpack = true;
//            this.button3.Enabled = false;
//        }

//        Done button
//        private void button2_Click(object sender, EventArgs e)
//        {
//            this.Close();
//        }

//        private void progressBar1_Click(object sender, EventArgs e)
//        {

//        }
//    }
//}
