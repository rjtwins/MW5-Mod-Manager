using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Application = System.Windows.Forms.Application;

namespace MW5_Mod_Manager
{
    public partial class Form1 : Form
    {
        public Form1 MainForm;
        public MainLogic logic;
        private List<ListViewItem> backupListView;
        bool filtered = false;
        private List<ListViewItem> markedForRemoval;
        public Form1()
        {
            InitializeComponent();
            this.MainForm = this;
            this.backupListView = new List<ListViewItem>();
            this.markedForRemoval = new List<ListViewItem>();

            this.AllowDrop = true;
            this.DragEnter += new DragEventHandler(Form1_DragEnter);
            this.DragDrop += new DragEventHandler(Form1_DragDrop);

            this.BringToFront();
            this.Focus();
            this.KeyPreview = true;

            this.KeyDown += new KeyEventHandler(form1_KeyDown);
            this.KeyUp += new KeyEventHandler(form1_KeyUp);

            backgroundWorker1.RunWorkerCompleted += backgroundWorker1_RunWorkerCompleted;
            backgroundWorker1.ProgressChanged += backgroundWorker1_ProgressChanged;
            backgroundWorker1.WorkerReportsProgress = true;
            backgroundWorker1.WorkerSupportsCancellation = true;
        }

        //handling key presses for hotkeys.
        private async void form1_KeyUp(object sender, KeyEventArgs e)
        {
            Console.WriteLine("KEY Released: " + e.KeyCode);
            if(e.KeyCode == Keys.ShiftKey)
            {
                await Task.Delay(50);
                this.button1.Text = "UP";
                this.button2.Text = "DOWN";
            }
        }

        private void form1_KeyDown(object sender, KeyEventArgs e)
        {
            Console.WriteLine("KEY Pressed: " + e.KeyCode);
            if (e.Shift)
            {
                this.button1.Text = "MOVE TO TOP";
                this.button2.Text = "MOVE TO BOTTOM";
            }
        }

        //called upon loading the form
        private void Form1_Load(object sender, EventArgs e)
        {
            this.logic = new MainLogic();
            if (logic.TryLoadProgramData())
            {
                this.textBox1.Text = logic.BasePath;
                LoadAndFill(false);
            }
            this.SetVersionAndVender();

            this.rotatingLabel1.Text = "";             // which can be changed by NewText property
            this.rotatingLabel1.AutoSize = false;      // adjust according to your text
            this.rotatingLabel1.NewText = "<- Low Priority/Loaded First --- High Priority/Loaded Last ->";     // whatever you want to display
            this.rotatingLabel1.ForeColor = Color.Black; // color to display
            this.rotatingLabel1.RotateAngle = -90;     // angle to rotate
            //this.button5.Enabled = false; //set Stop Search Button to disabled so the user won't be confused if a search isn't on-going
        }

        //When we hover over the manager with a file or folder
        void Form1_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Copy;
        }

        //When we drop a file or folder on the manager
        void Form1_DragDrop(object sender, DragEventArgs e)
        {
            //We only support single file drops!
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files.Length != 1)
                return;
            string file = files[0];

