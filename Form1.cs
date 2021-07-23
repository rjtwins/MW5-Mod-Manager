using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Media;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Application = System.Windows.Forms.Application;

namespace MW5_Mod_Manager
{
    public partial class Form1 : Form
    {
        public Form1 MainForm;
        public MainLogic logic = new MainLogic();
        public TCPFileShare fileShare;
        private List<ListViewItem> backupListView;
        bool filtered = false;
        private List<ListViewItem> markedForRemoval;
        public Form4 WaitForm;
        private bool MovingItem = false;
        internal bool JustPacking = true;

        public Form1()
        {
            InitializeComponent();
            this.MainForm = this;
            this.logic.MainForm = this;
            this.fileShare = new TCPFileShare(logic, this);
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
            backgroundWorker1.WorkerReportsProgress = true;
            backgroundWorker1.WorkerSupportsCancellation = true;
            backgroundWorker2.WorkerReportsProgress = true;
            backgroundWorker2.WorkerSupportsCancellation = true;

            //start the TCP listner for TCP mod sharing
            this.fileShare.Listener.RunWorkerAsync();
        }

        //handling key presses for hotkeys.
        private async void form1_KeyUp(object sender, KeyEventArgs e)
        {
            //Console.WriteLine("KEY Released: " + e.KeyCode);
            if (e.KeyCode == Keys.ShiftKey)
            {
                await Task.Delay(50);
                this.button1.Text = "&UP";
                this.button2.Text = "&DOWN";
            }
        }

