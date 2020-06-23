using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Windows.Forms;
using Application = System.Windows.Forms.Application;

namespace MW5_Mod_Manager
{
    public partial class Form1 : Form
    {
        public Form1 MainForm;
        public MainLogic logic;
        public Form1()
        {
            InitializeComponent();
            this.MainForm = this;

            backgroundWorker1.RunWorkerCompleted += backgroundWorker1_RunWorkerCompleted;
            backgroundWorker1.ProgressChanged += backgroundWorker1_ProgressChanged;
            backgroundWorker1.WorkerReportsProgress = true;
            backgroundWorker1.WorkerSupportsCancellation = true;
        }

        //called upon loading the form
        private void Form1_Load(object sender, EventArgs e)
        {
            this.logic = new MainLogic();
            if (logic.TryLoadInstallDir())
            {
                this.textBox1.Text = logic.BasePath;
                LoadAndFill(false);
            }
            this.rotatingLabel1.Text = "";             // which can be changed by NewText property
            this.rotatingLabel1.AutoSize = false;      // adjust according to your text
            this.rotatingLabel1.NewText = "<- Low Priority --- High Priority ->";     // whatever you want to display
            this.rotatingLabel1.ForeColor = Color.Black; // color to display
            this.rotatingLabel1.RotateAngle = -90;     // angle to rotate
        }

        //Up button
        //Get item info, remove item, insert above, set new item as selected.
        private void button1_Click(object sender, EventArgs e)
        {
            int i = SelectedItemIndex();
            if (i < 1)
                return;
            ListViewItem item = listView1.Items[i];
            listView1.Items.RemoveAt(i);
            listView1.Items.Insert(i - 1, item);
            item.Selected = true;
        }

        //Down button
        //Get item info, remove item, insert below, set new item as selected.
        private void button2_Click(object sender, EventArgs e)
        {

            int i = SelectedItemIndex();
            if (i > listView1.Items.Count - 2)
                return;

            ListViewItem item = listView1.Items[i];
            listView1.Items.RemoveAt(i);
            listView1.Items.Insert(i + 1, item);
            item.Selected = true;
        }
           
        //Apply button
        private void button3_Click(object sender, EventArgs e)
        {
            
            this.logic.ModList = new Dictionary<string, bool>();

            Console.WriteLine(this.logic.ModList.ToString());
            int length = listView1.Items.Count;
            for (int i = 0; i < length; i++)
            {
                string modName = listView1.Items[i].SubItems[2].Text;
                Console.WriteLine(modName.ToString());
                try
                {
                    bool modEnabled = listView1.Items[i].Checked;
                    int priority = listView1.Items.Count - i;
                    this.logic.ModList[modName] = modEnabled;
                    this.logic.ModDetails[modName].defaultLoadOrder = priority;
                }
                catch(Exception Ex)
                {
                    string message = "ERROR Mismatch between list key and details key : " + modName 
                        + ". Details keys available: " + string.Join(",", this.logic.ModDetails.Keys.ToList()) + ". This mod will be skipped and the operation continued.";
                    string caption = "ERROR Key Mismatch";
                    MessageBoxButtons buttons = MessageBoxButtons.OK;
                    DialogResult Result = MessageBox.Show(message, caption, buttons);
                    continue;
                }                   
            }
            this.logic.SaveToFiles();
        }

        private void ClearAll()
        {
            this.listView1.Items.Clear();
            logic.ClearAll();
        }

