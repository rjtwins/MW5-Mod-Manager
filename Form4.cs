using System;
using System.ComponentModel;
using System.Windows.Forms;

namespace MW5_Mod_Manager
{

    public partial class Form4 : Form
    {
        private BackgroundWorker worker1, worker2;

        public Form4(BackgroundWorker worker1, BackgroundWorker worker2)
        {
            InitializeComponent();
            this.worker1 = worker1;
            this.worker2 = worker2;
        }

        private void progressBar1_Click(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.worker1.CancelAsync();
            this.worker2.CancelAsync();
        }
    }
}