        private void form1_KeyDown(object sender, KeyEventArgs e)
        {
            //Console.WriteLine("KEY Pressed: " + e.KeyCode);
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
                foreach (string f in Directory.GetFiles(file))
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
                if (string.IsNullOrEmpty(logic.BasePath) || string.IsNullOrWhiteSpace(logic.BasePath) || logic.Vendor == "STEAM")
                {
                    //we may have found a mod but we have nowhere to put it :(
                    return;
                }
                string[] splitString = file.Split('\\');
                string modName = splitString[splitString.Length - 1];
                //Console.WriteLine(logic.BasePath + "\\" + modName);
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
                            //Console.WriteLine(entry.FullName);
                            if (entry.Name.Contains("mod.json"))
                            {
                                //we have found a mod!
                                //Console.WriteLine("MOD FOUND IN ZIP!: " + entry.FullName);
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
            this.MovingItem = true;
            bool movedToTop = false;
            int i = SelectedItemIndex();
            if (i < 1)
            {
                this.MovingItem = false;
                return;
            }
            ListViewItem item = listView1.Items[i];
            listView1.Items.RemoveAt(i);


            if (Control.ModifierKeys == Keys.Shift)
            {
                //Move to top
                movedToTop = true;
                listView1.Items.Insert(0, item);
            }
            else
            {
                //move one up
                listView1.Items.Insert(i - 1, item);
            }
            item.Selected = true;

            this.logic.GetOverridingData(this.listView1.Items);
            this.logic.CheckRequires(listView1.Items);
            listView1_SelectedIndexChanged(null, null);

            this.MovingItem = false;
        }

        //Down button
        //Get item info, remove item, insert below, set new item as selected.
        private void button2_Click(object sender, EventArgs e)
        {
            this.MovingItem = true;
            bool movedToBottom = false;
            int i = SelectedItemIndex();
            if (i > listView1.Items.Count - 2 || i < 0)
            {
                this.MovingItem = false;
                return;
            }

            ListViewItem item = listView1.Items[i];
            listView1.Items.RemoveAt(i);

            if (Control.ModifierKeys == Keys.Shift)
            {
                //Move to bottom
                movedToBottom = true;
                listView1.Items.Insert(listView1.Items.Count, item);
            }
            else
            {
                //move one down
                listView1.Items.Insert(i + 1, item);
            }
            item.Selected = true;

            this.logic.GetOverridingData(listView1.Items);
            this.logic.CheckRequires(listView1.Items);
            listView1_SelectedIndexChanged(null, null);

            this.MovingItem = false;
        }

        //Apply button
        private void button3_Click(object sender, EventArgs e)
        {
            #region mod removal

            //Stuff for removing mods:
            if (this.markedForRemoval.Count > 0)
            {
                List<string> modNames = new List<string>();
                foreach (ListViewItem item in this.markedForRemoval)
                {
                    modNames.Add(item.SubItems[1].Text);
                }

                string m = "The following mods will be permanently removed from your mods folder: " + string.Join(",", modNames) + ". ARE YOU SURE?";
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
            #endregion

            #region mod dependencies/requirments
            //Checking requirements:
            Dictionary<string, List<string>> CheckResult = logic.CheckRequires(listView1.Items);

            //Super ugly as we are undoing stuff we just did here but i'm lazy.
            foreach (ListViewItem item in this.listView1.Items)
            {
                item.SubItems[5].BackColor = Color.White;
            }

            if (CheckResult.Count > 0)
            {
                string wText = "";
                foreach (string key in CheckResult.Keys)
                {
                    wText += (key + "\n");
                    foreach (string value in CheckResult[key])
                    {
                        wText += ("--" + value + "\n");
                    }
                }

                string m2 = "Mods are missing or loaded after required dependencies: \n\n" + wText + "\nDo you want to apply anyway?";
                string c2 = "Mods Missing Dependencies";
                MessageBoxButtons b2 = MessageBoxButtons.YesNo;
                DialogResult r2 = MessageBox.Show(m2, c2, b2);
                if (r2 == DialogResult.No)
                    return;
            }
            #endregion

            #region Activation and Load order
            //Stuff for applying mods activation and load order:

            //Reset filter:
            this.filterBox.Text = "";
            this.filterBox_TextChanged(null, null);

            //Regenerate ModList dict
            this.logic.ModList = new Dictionary<string, bool>();

            //For each mod in the list view:
            //Check if mod enabled
            //Get its priority
            //Put mod in the ModList with it status
            //Adjust the ModDetails priority
            int length = listView1.Items.Count;
            for (int i = 0; i < length; i++)
            {
                string modName = listView1.Items[i].SubItems[2].Text;
                try
                {
                    bool modEnabled = listView1.Items[i].Checked;
                    int priority = listView1.Items.Count - i;
                    this.logic.ModList[modName] = modEnabled;
                    this.logic.ModDetails[modName].defaultLoadOrder = priority;
                    Console.WriteLine(modName + " : " + priority.ToString());
                }
                catch (Exception Ex)
                {
                    string message = "ERROR Mismatch between list key and details key : " + modName
                        + ". Details keys available: " + string.Join(",", this.logic.ModDetails.Keys.ToList()) + ". This mod will be skipped and the operation continued.";
                    string caption = "ERROR Key Mismatch";
                    MessageBoxButtons buttons = MessageBoxButtons.OK;
                    DialogResult Result = MessageBox.Show(message, caption, buttons);
                    continue;
                }

            }

            //Save the ModDetails to json file.
            this.logic.SaveToFiles();
            #endregion
        }

        //For clearing the entire applications data
        private void ClearAll()
        {
            this.listView1.Items.Clear();
            logic.ClearAll();
        }

        //For processing internals and updating ui after setting a vendor
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
                    item1.UseItemStyleForSubItems = false;
                    item1.Checked = entry.Value;
                    item1.SubItems.Add(logic.ModDetails[entry.Key].displayName);
                    item1.SubItems.Add(modName);
                    item1.SubItems.Add(logic.ModDetails[entry.Key].author);
                    item1.SubItems.Add(logic.ModDetails[entry.Key].version);
                    item1.SubItems.Add(" ");
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

        /*Stop Search Button
        //private void button5_Click(object sender, EventArgs e)
        //{
        //    this.button5.Enabled = false; //disable button since we are stopping the search
        //    backgroundWorker1.CancelAsync();
        //}
        */

        //Refresh listedcheckbox
        private void button6_Click(object sender, EventArgs e)
        {
            ClearAll();
            if (logic.TryLoadProgramData())
            {
                LoadAndFill(false);
                filterBox_TextChanged(null, null);
                if (true /*checkBox2.Checked*/)
                    logic.GetOverridingData(listView1.Items);
                if (true /*checkBox3.Checked*/)
                    logic.CheckRequires(listView1.Items);
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

        //Tool strip for selecting steam as a vendor
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

        //Tool strip for selecting gog as a vendor
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

        //Tool strip for selecting windows store as a vendor
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

        //Tool strip for selecting epic store as a vendor
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

        //Tool strip for selecting a install folder
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

        //Open mods folder
        //Only open if there is any folder in the base path.
        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(this.logic.BasePath) || string.IsNullOrWhiteSpace(this.logic.BasePath))
            {
                return;
            }
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

        //Epic vendor selection button
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
            if (MainForm.filterBox.Text == "" || string.IsNullOrWhiteSpace(MainForm.filterBox.Text))
            {
                Console.WriteLine("No filter text");
                if (this.filtered) //we are returning from filtering
                {
                    MainForm.listView1.Items.Clear();
                    foreach (ListViewItem item in this.backupListView)
                    {
                        item.BackColor = Color.White;
                        MainForm.listView1.Items.Add(item);
                        item.UseItemStyleForSubItems = false;
                    }
                }
                else //We are not returning from a filter
                {
                    // do nothing
                }
                MainForm.button1.Enabled = true;
                MainForm.button2.Enabled = true;
                MainForm.listBox1.Items.Clear();
                MainForm.listBox2.Items.Clear();
                MainForm.listBox3.Items.Clear();
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
                    foreach (ListViewItem item in MainForm.listView1.Items)
                    {
                        this.backupListView.Add(item);
                    }
                }

                MainForm.listView1.Items.Clear();
                foreach (ListViewItem x in this.backupListView)
                {
                    x.UseItemStyleForSubItems = true;
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
                        if (!MainForm.checkBox1.Checked)
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
                MainForm.listBox1.Items.Clear();
                MainForm.listBox2.Items.Clear();
                MainForm.listBox3.Items.Clear();
                this.filtered = true;
            }
        }

        //Filter or Highlight checkbox on tick action
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
                    item.SubItems[1].ForeColor = Color.Black;
                    item.Selected = false;
                }
                else
                {
                    this.markedForRemoval.Add(item);
                    item.SubItems[1].ForeColor = Color.Red;
                    item.Selected = false;
                }
            }
        }