        //Load mod data and fill in the list box.
        private void LoadAndFill(bool FromClipboard)
        {
            if(this.logic.Vendor != "")
            {
                if(this.logic.Vendor == "EPIC")
                {
                    this.toolStripLabel1.Text = "Game Vendor : Epic Store";
                    this.selectToolStripMenuItem.Enabled = true;
                    this.searcgToolStripMenuItem.Enabled = true;
                    this.windowsStoreToolStripMenuItem.Enabled = true;
                    this.epicStoreToolStripMenuItem.Enabled = false;
                    this.button4.Enabled = true;
                }
                else if(this.logic.Vendor == "WINDOWS")
                {
                    this.toolStripLabel1.Text = "Game Vendor : Windows Store";
                    this.selectToolStripMenuItem.Enabled = false;
                    this.searcgToolStripMenuItem.Enabled = false;
                    this.windowsStoreToolStripMenuItem.Enabled = false;
                    this.epicStoreToolStripMenuItem.Enabled = true;
                    this.button4.Enabled = false;
                }
            }
            try 
            {
                if (FromClipboard)
                    logic.LoadStuff2();
                else
                    logic.Loadstuff();
                foreach (KeyValuePair<string, bool> entry in logic.ModList)
                {
                    string modName = entry.Key;
                    ListViewItem item1 = new ListViewItem("", 0);
                    item1.Checked = entry.Value;
                    item1.SubItems.Add(logic.ModDetails[entry.Key].displayName);
                    item1.SubItems.Add(modName);
                    item1.SubItems.Add(logic.ModDetails[entry.Key].author);
                    item1.SubItems.Add(logic.ModDetails[entry.Key].version);
                    listView1.Items.Add(item1);
                }

                this.logic.ProgramData.installdir = logic.BasePath;
                this.logic.ProgramData.vendor = logic.Vendor;

                string systemPath = System.Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
                string complete = Path.Combine(systemPath, @"MW5LoadOrderManager");
                JsonSerializer serializer = new JsonSerializer();
                serializer.Formatting = Formatting.Indented;
                using (StreamWriter sw = new StreamWriter(complete + @"\ProgramData.json"))
                using (JsonWriter writer = new JsonTextWriter(sw))
                {
                    serializer.Serialize(writer, logic.ProgramData);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.StackTrace);
                string message = "While loading and parsing mod.json files some unknown error has occurred, please contact the developer with a screen shot of the editor, the error message and your mod folder.";
                string caption = "Error Loading";
                MessageBoxButtons buttons = MessageBoxButtons.OK;
                MessageBox.Show(message, caption, buttons);
            }
        }

        //gets the index of the selected item.
        private int SelectedItemIndex()
        {
            for (int i = 0; i < listView1.Items.Count; i++)
            {
                if (listView1.Items[i].Selected == true)// getting selected value from CheckBox List  
                {
                    return i;
                }
            }
            return -1;
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            // Get the BackgroundWorker that raised this event.
            BackgroundWorker worker = sender as BackgroundWorker;
            e.Result = this.logic.FindInstallDir(worker, e);
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            // First, handle the case where an exception was thrown.
            if (e.Error != null)
            {
                MessageBox.Show(e.Error.Message);
            }
            else if (e.Cancelled)
            {
                // Next, handle the case where the user canceled 
                // the operation.
                // Note that due to a race condition in 
                // the DoWork event handler, the Cancelled
                // flag may not have been set, even though
                // CancelAsync was called.
            }
            else
            {

                string txt = (string)e.Result;
                MainForm.textBox1.Invoke((MethodInvoker)delegate {
                    // Running on the UI thread
                    MainForm.textBox1.Text = txt;
                });
                LoadAndFill(false);
            }
        }

        // This event handler updates the progress bar.
        private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            string txt = (string)e.UserState;
            MainForm.textBox1.Invoke((MethodInvoker)delegate {
                // Running on the UI thread
                MainForm.textBox1.Text = "Searching: " + txt;
            });
        }

        //Select install directory button
        private void SelectInstallDirectory()
        {
            ClearAll();
            using (var fbd = new FolderBrowserDialog())
            {
                DialogResult result = fbd.ShowDialog();

                if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
                {
                    logic.BasePath = fbd.SelectedPath + @"\MW5Mercs\Mods";
                    string txt = logic.BasePath;
                    MainForm.textBox1.Invoke((MethodInvoker)delegate {
                        // Running on the UI thread
                        MainForm.textBox1.Text = txt;
                    });

                    LoadAndFill(false);
                }
            }
        }

