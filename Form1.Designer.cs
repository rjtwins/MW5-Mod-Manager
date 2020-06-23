namespace MW5_Mod_Manager
{
    partial class Form1
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            this.button1 = new System.Windows.Forms.Button();
            this.button2 = new System.Windows.Forms.Button();
            this.button3 = new System.Windows.Forms.Button();
            this.backgroundWorker1 = new System.ComponentModel.BackgroundWorker();
            this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.button5 = new System.Windows.Forms.Button();
            this.button6 = new System.Windows.Forms.Button();
            this.button8 = new System.Windows.Forms.Button();
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.toolStripDropDownButton1 = new System.Windows.Forms.ToolStripDropDownButton();
            this.installDirectoryToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.selectToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.searcgToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.changeVendorToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.windowsStoreToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.epicStoreToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.exportLoadOrderToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.importLoadOrderToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
            this.toolStripLabel1 = new System.Windows.Forms.ToolStripLabel();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.toolStripButton1 = new System.Windows.Forms.ToolStripButton();
            this.listView1 = new System.Windows.Forms.ListView();
            this.enabled = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.display = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.folder = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.author = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.version = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.button4 = new System.Windows.Forms.Button();
            this.rotatingLabel1 = new MW5_Mod_Manager.RotatingLabel();
            this.toolStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(13, 130);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(128, 38);
            this.button1.TabIndex = 1;
            this.button1.Text = "UP";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(13, 174);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(128, 38);
            this.button2.TabIndex = 2;
            this.button2.Text = "Down";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.button2_Click);
            // 
            // button3
            // 
            this.button3.Location = new System.Drawing.Point(13, 218);
            this.button3.Name = "button3";
            this.button3.Size = new System.Drawing.Size(128, 38);
            this.button3.TabIndex = 3;
            this.button3.Text = "Apply";
            this.button3.UseVisualStyleBackColor = true;
            this.button3.Click += new System.EventHandler(this.button3_Click);
            // 
            // backgroundWorker1
            // 
            this.backgroundWorker1.DoWork += new System.ComponentModel.DoWorkEventHandler(this.backgroundWorker1_DoWork);
            // 
            // textBox1
            // 
            this.textBox1.Location = new System.Drawing.Point(189, 56);
            this.textBox1.Name = "textBox1";
            this.textBox1.Size = new System.Drawing.Size(506, 20);
            this.textBox1.TabIndex = 5;
            // 
            // button5
            // 
            this.button5.Location = new System.Drawing.Point(701, 46);
            this.button5.Name = "button5";
            this.button5.Size = new System.Drawing.Size(128, 38);
            this.button5.TabIndex = 6;
            this.button5.Text = "Stop Search";
            this.button5.UseVisualStyleBackColor = true;
            this.button5.Click += new System.EventHandler(this.button5_Click);
            // 
            // button6
            // 
            this.button6.Location = new System.Drawing.Point(13, 46);
            this.button6.Name = "button6";
            this.button6.Size = new System.Drawing.Size(128, 38);
            this.button6.TabIndex = 7;
            this.button6.Text = "Refresh";
            this.button6.UseVisualStyleBackColor = true;
            this.button6.Click += new System.EventHandler(this.button6_Click);
            // 
            // button8
            // 
            this.button8.Image = ((System.Drawing.Image)(resources.GetObject("button8.Image")));
            this.button8.Location = new System.Drawing.Point(12, 380);
            this.button8.Margin = new System.Windows.Forms.Padding(0);
            this.button8.Name = "button8";
            this.button8.Size = new System.Drawing.Size(128, 128);
            this.button8.TabIndex = 0;
            this.button8.UseVisualStyleBackColor = true;
            this.button8.Click += new System.EventHandler(this.button8_Click);
            // 
            // toolStrip1
            // 
            this.toolStrip1.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripDropDownButton1,
            this.toolStripSeparator3,
            this.toolStripLabel1,
            this.toolStripSeparator2,
            this.toolStripButton1});
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(841, 25);
            this.toolStrip1.TabIndex = 10;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // toolStripDropDownButton1
            // 
            this.toolStripDropDownButton1.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.toolStripDropDownButton1.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.installDirectoryToolStripMenuItem,
            this.changeVendorToolStripMenuItem,
            this.toolStripSeparator1,
            this.exportLoadOrderToolStripMenuItem,
            this.importLoadOrderToolStripMenuItem});
            this.toolStripDropDownButton1.Image = ((System.Drawing.Image)(resources.GetObject("toolStripDropDownButton1.Image")));
            this.toolStripDropDownButton1.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripDropDownButton1.Name = "toolStripDropDownButton1";
            this.toolStripDropDownButton1.Size = new System.Drawing.Size(38, 22);
            this.toolStripDropDownButton1.Text = "File";
            // 
            // installDirectoryToolStripMenuItem
            // 
            this.installDirectoryToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.selectToolStripMenuItem,
            this.searcgToolStripMenuItem});
            this.installDirectoryToolStripMenuItem.Name = "installDirectoryToolStripMenuItem";
            this.installDirectoryToolStripMenuItem.Size = new System.Drawing.Size(172, 22);
            this.installDirectoryToolStripMenuItem.Text = "Install Directory";
            // 
            // selectToolStripMenuItem
            // 
            this.selectToolStripMenuItem.Name = "selectToolStripMenuItem";
            this.selectToolStripMenuItem.Size = new System.Drawing.Size(109, 22);
            this.selectToolStripMenuItem.Text = "Select";
            this.selectToolStripMenuItem.Click += new System.EventHandler(this.selectToolStripMenuItem_Click);
            // 
            // searcgToolStripMenuItem
            // 
            this.searcgToolStripMenuItem.Name = "searcgToolStripMenuItem";
            this.searcgToolStripMenuItem.Size = new System.Drawing.Size(109, 22);
            this.searcgToolStripMenuItem.Text = "Search";
            this.searcgToolStripMenuItem.Click += new System.EventHandler(this.searcgToolStripMenuItem_Click);
            // 
            // changeVendorToolStripMenuItem
            // 
            this.changeVendorToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.windowsStoreToolStripMenuItem,
            this.epicStoreToolStripMenuItem});
            this.changeVendorToolStripMenuItem.Name = "changeVendorToolStripMenuItem";
            this.changeVendorToolStripMenuItem.Size = new System.Drawing.Size(172, 22);
            this.changeVendorToolStripMenuItem.Text = "Set Vendor";
            // 
            // windowsStoreToolStripMenuItem
            // 
            this.windowsStoreToolStripMenuItem.Name = "windowsStoreToolStripMenuItem";
            this.windowsStoreToolStripMenuItem.Size = new System.Drawing.Size(153, 22);
            this.windowsStoreToolStripMenuItem.Text = "Windows Store";
            this.windowsStoreToolStripMenuItem.Click += new System.EventHandler(this.windowsStoreToolStripMenuItem_Click);
            // 
            // epicStoreToolStripMenuItem
            // 
            this.epicStoreToolStripMenuItem.Name = "epicStoreToolStripMenuItem";
            this.epicStoreToolStripMenuItem.Size = new System.Drawing.Size(153, 22);
            this.epicStoreToolStripMenuItem.Text = "Epic Store";
            this.epicStoreToolStripMenuItem.Click += new System.EventHandler(this.epicStoreToolStripMenuItem_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(169, 6);
            // 
            // exportLoadOrderToolStripMenuItem
            // 
            this.exportLoadOrderToolStripMenuItem.Name = "exportLoadOrderToolStripMenuItem";
            this.exportLoadOrderToolStripMenuItem.Size = new System.Drawing.Size(172, 22);
            this.exportLoadOrderToolStripMenuItem.Text = "Export Load Order";
            this.exportLoadOrderToolStripMenuItem.Click += new System.EventHandler(this.exportLoadOrderToolStripMenuItem_Click);
            // 
            // importLoadOrderToolStripMenuItem
            // 
            this.importLoadOrderToolStripMenuItem.Name = "importLoadOrderToolStripMenuItem";
            this.importLoadOrderToolStripMenuItem.Size = new System.Drawing.Size(172, 22);
            this.importLoadOrderToolStripMenuItem.Text = "Import Load Order";
            this.importLoadOrderToolStripMenuItem.Click += new System.EventHandler(this.importLoadOrderToolStripMenuItem_Click);
            // 
            // toolStripSeparator3
            // 
            this.toolStripSeparator3.Name = "toolStripSeparator3";
            this.toolStripSeparator3.Size = new System.Drawing.Size(6, 25);
            // 
            // toolStripLabel1
            // 
            this.toolStripLabel1.Name = "toolStripLabel1";
            this.toolStripLabel1.Size = new System.Drawing.Size(67, 22);
            this.toolStripLabel1.Text = "------------";
            // 
            // toolStripSeparator2
            // 
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            this.toolStripSeparator2.Size = new System.Drawing.Size(6, 25);
            // 
            // toolStripButton1
            // 
            this.toolStripButton1.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.toolStripButton1.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButton1.Image")));
            this.toolStripButton1.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButton1.Name = "toolStripButton1";
            this.toolStripButton1.Size = new System.Drawing.Size(109, 22);
            this.toolStripButton1.Text = "Open Mods Folder";
            this.toolStripButton1.Click += new System.EventHandler(this.toolStripButton1_Click);
            // 
            // listView1
            // 
            this.listView1.CheckBoxes = true;
            this.listView1.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.enabled,
            this.display,
            this.folder,
            this.author,
            this.version});
            this.listView1.FullRowSelect = true;
            this.listView1.GridLines = true;
            this.listView1.HideSelection = false;
            this.listView1.Location = new System.Drawing.Point(189, 90);
            this.listView1.MultiSelect = false;
            this.listView1.Name = "listView1";
            this.listView1.Size = new System.Drawing.Size(640, 407);
            this.listView1.TabIndex = 11;
            this.listView1.UseCompatibleStateImageBehavior = false;
            this.listView1.View = System.Windows.Forms.View.Details;
            // 
            // enabled
            // 
            this.enabled.Tag = "enabled";
            this.enabled.Text = "X";
            this.enabled.Width = 20;
            // 
            // display
            // 
            this.display.Tag = "display";
            this.display.Text = "Display Name";
            this.display.Width = 200;
            // 
            // folder
            // 
            this.folder.Tag = "folder";
            this.folder.Text = "Mod Folder";
            this.folder.Width = 200;
            // 
            // author
            // 
            this.author.Tag = "author";
            this.author.Text = "Author";
            this.author.Width = 100;
            // 
            // version
            // 
            this.version.Tag = "version";
            this.version.Text = "Version";
            // 
            // button4
            // 
            this.button4.Image = ((System.Drawing.Image)(resources.GetObject("button4.Image")));
            this.button4.Location = new System.Drawing.Point(31, 279);
            this.button4.Margin = new System.Windows.Forms.Padding(0);
            this.button4.Name = "button4";
            this.button4.Size = new System.Drawing.Size(90, 90);
            this.button4.TabIndex = 13;
            this.button4.UseVisualStyleBackColor = true;
            this.button4.Click += new System.EventHandler(this.button4_Click);
            // 
            // rotatingLabel1
            // 
            this.rotatingLabel1.AutoSize = true;
            this.rotatingLabel1.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.rotatingLabel1.Location = new System.Drawing.Point(161, 174);
            this.rotatingLabel1.Name = "rotatingLabel1";
            this.rotatingLabel1.NewText = "";
            this.rotatingLabel1.RotateAngle = 0;
            this.rotatingLabel1.Size = new System.Drawing.Size(18, 17);
            this.rotatingLabel1.TabIndex = 12;
            this.rotatingLabel1.Text = "X";
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(841, 520);
            this.Controls.Add(this.button4);
            this.Controls.Add(this.rotatingLabel1);
            this.Controls.Add(this.listView1);
            this.Controls.Add(this.toolStrip1);
            this.Controls.Add(this.button8);
            this.Controls.Add(this.button6);
            this.Controls.Add(this.button5);
            this.Controls.Add(this.textBox1);
            this.Controls.Add(this.button3);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.button1);
            this.HelpButton = true;
            this.MaximizeBox = false;
            this.MaximumSize = new System.Drawing.Size(857, 559);
            this.MinimumSize = new System.Drawing.Size(857, 559);
            this.Name = "Form1";
            this.ShowIcon = false;
            this.Text = "MW5 LoadOrderManager";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.Button button3;
        private System.ComponentModel.BackgroundWorker backgroundWorker1;
        private System.Windows.Forms.OpenFileDialog openFileDialog1;
        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.Button button5;
        private System.Windows.Forms.Button button6;
        private System.Windows.Forms.Button button8;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripDropDownButton toolStripDropDownButton1;
        private System.Windows.Forms.ToolStripMenuItem exportLoadOrderToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem importLoadOrderToolStripMenuItem;
        private System.Windows.Forms.ListView listView1;
        public System.Windows.Forms.ColumnHeader display;
        public System.Windows.Forms.ColumnHeader folder;
        public System.Windows.Forms.ColumnHeader author;
        private System.Windows.Forms.ColumnHeader enabled;
        private System.Windows.Forms.ColumnHeader version;
        private System.Windows.Forms.ToolStripMenuItem changeVendorToolStripMenuItem;
        public System.Windows.Forms.ToolStripLabel toolStripLabel1;
        private System.Windows.Forms.ToolStripMenuItem windowsStoreToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem epicStoreToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem installDirectoryToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem selectToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem searcgToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
        private System.Windows.Forms.ToolStripButton toolStripButton1;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator3;
        private RotatingLabel rotatingLabel1;
        private System.Windows.Forms.Button button4;
    }
}