        //Unused
        private void listBox2_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        //Selected index of mods overriding the currently selected mod has changed.
        private void listBox3_SelectedIndexChanged(object sender, EventArgs e)
        {
            listBox2.Items.Clear();
            listBox1.ClearSelected();
            if (listBox3.Items.Count == 0 || listView1.Items.Count == 0)
                return;

            if (listBox3.SelectedItem == null)
                return;

            string selectedMod = listBox3.SelectedItem.ToString();
            if (string.IsNullOrEmpty(selectedMod) || string.IsNullOrWhiteSpace(selectedMod))
                return;

            string superMod = listView1.SelectedItems[0].SubItems[2].Text;

            if (!logic.OverrridingData.ContainsKey(superMod))
                return;

            OverridingData modData = logic.OverrridingData[superMod];

            if (!modData.overriddenBy.ContainsKey(selectedMod))
                return;

            foreach (string entry in modData.overriddenBy[selectedMod])
            {
                listBox2.Items.Add(entry);
            }
        }

        //Selected indox of mods that are beeing overriden by the currently selected mod had changed.
        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            listBox2.Items.Clear();
            listBox3.ClearSelected();
            if (listBox1.Items.Count == 0 || listView1.Items.Count == 0)
                return;

            if (listBox1.SelectedItem == null)
                return;
            string selectedMod = listBox1.SelectedItem.ToString();

            if (string.IsNullOrEmpty(selectedMod) || string.IsNullOrWhiteSpace(selectedMod))
                return;

            string superMod = listView1.SelectedItems[0].SubItems[2].Text;

            if (!logic.OverrridingData.ContainsKey(superMod))
                return;

            OverridingData modData = logic.OverrridingData[superMod];