        //Stop Search Button
        private void button5_Click(object sender, EventArgs e)
        {
            backgroundWorker1.CancelAsync();
        }

        //Refresh listedcheckbox
        private void button6_Click(object sender, EventArgs e)
        {
            ClearAll();
            if (logic.TryLoadInstallDir())
            {
                LoadAndFill(false);
            }
        }

        //Image
        private void button8_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start(@"https://www.nexusmods.com/mechwarrior5mercenaries/mods/174?tab=description");
        }

        //Export load order
        private void exportLoadOrderToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string json = JsonConvert.SerializeObject(logic.ModList, Formatting.Indented);
            Form3 exportDialog = new Form3();

            // Show testDialog as a modal dialog and determine if DialogResult = OK.
            exportDialog.textBox1.Text = logic.Scramble(json);
            exportDialog.ShowDialog(this);
            exportDialog.Dispose();
        }

        //Import load order
        private void importLoadOrderToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Form2 testDialog = new Form2();
            string txtResult = "";

            // Show testDialog as a modal dialog and determine if DialogResult = OK.
            testDialog.ShowDialog(this);
            txtResult = testDialog.textBox1.Text;
            testDialog.Dispose();

            if (txtResult == "" || txtResult == " " ||
                txtResult == "Paste load order clipboard here, any mods that you do not have but are in the pasted load order will be ignored.")
                return;

            Dictionary<string, bool> temp;
            try
            {
                temp = JsonConvert.DeserializeObject<Dictionary<string, bool>>(logic.UnScramble(txtResult));
            }
            catch (Exception Ex)
            {
                string message = "There was an error in decoding the load order string.";
                string caption = "Load Order Decoding Error";
                MessageBoxButtons buttons = MessageBoxButtons.OK;
                MessageBox.Show(message, caption, buttons);
                return;
            }
            this.ClearAll();
            this.logic.ModList = temp;
            this.LoadAndFill(true);
        }

        private void windowsStoreToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ClearAll();
            logic.WhipeInstallDirMemory();
            this.logic.Vendor = "WINDOWS";
            this.toolStripLabel1.Text = "Game Vendor : Windows Store";
            this.selectToolStripMenuItem.Enabled = false;
            this.searcgToolStripMenuItem.Enabled = false;
            this.button5.Enabled = false;
            this.button4.Enabled = false;
            this.windowsStoreToolStripMenuItem.Enabled = false;
            this.epicStoreToolStripMenuItem.Enabled = true;

            this.logic.BasePath = Application.LocalUserAppDataPath.Replace(@"\MW5_Mod_Manager\MW5 Mod Manager\1.0.0.0", "") + @"\MW5Mercs\Saved\Mods";
            this.textBox1.Text = logic.BasePath;
            LoadAndFill(false);
        }

        private void epicStoreToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ClearAll();
            logic.WhipeInstallDirMemory();
            this.logic.Vendor = "EPIC";
            this.toolStripLabel1.Text = "Game Vendor : Epic Store";
            this.selectToolStripMenuItem.Enabled = true;
            this.searcgToolStripMenuItem.Enabled = true;
            this.button5.Enabled = true;
            this.button4.Enabled = true;
            this.windowsStoreToolStripMenuItem.Enabled = true;
            this.epicStoreToolStripMenuItem.Enabled = false;
            this.textBox1.Text = logic.BasePath;
        }

        private void selectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SelectInstallDirectory();
        }

        private void searcgToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ClearAll();
            backgroundWorker1.RunWorkerAsync();
        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            try
            {
                Process.Start(this.logic.BasePath);
            }
            catch (Win32Exception win32Exception)
            {
                Console.WriteLine(win32Exception.Message);
                Console.WriteLine(win32Exception.StackTrace);
                string message = "While trying to open the mods folder, windows has encountered an error. Your folder does not exist, is not valid or was not set.";
                string caption = "Error Opening Mods Folder";
                MessageBoxButtons buttons = MessageBoxButtons.OK;
                MessageBox.Show(message, caption, buttons);
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            if(this.logic.Vendor == "EPIC")
            {
                try
                {
                    Process.Start(@"com.epicgames.launcher://apps/Hoopoe?action=launch&silent=false");
                }
                catch (Exception Ex)
                {
                    Console.WriteLine(Ex.Message);
                    Console.WriteLine(Ex.StackTrace);
                    string message = "There was an error while trying to make EPIC Games Launcher laumch Mechwarrior 5.";
                    string caption = "Error Launching";
                    MessageBoxButtons buttons = MessageBoxButtons.OK;
                    MessageBox.Show(message, caption, buttons);
                }
            }else if (this.logic.Vendor == "WINDOWS")
            {
                //Dunno how this works at all.. 
                string message = "This feature is not available in this version.";
                string caption = "Feature not available.";
                MessageBoxButtons buttons = MessageBoxButtons.OK;
                MessageBox.Show(message, caption, buttons);
            }

        }
    }

    public class RotatingLabel : System.Windows.Forms.Label
    {
        private int m_RotateAngle = 0;
        private string m_NewText = string.Empty;

        public int RotateAngle { get { return m_RotateAngle; } set { m_RotateAngle = value; Invalidate(); } }
        public string NewText { get { return m_NewText; } set { m_NewText = value; Invalidate(); } }

        protected override void OnPaint(System.Windows.Forms.PaintEventArgs e)
        {
            Func<double, double> DegToRad = (angle) => Math.PI * angle / 180.0;

            Brush b = new SolidBrush(this.ForeColor);
            SizeF size = e.Graphics.MeasureString(this.NewText, this.Font, this.Parent.Width);

            int normalAngle = ((RotateAngle % 360) + 360) % 360;
            double normaleRads = DegToRad(normalAngle);

            int hSinTheta = (int)Math.Ceiling((size.Height * Math.Sin(normaleRads)));
            int wCosTheta = (int)Math.Ceiling((size.Width * Math.Cos(normaleRads)));
            int wSinTheta = (int)Math.Ceiling((size.Width * Math.Sin(normaleRads)));
            int hCosTheta = (int)Math.Ceiling((size.Height * Math.Cos(normaleRads)));

            int rotatedWidth = Math.Abs(hSinTheta) + Math.Abs(wCosTheta);
            int rotatedHeight = Math.Abs(wSinTheta) + Math.Abs(hCosTheta);

            this.Width = rotatedWidth;
            this.Height = rotatedHeight;

            int numQuadrants =
                (normalAngle >= 0 && normalAngle < 90) ? 1 :
                (normalAngle >= 90 && normalAngle < 180) ? 2 :
                (normalAngle >= 180 && normalAngle < 270) ? 3 :
                (normalAngle >= 270 && normalAngle < 360) ? 4 :
                0;

            int horizShift = 0;
            int vertShift = 0;

            if (numQuadrants == 1)
            {
                horizShift = Math.Abs(hSinTheta);
            }
            else if (numQuadrants == 2)
            {
                horizShift = rotatedWidth;
                vertShift = Math.Abs(hCosTheta);
            }
            else if (numQuadrants == 3)
            {
                horizShift = Math.Abs(wCosTheta);
                vertShift = rotatedHeight;
            }
            else if (numQuadrants == 4)
            {
                vertShift = Math.Abs(wSinTheta);
            }

            e.Graphics.TranslateTransform(horizShift, vertShift);
            e.Graphics.RotateTransform(this.RotateAngle);

            e.Graphics.DrawString(this.NewText, this.Font, b, 0f, 0f);
            base.OnPaint(e);
        }
    }
}