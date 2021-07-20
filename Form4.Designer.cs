
namespace MW5_Mod_Manager
{
    partial class Form4
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.button1 = new System.Windows.Forms.Button();
            this.textProgressBar1 = new MW5_Mod_Manager.TextProgressBar();
            this.SuspendLayout();
            // 
            // textBox1
            // 
            this.textBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.textBox1.BackColor = System.Drawing.SystemColors.ButtonFace;
            this.textBox1.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.textBox1.Location = new System.Drawing.Point(12, 12);
            this.textBox1.Multiline = true;
            this.textBox1.Name = "textBox1";
            this.textBox1.ReadOnly = true;
            this.textBox1.Size = new System.Drawing.Size(303, 76);
            this.textBox1.TabIndex = 0;
            this.textBox1.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(131, 146);
            this.button1.MaximumSize = new System.Drawing.Size(60, 25);
            this.button1.MinimumSize = new System.Drawing.Size(60, 25);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(60, 25);
            this.button1.TabIndex = 2;
            this.button1.Text = "Cancel";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // textProgressBar1
            // 
            this.textProgressBar1.CustomText = "";
            this.textProgressBar1.Enabled = false;
            this.textProgressBar1.Location = new System.Drawing.Point(12, 117);
            this.textProgressBar1.Name = "textProgressBar1";
            this.textProgressBar1.ProgressColor = System.Drawing.Color.LightGreen;
            this.textProgressBar1.Size = new System.Drawing.Size(303, 23);
            this.textProgressBar1.TabIndex = 1;
            this.textProgressBar1.TextColor = System.Drawing.Color.Black;
            this.textProgressBar1.TextFont = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textProgressBar1.VisualMode = MW5_Mod_Manager.ProgressBarDisplayMode.Percentage;
            this.textProgressBar1.Click += new System.EventHandler(this.progressBar1_Click);
            // 
            // Form4
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.ClientSize = new System.Drawing.Size(327, 183);
            this.ControlBox = false;
            this.Controls.Add(this.button1);
            this.Controls.Add(this.textProgressBar1);
            this.Controls.Add(this.textBox1);
            this.ImeMode = System.Windows.Forms.ImeMode.Disable;
            this.MaximumSize = new System.Drawing.Size(343, 222);
            this.MinimumSize = new System.Drawing.Size(343, 222);
            this.Name = "Form4";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Form4";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        public System.Windows.Forms.TextBox textBox1;
        public TextProgressBar textProgressBar1;
        public System.Windows.Forms.ProgressBar progressBar1;
        public System.Windows.Forms.Button button1;
    }
}