            //Lets see what we got here
            // get the file attributes for file or directory
            FileAttributes attr = File.GetAttributes(file);
            if (attr.HasFlag(FileAttributes.Directory))
            {
                //Directory
                //Check if we have a mod.json
                bool foundMod = false;
                foreach(string f in Directory.GetFiles(file))
                {
                    if (f.Contains("mod.json"))
                    {
                        foundMod = true;
                        break;
                    }
                }
                if (!foundMod)
                {
                    //No mods found
                    return;
                }
                //we've got a mod people!
                if(string.IsNullOrEmpty(logic.BasePath) || string.IsNullOrWhiteSpace(logic.BasePath) || logic.Vendor == "STEAM")
                {
                    //we may have found a mod but we have nowhere to put it :(
                    return;
                }
                string[] splitString = file.Split('\\');
                string modName = splitString[splitString.Length - 1];
                Console.WriteLine(logic.BasePath + "\\" + modName);
                Utils.DirectoryCopy(file, logic.BasePath + "\\" + modName, true);
                button6_Click(null, null);
            }
            else
            {
                //Its a file!
                if (file.Contains(".zip"))
                {
                    //we have a zip!
                    using (ZipArchive archive = ZipFile.OpenRead(file))
                    {
                        bool modFound = false;
                        foreach (ZipArchiveEntry entry in archive.Entries)
                        {
                            Console.WriteLine(entry.FullName);
                            if (entry.Name.Contains("mod.json"))
                            {
                                //we have found a mod!
                                Console.WriteLine("MOD FOUND IN ZIP!: " + entry.FullName);
                                modFound = true;
                                break;
                            }
                        }
                        if (!modFound)
                        {
                            return;
                        }
                        //Extract mod to mods dir
                        ZipFile.ExtractToDirectory(file, logic.BasePath);
                        button6_Click(null, null);
                    }
                }
                else
                {
                    string message = "Only .zip files are supported. " +
                        "Please extract first and drag the folder into the application.";
                    string caption = "Unsuported File Type";
                    MessageBoxButtons buttons = MessageBoxButtons.OK;
                    DialogResult Result = MessageBox.Show(message, caption, buttons);
                    return;
                }
            }
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

            
            if (Control.ModifierKeys == Keys.Shift)
            {
                //Move to top
                listView1.Items.Insert(0, item);
            }
            else
            {
                //move one up
                listView1.Items.Insert(i - 1, item);
            }
            item.Selected = true;

        }

        //Down button
        //Get item info, remove item, insert below, set new item as selected.
        private void button2_Click(object sender, EventArgs e)
        {
            int i = SelectedItemIndex();
            if (i > listView1.Items.Count - 2 || i < 0)
                return;

            ListViewItem item = listView1.Items[i];
            listView1.Items.RemoveAt(i);

            if (Control.ModifierKeys == Keys.Shift)
            {
                //Move to buttom
                listView1.Items.Insert(listView1.Items.Count, item);
            }
            else
            {
                //move one down
                listView1.Items.Insert(i + 1, item);
            }
            item.Selected = true;


            item.Selected = true;
        }
           
        //Apply button
        private void button3_Click(object sender, EventArgs e)
        {
            //Stuff for removing mods:
            if(this.markedForRemoval.Count > 0)
            {
                List<string> modNames = new List<string>();
                foreach (ListViewItem item in this.markedForRemoval)
                {
                    modNames.Add(item.SubItems[1].Text);
                }

                string m = "The following mods will be permenetly removed from your mods folder: " + string.Join(",", modNames) + ". ARE YOU SURE?";
                string c = "Are you sure?";
                MessageBoxButtons b = MessageBoxButtons.YesNo;
                DialogResult r = MessageBox.Show(m, c, b);

                if (r == DialogResult.Yes)
                {
                    foreach (ListViewItem item in markedForRemoval)
                    {
                        listView1.Items.Remove(item);
                        logic.DeleteMod(item.SubItems[2].Text);
                        this.logic.ModDetails.Remove(item.SubItems[2].Text);
                    }
                    markedForRemoval.Clear();
                }
                else if (r == DialogResult.No)
                {
                    foreach (ListViewItem item in markedForRemoval)
                    {
                        item.ForeColor = Color.Black;
                    }
                    return;
                }
            }

            //Stuff for applying mods activation and load order:
            this.logic.ModList = new Dictionary<string, bool>();
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

        private void SetVersionAndVender()
        {
            if (this.logic.Version > 0f)
            {
                this.label1.Text = @"~RJ v." + this.logic.Version.ToString();
            }
            if (this.logic.Vendor != "")
            {
                if (this.logic.Vendor == "EPIC")
                {
                    this.toolStripLabel1.Text = "Game Vendor : Epic Store";
                    this.selectToolStripMenuItem.Enabled = true;
                    //this.searcgToolStripMenuItem.Enabled = true;
                    this.steamToolStripMenuItem.Enabled = true;
                    this.gogToolStripMenuItem.Enabled = true;
                    this.windowsStoreToolStripMenuItem.Enabled = true;
                    this.epicStoreToolStripMenuItem.Enabled = false;
                    this.button4.Enabled = true;
                    this.MainForm.button5.Enabled = true;
                }
                else if (this.logic.Vendor == "WINDOWS")
                {
                    this.toolStripLabel1.Text = "Game Vendor : Windows Store";
                    this.selectToolStripMenuItem.Enabled = false;
                    //this.searcgToolStripMenuItem.Enabled = false;
                    this.steamToolStripMenuItem.Enabled = true;
                    this.gogToolStripMenuItem.Enabled = true;
                    this.windowsStoreToolStripMenuItem.Enabled = false;
                    this.epicStoreToolStripMenuItem.Enabled = true;
                    this.button4.Enabled = false;
                    this.MainForm.button5.Enabled = true;

                }
                else if (this.logic.Vendor == "STEAM")
                {
                    this.toolStripLabel1.Text = "Game Vendor : Steam";
                    this.selectToolStripMenuItem.Enabled = true;
                    //this.searcgToolStripMenuItem.Enabled = true;
                    this.steamToolStripMenuItem.Enabled = false;
                    this.gogToolStripMenuItem.Enabled = true;
                    this.windowsStoreToolStripMenuItem.Enabled = true;
                    this.epicStoreToolStripMenuItem.Enabled = true;
                    this.MainForm.button5.Enabled = false;
                    this.button4.Enabled = true;

                }
                else if (this.logic.Vendor == "GOG")
                {
                    this.toolStripLabel1.Text = "Game Vendor : GOG";
                    this.selectToolStripMenuItem.Enabled = true;
                    //this.searcgToolStripMenuItem.Enabled = true;
                    this.steamToolStripMenuItem.Enabled = true;
                    this.gogToolStripMenuItem.Enabled = false;
                    this.windowsStoreToolStripMenuItem.Enabled = true;
                    this.epicStoreToolStripMenuItem.Enabled = true;
                    this.button4.Enabled = true;
                    this.MainForm.button5.Enabled = true;
                }
            }
        }

        //Load mod data and fill in the list box..
        private void LoadAndFill(bool FromClipboard)
        {
            KeyValuePair<string, bool> currentEntry = new KeyValuePair<string, bool>();
            try 
            {
                if (FromClipboard)
                    logic.LoadStuff2();
                else
                    logic.Loadstuff();
                foreach (KeyValuePair<string, bool> entry in logic.ModList)
                {
                    currentEntry = entry;
                    string modName = entry.Key;
                    ListViewItem item1 = new ListViewItem("", 0);
                    item1.Checked = entry.Value;
                    item1.SubItems.Add(logic.ModDetails[entry.Key].displayName);
                    item1.SubItems.Add(modName);
                    item1.SubItems.Add(logic.ModDetails[entry.Key].author);
                    item1.SubItems.Add(logic.ModDetails[entry.Key].version);
                    listView1.Items.Add(item1);
                }

                logic.SaveProgramData();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.StackTrace);
                string message = "While loading " + currentEntry.Key.ToString() + "something went wrong.";
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
                //we just wanna do nothing and return here.
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

        ////Stop Search Button
        //private void button5_Click(object sender, EventArgs e)
        //{
        //    this.button5.Enabled = false; //disable button since we are stopping the search
        //    backgroundWorker1.CancelAsync();
        //}

        //Refresh listedcheckbox
        private void button6_Click(object sender, EventArgs e)
        {
            ClearAll();
            if (logic.TryLoadProgramData())
            {
                LoadAndFill(false);
                filterBox_TextChanged(null, null);
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
            exportDialog.textBox1.Text = json; //logic.Scramble(json);
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
                temp = JsonConvert.DeserializeObject<Dictionary<string, bool>>(txtResult);//logic.UnScramble(txtResult));
                Console.WriteLine("OUTPUT HERE!");
                Console.WriteLine(JsonConvert.SerializeObject(temp, Formatting.Indented));

            }
            catch (Exception Ex)
            {
                string message = "There was an error in decoding the load order string.";
                string caption = "Load Order Decoding Error";
                MessageBoxButtons buttons = MessageBoxButtons.OK;
                MessageBox.Show(message, caption, buttons);
                return;
            }
            //this.ClearAll();

            this.listView1.Items.Clear();
            this.logic.ModDetails = new Dictionary<string, ModObject>();
            this.logic.ModList = new Dictionary<string, bool>();
            this.logic.ModList = temp;
            this.LoadAndFill(true);
            this.filterBox_TextChanged(null, null);
        }

        private void steamToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ClearAll();
            logic.WhipeInstallDirMemory();
            this.logic.Vendor = "STEAM";
            this.toolStripLabel1.Text = "Game Vendor : Steam";
            this.selectToolStripMenuItem.Enabled = true;
            //this.searcgToolStripMenuItem.Enabled = true;
            this.button4.Enabled = true;
            this.steamToolStripMenuItem.Enabled = false;
            this.windowsStoreToolStripMenuItem.Enabled = true;
            this.epicStoreToolStripMenuItem.Enabled = true;
            this.MainForm.button5.Enabled = false;
            this.textBox1.Text = logic.BasePath;
            LoadAndFill(false);
        }
        private void gogToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ClearAll();
            logic.WhipeInstallDirMemory();
            this.logic.Vendor = "GOG";
            this.toolStripLabel1.Text = "Game Vendor : GOG";
            this.selectToolStripMenuItem.Enabled = true;
            //this.searcgToolStripMenuItem.Enabled = true;
            this.button4.Enabled = true;
            this.steamToolStripMenuItem.Enabled = true;
            this.gogToolStripMenuItem.Enabled = false;
            this.windowsStoreToolStripMenuItem.Enabled = true;
            this.epicStoreToolStripMenuItem.Enabled = true;
            this.textBox1.Text = logic.BasePath;
            this.MainForm.button5.Enabled = true;
            LoadAndFill(false);
        }

        private void windowsStoreToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ClearAll();
            logic.WhipeInstallDirMemory();
            this.logic.Vendor = "WINDOWS";
            this.toolStripLabel1.Text = "Game Vendor : Windows Store";
            this.selectToolStripMenuItem.Enabled = false;
            //this.searcgToolStripMenuItem.Enabled = false;
            //this.button5.Enabled = false;
            this.button4.Enabled = false;
            this.steamToolStripMenuItem.Enabled = true;
            this.gogToolStripMenuItem.Enabled = true;
            this.windowsStoreToolStripMenuItem.Enabled = false;
            this.epicStoreToolStripMenuItem.Enabled = true;
            this.logic.BasePath = Application.LocalUserAppDataPath.Replace(@"\MW5_Mod_Manager\MW5 Mod Manager\1.0.0.0", "") + @"\MW5Mercs\Saved\Mods";
            this.textBox1.Text = logic.BasePath;
            this.MainForm.button5.Enabled = true;
            LoadAndFill(false);
        }

        private void epicStoreToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ClearAll();
            logic.WhipeInstallDirMemory();
            this.logic.Vendor = "EPIC";
            this.toolStripLabel1.Text = "Game Vendor : Epic Store";
            this.selectToolStripMenuItem.Enabled = true;
            //this.searcgToolStripMenuItem.Enabled = true;
            this.button4.Enabled = true;
            this.steamToolStripMenuItem.Enabled = true;
            this.gogToolStripMenuItem.Enabled = true;
            this.windowsStoreToolStripMenuItem.Enabled = true;
            this.epicStoreToolStripMenuItem.Enabled = false;
            this.textBox1.Text = logic.BasePath;
            this.MainForm.button5.Enabled = true;
        }

        private void selectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SelectInstallDirectory();
        }

        //private void searcgToolStripMenuItem_Click(object sender, EventArgs e)
        //{
        //    ClearAll();
        //    this.button5.Enabled = true; //enable the Stop Search button here so it's only enabled while we are searching
        //    backgroundWorker1.RunWorkerAsync();
        //}

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
            if (this.logic.Vendor == "EPIC")
            {
                try
                {
                    Process.Start(@"com.epicgames.launcher://apps/Hoopoe?action=launch&silent=false");
                }
                catch (Exception Ex)
                {
                    Console.WriteLine(Ex.Message);
                    Console.WriteLine(Ex.StackTrace);
                    string message = "There was an error while trying to make EPIC Games Launcher launch Mechwarrior 5.";
                    string caption = "Error Launching";
                    MessageBoxButtons buttons = MessageBoxButtons.OK;
                    MessageBox.Show(message, caption, buttons);
                }
            }
            else if (this.logic.Vendor == "STEAM")
            {
                try
                {
                    System.Diagnostics.Process.Start(@"steam://rungameid/784080");
                }
                catch (Exception Ex)
                {
                    Console.WriteLine(Ex.Message);
                    Console.WriteLine(Ex.StackTrace);
                    string message = "There was an error while trying to make Steam launch Mechwarrior 5.";
                    string caption = "Error Launching";
                    MessageBoxButtons buttons = MessageBoxButtons.OK;
                    MessageBox.Show(message, caption, buttons);
                }
            }
            else if (this.logic.Vendor == "GOG")
            {
                string Gamepath = this.logic.BasePath;
                Gamepath = Gamepath.Remove(Gamepath.Length - 13, 13);
                Gamepath += "MechWarrior.exe";
                try
                {
                    Process.Start(Gamepath);
                }
                catch (Exception Ex)
                {
                    Console.WriteLine(Ex.Message);
                    Console.WriteLine(Ex.StackTrace);
                    string message = "There was an error while trying to launch Mechwarrior 5.";
                    string caption = "Error Launching";
                    MessageBoxButtons buttons = MessageBoxButtons.OK;
                    MessageBox.Show(message, caption, buttons);
                }
            }
            else if (this.logic.Vendor == "WINDOWS")
            {
                //Dunno how this works at all.. 
                string message = "This feature is not available in this version.";
                string caption = "Feature not available.";
                MessageBoxButtons buttons = MessageBoxButtons.OK;
                MessageBox.Show(message, caption, buttons);
            }

        }

        //Crude filter because to lazy to add a proper list as backup for the items.
        private void filterBox_TextChanged(object sender, EventArgs e)
        {
            Console.WriteLine("There are " + this.backupListView.Count() + " items in the backup");
            string filtertext = MainForm.filterBox.Text.ToLower();
            if(MainForm.filterBox.Text == "" || string.IsNullOrWhiteSpace(MainForm.filterBox.Text))
            {
                Console.WriteLine("No filter text");
                if (this.filtered) //we are returning from filtering
                {
                    MainForm.listView1.Items.Clear();
                    foreach (ListViewItem item in this.backupListView)
                    {
                        item.BackColor = Color.White;
                        MainForm.listView1.Items.Add(item);
                    }
                }
                else //We are not returning from a filter
                {
                    // do nothing
                }
                MainForm.button1.Enabled = true;
                MainForm.button2.Enabled = true;
                this.filtered = false;
            }
            else
            {
                Console.WriteLine("Filter Text!");
                Console.WriteLine(filtertext);
                if (!this.filtered && !string.IsNullOrWhiteSpace(filtertext) && !string.IsNullOrEmpty(filtertext)) // we are staring a filter now!
                {
                    //make a backup
                    this.backupListView.Clear();
                    foreach(ListViewItem item in MainForm.listView1.Items)
                    {
                        this.backupListView.Add(item);
                    }
                }

                MainForm.listView1.Items.Clear();
                foreach (ListViewItem x in this.backupListView)
                {
                    x.BackColor = Color.White;
                }
                //Check if the items modname, foltername or author stars with or contains the filter text
                foreach (ListViewItem item in this.backupListView)
                {
                    if (
                        item.SubItems[1].Text.ToLower().StartsWith(filtertext) ||
                        item.SubItems[2].Text.ToLower().StartsWith(filtertext) ||
                        item.SubItems[3].Text.ToLower().StartsWith(filtertext) ||
                        item.SubItems[1].Text.ToLower().Contains(filtertext) ||
                        item.SubItems[2].Text.ToLower().Contains(filtertext) ||
                        item.SubItems[3].Text.ToLower().Contains(filtertext)
                        )
                    {
                        if(!MainForm.checkBox1.Checked)
                            MainForm.listView1.Items.Add(item);
                        else
                        {
                            item.BackColor = Color.Yellow;
                        }
                    }
                }
                if (MainForm.checkBox1.Checked)
                {
                    foreach (ListViewItem item in this.backupListView)
                    {
                        MainForm.listView1.Items.Add(item);
                    }
                }
                MainForm.button1.Enabled = false;
                MainForm.button2.Enabled = false;
                this.filtered = true;
            }
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            this.filterBox_TextChanged(null, null);
        }

        //Mark currently selected mod for removal upon apply
        private void button5_Click(object sender, EventArgs e)
        {
            foreach (ListViewItem item in this.MainForm.listView1.SelectedItems)
            {
                if (this.markedForRemoval.Contains(item))
                {
                    markedForRemoval.Remove(item);
                    item.ForeColor = Color.Black;
                    item.Selected = false;
                }
                else
                {
                    this.markedForRemoval.Add(item);
                    item.ForeColor = Color.Red;
                    item.Selected = false;
                }
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