            foreach (string entry in modData.overrides[selectedMod])
            {
                listBox2.Items.Add(entry);
            }
        }

        //Selected item in the list view has cahnged
        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {
            this.label4.Text = "";

            if (listView1.SelectedItems.Count == 0)
                return;

            string SelectedMod = listView1.SelectedItems[0].SubItems[2].Text;
            string SelectedModDisplayName = listView1.SelectedItems[0].SubItems[1].Text;
            bool ItemChecked = listView1.SelectedItems[0].Checked;

            if (string.IsNullOrEmpty(SelectedMod) ||
                string.IsNullOrWhiteSpace(SelectedMod) ||
                string.IsNullOrEmpty(SelectedModDisplayName) ||
                string.IsNullOrWhiteSpace(SelectedModDisplayName)
                )
                return;

            if (!filtered)
            {
                HandleOverrding(SelectedMod);
                HandleDependencies(listView1.SelectedItems[0], SelectedModDisplayName);
            }
        }

        //Handles the showing of overrding data on select
        private void HandleOverrding(string SelectedMod)
        {
            if (logic.OverrridingData.Count == 0)
                return;

            this.listBox1.Items.Clear();
            this.listBox2.Items.Clear();
            this.listBox3.Items.Clear();

            this.label4.Text = SelectedMod;

            //If we select a mod that is not ticked its data is never gotten so will get an error if we don't do this.
            if (!logic.OverrridingData.ContainsKey(SelectedMod))
                return;

            OverridingData modData = logic.OverrridingData[SelectedMod];
            foreach (string orverriding in modData.overriddenBy.Keys)
            {
                this.listBox3.Items.Add(orverriding);
            }
            foreach (string overrides in modData.overrides.Keys)
            {
                this.listBox1.Items.Add(overrides);
            }
        }

        private void HandleDependencies(ListViewItem Item, string SelectedModDisplayName)
        {
            string SelectedMod = Item.SubItems[2].Text;
            this.MainForm.label8.Text = SelectedModDisplayName;
            List<string> Dependencies = logic.GetModDependencies(SelectedMod);
            this.listView2.Items.Clear();

            if (!Item.Checked)
            {
                Item.SubItems[5].BackColor = Color.White;
                Item.SubItems[5].Text = "---";
            }

            List<string> MissingDependencies = new List<string>();
            if (logic.MissingModsDependenciesDict.ContainsKey(SelectedModDisplayName))
            {
                MissingDependencies = logic.MissingModsDependenciesDict[SelectedModDisplayName];
            }

            if (Dependencies == null)
            {
                ListViewItem item = new ListViewItem();
                item.Text = "No Dependencies";
                return;
            }

            foreach (string mod in Dependencies)
            {
                ListViewItem item = new ListViewItem();
                item.Text = mod;
                if (MissingDependencies.Contains(mod))
                {
                    item.ForeColor = Color.Red;
                }
                listView2.Items.Add(item);
            }
        }

        //Fires when an item is checked or unchecked.
        private void listView1_ItemChecked(object sender, ItemCheckedEventArgs e)
        {
            //While we are removing/inserting items this will fire and we dont want that to happen when we move an item.
            if (MovingItem || this.filtered)
            {
                return;
            }

            logic.UpdateNewModOverrideData(listView1.Items, e.Item);
            logic.CheckRequires(listView1.Items);
            HandleOverrding(e.Item.SubItems[2].Text);
            HandleDependencies(e.Item, e.Item.SubItems[1].Text);
        }

        //Check for mod overrding data
        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            if (true /*checkBox2.Checked*/)
                this.logic.GetOverridingData(listView1.Items);
            else
            {
                foreach(ListViewItem item in listView1.Items)
                {
                    item.SubItems[1].ForeColor = Color.Black;
                    logic.OverrridingData.Clear();
                }
            }
        }

        //Check for mod requirements/dependencies
        private void checkBox3_CheckedChanged(object sender, EventArgs e)
        {
            if (true /*checkBox3.Checked*/)
            {
                this.logic.CheckRequires(listView1.Items);
            }
            else
            {
                foreach (ListViewItem item in listView1.Items)
                {
                    item.SubItems[5].BackColor = Color.White;
                    item.SubItems[5].Text = "";
                }
                listView2.Items.Clear();
            }
        }

        //On tap click?
        private void tabPage1_Click(object sender, EventArgs e)
        {

        }

        //Export all mods in the mods foler (after pressing apply)
        internal void exportModsFolderToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //apply current settings to file
            this.button3_Click(null, null);

            //start packing worker
            backgroundWorker1.RunWorkerAsync();
            //A little time to start up
            System.Threading.Thread.Sleep(100);
            //Start monitoring worker
            backgroundWorker2.RunWorkerAsync();

            //Show Form 4 with informing user that we are packaging mods..
            Console.WriteLine("Opening form:");
            this.WaitForm = new Form4(backgroundWorker1, backgroundWorker2);
            string message = "Packaging Mods.zip, this may take several minutes depending on the combinded size of your mods...";
            this.WaitForm.textBox1.Text = message;
            string caption = "Packing Mods.zip";
            this.WaitForm.Text = caption;
            WaitForm.ShowDialog(this);

            backgroundWorker2.CancelAsync();
            //For the rest of the code see "background"
        }

        #region background workers
        //Background worker stuff for search, no longer used.
        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            // Get the BackgroundWorker that raised this event.
            BackgroundWorker worker = sender as BackgroundWorker;
            this.logic.PackModsToZip(worker, e);
        }

        //Background worker stuff for search, no longer 
        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            // First, handle the case where an exception was thrown.
            if (e.Error != null)
            {
                MessageBox.Show(e.Error.Message);
            }
            else if (e.Cancelled || (string)e.Result == "ABORTED")
            {
                //we just wanna do nothing and return here.
                MainForm.WaitForm.Close();
                //MessageBox.Show("TEST123");
            }
            else
            {
                //We are actually done!
                MainForm.WaitForm.Close();

                //For when we just wanna pack and not show the dialog
                if (!JustPacking)
                {
                    JustPacking = true;
                    return;
                }

                //Returing from dialog:
                SystemSounds.Asterisk.Play();
                //Get parent dir
                string parent = Directory.GetParent(this.logic.BasePath).ToString();
                string m = "Done packing mods, output in: \n" + parent + "\\Mods.zip";
                string c = "Done";
                MessageBoxButtons b = MessageBoxButtons.OK;
                MessageBox.Show(m, c, b);
                Process.Start(parent);
            }
        }

        private void backgroundWorker2_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;
            logic.MonitorZipSize(worker, e);
            //We dont need to pass any results anywhere as we are just monitoring.
        }

        private void backgroundWorker2_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            // First, handle the case where an exception was thrown.
            if (e.Error != null)
            {
                MessageBox.Show(e.Error.Message);
            }
            else
            {
                //We are actually done!
            }
        }

        private void backgroundWorker2_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            MainForm.textBox1.Invoke((MethodInvoker)delegate {
                // Running on the UI thread
                MainForm.WaitForm.textProgressBar1.Value = e.ProgressPercentage;
            });
        }
        #endregion

        //unused
        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        //check all items.
        private void button10_Click(object sender, EventArgs e)
        {
            this.MovingItem = true;
            foreach (ListViewItem item in this.listView1.Items)
            {
                item.Checked = true;
            }
            this.MovingItem = false;
            this.logic.GetOverridingData(listView1.Items);
            this.logic.CheckRequires(listView1.Items);
        }

        //Disable all items
        private void button9_Click(object sender, EventArgs e)
        {
            this.MovingItem = true;
            foreach (ListViewItem item in this.listView1.Items)
            {
                item.Checked = false;
            }
            this.MovingItem = false;
            this.logic.GetOverridingData(listView1.Items);
            this.logic.CheckRequires(listView1.Items);
        }

        private void toolStripMenuItem1_Click(object sender, EventArgs e)
        {
            Console.WriteLine(logic.BasePath);
            Form5 form5 = new Form5(this);
            form5.ShowDialog(this);
        }
    }

    #region extra designer items
    //The rotating label for priority indication.
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

    public enum ProgressBarDisplayMode
    {
        NoText,
        Percentage,
        CurrProgress,
        CustomText,
        TextAndPercentage,
        TextAndCurrProgress
    }

    public class TextProgressBar : ProgressBar
    {
        [Description("Font of the text on ProgressBar"), Category("Additional Options")]
        public Font TextFont { get; set; } = new Font(FontFamily.GenericSerif, 11, FontStyle.Bold | FontStyle.Italic);

        private SolidBrush _textColourBrush = (SolidBrush)Brushes.Black;
        [Category("Additional Options")]
        public Color TextColor
        {
            get
            {
                return _textColourBrush.Color;
            }
            set
            {
                _textColourBrush.Dispose();
                _textColourBrush = new SolidBrush(value);
            }
        }

        private SolidBrush _progressColourBrush = (SolidBrush)Brushes.LightGreen;
        [Category("Additional Options"), Browsable(true), EditorBrowsable(EditorBrowsableState.Always)]
        public Color ProgressColor
        {
            get
            {
                return _progressColourBrush.Color;
            }
            set
            {
                _progressColourBrush.Dispose();
                _progressColourBrush = new SolidBrush(value);
            }
        }

        private ProgressBarDisplayMode _visualMode = ProgressBarDisplayMode.CurrProgress;
        [Category("Additional Options"), Browsable(true)]
        public ProgressBarDisplayMode VisualMode
        {
            get
            {
                return _visualMode;
            }
            set
            {
                _visualMode = value;
                Invalidate();//redraw component after change value from VS Properties section
            }
        }

        private string _text = string.Empty;

        [Description("If it's empty, % will be shown"), Category("Additional Options"), Browsable(true), EditorBrowsable(EditorBrowsableState.Always)]
        public string CustomText
        {
            get
            {
                return _text;
            }
            set
            {
                _text = value;
                Invalidate();//redraw component after change value from VS Properties section
            }
        }

        private string _textToDraw
        {
            get
            {
                string text = CustomText;

                switch (VisualMode)
                {
                    case (ProgressBarDisplayMode.Percentage):
                        text = _percentageStr;
                        break;
                    case (ProgressBarDisplayMode.CurrProgress):
                        text = _currProgressStr;
                        break;
                    case (ProgressBarDisplayMode.TextAndCurrProgress):
                        text = $"{CustomText}: {_currProgressStr}";
                        break;
                    case (ProgressBarDisplayMode.TextAndPercentage):
                        text = $"{CustomText}: {_percentageStr}";
                        break;
                }

                return text;
            }
            set { }
        }

        private string _percentageStr { get { return $"{(int)((float)Value - Minimum) / ((float)Maximum - Minimum) * 100 } %"; } }

        private string _currProgressStr
        {
            get
            {
                return $"{Value}/{Maximum}";
            }
        }

        public TextProgressBar()
        {
            Value = Minimum;
            FixComponentBlinking();
        }

        private void FixComponentBlinking()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;

            DrawProgressBar(g);

            DrawStringIfNeeded(g);
        }

        private void DrawProgressBar(Graphics g)
        {
            Rectangle rect = ClientRectangle;

            ProgressBarRenderer.DrawHorizontalBar(g, rect);

            rect.Inflate(-3, -3);

            if (Value > 0)
            {
                Rectangle clip = new Rectangle(rect.X, rect.Y, (int)Math.Round(((float)Value / Maximum) * rect.Width), rect.Height);

                g.FillRectangle(_progressColourBrush, clip);
            }
        }

        private void DrawStringIfNeeded(Graphics g)
        {
            if (VisualMode != ProgressBarDisplayMode.NoText)
            {

                string text = _textToDraw;

                SizeF len = g.MeasureString(text, TextFont);

                Point location = new Point(((Width / 2) - (int)len.Width / 2), ((Height / 2) - (int)len.Height / 2));

                g.DrawString(text, TextFont, (Brush)_textColourBrush, location);
            }
        }

        public new void Dispose()
        {
            _textColourBrush.Dispose();
            _progressColourBrush.Dispose();
            base.Dispose();
        }
    }
    #endregion
}