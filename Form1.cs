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
using System.Threading.Tasks;
using System.Windows.Forms;
using Application = System.Windows.Forms.Application;

namespace MW5_Mod_Manager
{
    public partial class Form1 : Form
    {
        public Form1 MainForm;
        public MainLogic logic = new MainLogic();
        //public TCPFileShare fileShare;
        bool filtered = false;
        //We can just use a list here since they guarantee order.
        private List<ModItem> ListViewData = new List<ModItem>();
        private List<ListViewItem> markedForRemoval;
        public Form4 WaitForm;
        private bool MovingItem = false;
        internal bool JustPacking = true;

        public bool LoadingAndFilling { get; private set; }

        public Form1()
        {
            InitializeComponent();
            this.MainForm = this;
            this.logic.MainForm = this;
            //this.fileShare = new TCPFileShare(logic, this);
            this.markedForRemoval = new List<ListViewItem>();

            this.AllowDrop = true;
            this.DragEnter += new DragEventHandler(Form1_DragEnter);
            this.DragDrop += new DragEventHandler(Form1_DragDrop);

            this.listBox4.MouseDoubleClick += new MouseEventHandler(listBox4_OnMouseClick);

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
            //Disabled for now.
            //this.fileShare.Listener.RunWorkerAsync();
        }

        //called upon loading the form
        private void Form1_Load(object sender, EventArgs e)
        {
            this.logic = new MainLogic();
            if (logic.TryLoadProgramData())
            {
                this.textBox1.Text = logic.BasePath[0];
                LoadAndFill(false);
            }
            this.LoadPresets();
            this.SetVersionAndVender();

            SetupRotatingLabel();
        }

        private void SetupRotatingLabel()
        {
            this.rotatingLabel1.Text = "";                  // which can be changed by NewText property
            this.rotatingLabel1.AutoSize = false;           // adjust according to your text
            this.rotatingLabel1.NewText = "<- Low Priority/Loaded First --- High Priority/Loaded Last ->";     // whatever you want to display
            this.rotatingLabel1.ForeColor = Color.Black;    // color to display
            this.rotatingLabel1.RotateAngle = -90;          // angle to rotate
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
            {
                return;
            }
            string file = files[0];

            //Lets see what we got here
            // get the file attributes for file or directory
            FileAttributes attr = File.GetAttributes(file);
            bool IsDirectory = attr.HasFlag(FileAttributes.Directory);

            HandleDirectory();

            HandleFile();

            //Refresh button
            button6_Click(null, null);

            void HandleFile()
            {
                if (!file.Contains(".zip"))
                {
                    string message = "Only .zip files are supported. " +
                        "Please extract first and drag the folder into the application.";
                    string caption = "Unsuported File Type";
                    MessageBoxButtons buttons = MessageBoxButtons.OK;
                    MessageBox.Show(message, caption, buttons);
                    return;
                }
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
                    ZipFile.ExtractToDirectory(file, logic.BasePath[0]);
                    button6_Click(null, null);
                }
            }

            void HandleDirectory()
            {
                if (!IsDirectory)
                {
                    return;
                }
                if (!ModInDirectory(file))
                {
                    return;
                }
                if (ModsFolderNotSet())
                {
                    return;
                }

                string modName;
                string[] splitString = file.Split('\\');
                modName = splitString[splitString.Length - 1];
                Utils.DirectoryCopy(file, logic.BasePath[0] + "\\" + modName, true);
            }

            bool ModInDirectory(string _file)
            {
                bool foundMod = false;
                foreach (string f in Directory.GetFiles(_file))
                {
                    if (f.Contains("mod.json"))
                    {
                        foundMod = true;
                        break;
                    }
                }

                return foundMod;
            }

            bool ModsFolderNotSet()
            {
                return !Utils.StringNullEmptyOrWhiteSpace(logic.BasePath[0]);
            }
        }

        //Up button
        //Get item info, remove item, insert above, set new item as selected.
        private void button1_Click(object sender, EventArgs e)
        {
            ListView.ListViewItemCollection items = listView1.Items;
            this.MovingItem = true;
            bool movedToTop = false;
            int i = SelectedItemIndex();
            if (i < 1)
            {
                this.MovingItem = false;
                return;
            }
            ModItem item = ListViewData[i];
            items.RemoveAt(i);
            ListViewData.RemoveAt(i);

            if (Control.ModifierKeys == Keys.Shift)
            {
                //Move to top
                movedToTop = true;
                items.Insert(0, item);
                ListViewData.Insert(0, item);

            }
            else
            {
                //move one up
                items.Insert(i - 1, item);
                ListViewData.Insert(i - 1, item);

            }
            item.Selected = true;

            this.logic.GetOverridingData(this.ListViewData);
            this.logic.CheckRequires(this.ListViewData);
            listView1_SelectedIndexChanged(null, null);
            this.MovingItem = false;
        }

        //Down button
        //Get item info, remove item, insert below, set new item as selected.
        private void button2_Click(object sender, EventArgs e)
        {
            ListView.ListViewItemCollection items = listView1.Items;
            this.MovingItem = true;
            bool movedToBottom = false;
            int i = SelectedItemIndex();
            if (i > ListViewData.Count - 2 || i < 0)
            {
                this.MovingItem = false;
                return;
            }

            ModItem item = ListViewData[i];
            items.RemoveAt(i);
            ListViewData.RemoveAt(i);

            if (Control.ModifierKeys == Keys.Shift)
            {
                //Move to bottom
                movedToBottom = true;
                items.Insert(ListViewData.Count, item);
                ListViewData.Insert(ListViewData.Count, item);
            }
            else
            {
                //move one down
                items.Insert(i + 1, item);
                ListViewData.Insert(i + 1, item);
            }
            item.Selected = true;

            //Move to below when refactor is complete
            //UpdateListView();

            this.logic.GetOverridingData(ListViewData);
            this.logic.CheckRequires(ListViewData);
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

                string m = "The following mods will be permanently be removed:\n" + string.Join("\n---", modNames) + "\nARE YOU SURE?";
                string c = "Are you sure?";
                MessageBoxButtons b = MessageBoxButtons.YesNo;
                DialogResult r = MessageBox.Show(m, c, b);

                if (r == DialogResult.Yes)
                {
                    foreach (ModItem item in markedForRemoval)
                    {
                        ListViewData.Remove(item);
                        listView1.Items.Remove(item);
                        logic.DeleteMod(logic.DirectoryToPathDict[item.SubItems[2].Text]);
                        this.logic.ModDetails.Remove(logic.DirectoryToPathDict[item.SubItems[2].Text]);
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
            Dictionary<string, List<string>> CheckResult = logic.CheckRequires(ListViewData);

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
                string modDir = logic.DirectoryToPathDict[modName];
                try
                {
                    bool modEnabled = listView1.Items[i].Checked;
                    int priority = listView1.Items.Count - i;
                    this.logic.ModList[modDir] = modEnabled;
                    this.logic.ModDetails[modDir].defaultLoadOrder = priority;
                    Console.WriteLine(modDir + " : " + priority.ToString());
                }
                catch (Exception Ex)
                {
                    string message = "ERROR Mismatch between list key and details key : " + modName
                        + ". Details keys available: " + string.Join(",", this.logic.ModDetails.Keys.ToList()) + ". This mod will be skipped and the operation continued.";
                    string caption = "ERROR Key Mismatch";
                    MessageBoxButtons buttons = MessageBoxButtons.OK;
                    MessageBox.Show(message, caption, buttons);
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
            this.ListViewData.Clear();
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

                    this.textBox3.Visible = false;
                    this.textBox1.Size = new Size(506, 20);
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

                    this.textBox3.Visible = false;
                    this.textBox1.Size = new Size(506, 20);

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

                    this.textBox3.Visible = true;
                    this.textBox1.Size = new Size(250, 20);
                    this.textBox3.Text = logic.BasePath[1];

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

                    this.textBox3.Visible = false;
                    this.textBox1.Size = new Size(506, 20);
                }
            }
            ScrollFolderTextBoxToRight();
        }

        //Load mod data and fill in the list box..
        private void LoadAndFill(bool FromClipboard)
        {
            this.LoadingAndFilling = true;
            KeyValuePair<string, bool> currentEntry = new KeyValuePair<string, bool>();
            try
            {
                if (FromClipboard)
                    logic.LoadStuff2();
                else
                    logic.Loadstuff();

                foreach (KeyValuePair<string, bool> entry in logic.ModList)
                {
                    if (entry.Equals(new KeyValuePair<string, bool>(null, false)))
                        continue;
                    if (entry.Key == null)
                        continue;

                    currentEntry = entry;
                    AddEntryToListViewAndData(entry);
                }

                UpdateListView();
                logic.SaveProgramData();
            }
            catch (Exception e)
            {
                if(currentEntry.Key == null)
                {
                    currentEntry = new KeyValuePair<string, bool>("NULL", false);
                }
                Console.WriteLine(e.StackTrace);
                string message = "While loading " + currentEntry.Key.ToString() + "something went wrong.\n" + e.StackTrace;
                string caption = "Error Loading";
                MessageBoxButtons buttons = MessageBoxButtons.OK;
                MessageBox.Show(message, caption, buttons);
            }
            this.LoadingAndFilling = false;
            logic.CheckRequires(ListViewData);
            logic.GetOverridingData(ListViewData);
        }

        private void AddEntryToListViewAndData(KeyValuePair<string, bool> entry)
        {
            string modName = entry.Key;
            ModItem item1 = new ModItem
            {
                UseItemStyleForSubItems = false,
                Checked = entry.Value
            };
            item1.SubItems.Add(logic.ModDetails[entry.Key].displayName);

            item1.SubItems.Add(logic.PathToDirectoryDict[modName]);
            item1.SubItems.Add(logic.ModDetails[entry.Key].author);
            item1.SubItems.Add(logic.ModDetails[entry.Key].version);
            item1.SubItems.Add(" ");
            item1.EnsureVisible();
            ListViewData.Add(item1);
        }

        //Fill list view from internal list of data.
        private void UpdateListView()
        {
            listView1.Items.Clear();
            listView1.Items.AddRange(ListViewData.ToArray());
        }

        //gets the index of the selected item in listview1.
        private int SelectedItemIndex()
        {
            int index = -1;
            index = listView1.SelectedItems[0].Index;
            if (index < 0)
            {
                return -1;
            }
            return index;
        }

        //Select install directory button
        private void SelectInstallDirectory()
        {
            ClearAll();
            using (var fbd = new FolderBrowserDialog())
            {
                DialogResult result = fbd.ShowDialog();

                if (result == DialogResult.OK && !Utils.StringNullEmptyOrWhiteSpace(fbd.SelectedPath))
                {
                    string path = fbd.SelectedPath;

                    logic.BasePath[0] = path + @"\MW5Mercs\Mods";

                    //We need to do something different for steam cause its special.
                    //Once a switch now an iff.
                    switch (this.logic.Vendor)
                    {
                        case "STEAM":
                            SetSteamWorkshopPath();
                            break;
                        //case "GAMEPASS":
                        //    SetGamepassPath();
                        //    break;
                    }
                    MainForm.textBox1.Text = logic.BasePath[0];
                    MainForm.textBox3.Text = logic.BasePath[1];

                    LoadAndFill(false);

                    ScrollFolderTextBoxToRight();
                }
            }
        }

        private void ScrollFolderTextBoxToRight()
        {
            textBox1.SelectionStart = textBox1.Text.Length;
            textBox1.ScrollToCaret();
            textBox1.Focus();
            textBox3.SelectionStart = textBox1.Text.Length;
            textBox3.ScrollToCaret();
            textBox3.Focus();
        }

        private void SetSteamWorkshopPath()
        {
            //Split by folder depth
            List<string> splitBasePath = this.logic.BasePath[0].Split('\\').ToList<string>();

            //Find the steamapps folder
            int steamAppsIndex = splitBasePath.IndexOf("steamapps");

            //Remove all past the steamapps folder
            splitBasePath.RemoveRange(steamAppsIndex + 1, splitBasePath.Count - steamAppsIndex - 1);

            //Put string back together
            this.logic.BasePath[1] = string.Join("\\", splitBasePath);

            //Point to workshop folder.
            this.logic.BasePath[1] += @"\workshop\content\784080";
        }

        //Refresh listedcheckbox
        private void button6_Click(object sender, EventArgs e)
        {
            ClearAll();
            if (logic.TryLoadProgramData())
            {
                LoadAndFill(false);
                filterBox_TextChanged(null, null);
                logic.GetOverridingData(ListViewData);
                logic.CheckRequires(ListViewData);
            }
        }

        //Image
        private void button8_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start(@"https://www.nexusmods.com/mechwarrior5mercenaries/mods/174?tab=description");
        }

        //Saves current load order to preset.
        private void SavePreset(string name)
        {
            Dictionary<string, bool> NoPathModlist = new Dictionary<string, bool>();
            foreach (KeyValuePair<string, bool> entry in logic.ModList)
            {
                string folderName = logic.PathToDirectoryDict[entry.Key];
                NoPathModlist[folderName] = entry.Value;
            }
            this.logic.Presets[name] = JsonConvert.SerializeObject(NoPathModlist, Formatting.Indented);
            this.logic.SavePresets();
        }

        //Sets up the load order from a preset.
        private void LoadPreset(string name)
        {
            string JsonString = logic.Presets[name];
            Dictionary<string, bool> temp;
            try
            {
                temp = JsonConvert.DeserializeObject<Dictionary<string, bool>>(JsonString);
            }
            catch (Exception Ex)
            {
                string message = "There was an error in decoding the load order string.";
                string caption = "Load Order Decoding Error";
                MessageBoxButtons buttons = MessageBoxButtons.OK;
                MessageBox.Show(message, caption, buttons);
                return;
            }

            this.listView1.Items.Clear();
            this.ListViewData.Clear();
            this.logic.ModDetails = new Dictionary<string, ModObject>();
            this.logic.ModList.Clear();
            this.logic.ModList = temp;
            this.LoadAndFill(true);
            this.filterBox_TextChanged(null, null);
        }

        //Load all presets from file and fill the listbox.
        private void LoadPresets()
        {
            this.logic.LoadPresets();
            foreach (string key in logic.Presets.Keys)
            {
                this.listBox4.Items.Add(key);
            }
        }

        //Export load order
        private void ExportLoadOrderToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Dictionary<string, bool> FolderNameModList = new Dictionary<string, bool>();

            //Get the folder names from the paths in modlist
            foreach (string key in logic.ModList.Keys)
            {
                string folderName = logic.PathToDirectoryDict[key];
                FolderNameModList[folderName] = logic.ModList[key];
            }

            string json = JsonConvert.SerializeObject(FolderNameModList, Formatting.Indented);
            Form3 exportDialog = new Form3();

            // Show testDialog as a modal dialog and determine if DialogResult = OK.
            exportDialog.textBox1.Text = json; //logic.Scramble(json);
            exportDialog.ShowDialog(this);
            exportDialog.Dispose();
        }

        //Import load order
        private void ImportLoadOrderToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Form2 testDialog = new Form2();
            string txtResult = "";

            // Show testDialog as a modal dialog and determine if DialogResult = OK.
            testDialog.ShowDialog(this);
            txtResult = testDialog.textBox1.Text;
            testDialog.Dispose();

            if (Utils.StringNullEmptyOrWhiteSpace(txtResult) ||
                txtResult == "Paste load order clipboard here, any mods that you do not have but are in the pasted load order will be ignored.")
                return;

            Dictionary<string, bool> temp;
            try
            {
                temp = JsonConvert.DeserializeObject<Dictionary<string, bool>>(txtResult);//logic.UnScramble(txtResult));
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
            this.ListViewData.Clear();
            this.logic.ModDetails = new Dictionary<string, ModObject>();
            this.logic.ModList = new Dictionary<string, bool>();
            this.logic.ModList = temp;
            this.LoadAndFill(true);
            this.filterBox_TextChanged(null, null);
        }

        #region Vendor Selection Tool Strip buttons

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
            this.textBox1.Text = logic.BasePath[0];
            this.textBox3.Text = logic.BasePath[1];

            this.textBox3.Visible = true;
            this.textBox1.Size = new Size(250, 20);
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
            this.textBox1.Text = logic.BasePath[0];
            this.MainForm.button5.Enabled = true;

            this.textBox3.Visible = false;
            this.textBox1.Size = new Size(506, 20);
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

            string AppDataRoaming = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

            this.logic.BasePath[0] = GetBasePathFromAppDataRoaming(AppDataRoaming);
            this.logic.CheckModsDir();

            Console.WriteLine("BasePath from AppDataRoaming" + this.logic.BasePath[0]);

            this.textBox1.Text = logic.BasePath[0];
            this.MainForm.button5.Enabled = true;

            this.textBox3.Visible = false;
            this.textBox1.Size = new Size(506, 20);
        }

        private static string GetBasePathFromAppDataRoaming(string AppDataRoaming)
        {
            //Split by folder depth
            List<string> splitBasePath = AppDataRoaming.Split('\\').ToList<string>();

            //Find the steamapps folder
            int AppDataIndex = splitBasePath.IndexOf("AppData");

            //Remove all past the steamapps folder
            splitBasePath.RemoveRange(AppDataIndex + 1, splitBasePath.Count - AppDataIndex - 1);

            //Put string back together
            return string.Join("\\", splitBasePath) + @"\Local\MW5Mercs\Saved\Mods";
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
            this.textBox1.Text = logic.BasePath[0];
            this.MainForm.button5.Enabled = true;

            this.textBox3.Visible = false;
            this.textBox1.Size = new Size(506, 20);
        }
        #endregion

        //Launch game button
        private void button4_Click(object sender, EventArgs e)
        {
            switch (logic.Vendor)
            {
                case "EPIC":
                    LaunchEpicGame();
                    break;
                case "STEAM":
                    LaunchSteamGame();
                    break;
                case "GOG":
                    LaunchGogGame();
                    break;
                case "WINDOWS":
                    LaunchWindowsGame();
                    break;
                case "GAMEPASS":
                    LaunchGamepassGame();
                    break;
            }

        }

        #region Launch Game
        private static void LaunchWindowsGame()
        {
            //Dunno how this works at all.. 
            string message = "This feature is not available in this version.";
            string caption = "Feature not available.";
            MessageBoxButtons buttons = MessageBoxButtons.OK;
            MessageBox.Show(message, caption, buttons);
        }

        private static void LaunchGamepassGame()
        {
            //Dunno how this works at all.. 
            string message = "This feature is not available in this version.";
            string caption = "Feature not available.";
            MessageBoxButtons buttons = MessageBoxButtons.OK;
            MessageBox.Show(message, caption, buttons);
        }

        private void LaunchGogGame()
        {
            string Gamepath = this.logic.BasePath[0];
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

        private static void LaunchEpicGame()
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

        private static void LaunchSteamGame()
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
        #endregion

        //Tool strip for selecting a install folder
        private void selectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SelectInstallDirectory();
        }

        //Open mods folder button
        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            if (Utils.StringNullEmptyOrWhiteSpace(this.logic.BasePath[0]))
            {
                return;
            }
            try
            {
                Process.Start(this.logic.BasePath[0]);
                if (!Utils.StringNullEmptyOrWhiteSpace(this.logic.BasePath[1]))
                {
                    Process.Start(this.logic.BasePath[1]);
                }
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

        //Crude filter because to lazy to add a proper list as backup for the items.
        private void filterBox_TextChanged(object sender, EventArgs e)
        {
            string filtertext = MainForm.filterBox.Text.ToLower();
            if (Utils.StringNullEmptyOrWhiteSpace(filtertext))
            {
                //Console.WriteLine("No filter text");
                if (this.filtered) //we are returning from filtering
                {
                    foreach (ListViewItem x in this.ListViewData)
                    {
                        x.SubItems[1].BackColor = Color.White;
                        x.SubItems[2].BackColor = Color.White;
                        x.SubItems[3].BackColor = Color.White;
                        x.SubItems[4].BackColor = Color.White;
                    }
                    UpdateListView();
                }
                else //We are not returning from a filter
                {
                    //This should never happen. We can't return from a filter we never entered.
                    // do nothing
                }
                MainForm.button1.Enabled = true;
                MainForm.button2.Enabled = true;
                this.filtered = false;
            }
            else
            {
                this.filtered = true;
                //If we are filtering with highlight
                if (MainForm.checkBox1.Checked)
                {
                    //For each item in the list
                    foreach (ListViewItem item in this.ListViewData)
                    {
                        //Check if there is a hit.
                        if (MatchItemToText(filtertext, item))
                        {
                            item.SubItems[1].BackColor = Color.Yellow;
                            item.SubItems[2].BackColor = Color.Yellow;
                            item.SubItems[3].BackColor = Color.Yellow;
                            item.SubItems[4].BackColor = Color.Yellow;
                        }
                        //if not set to white.
                        else
                        {
                            item.SubItems[1].BackColor = Color.White;
                            item.SubItems[2].BackColor = Color.White;
                            item.SubItems[3].BackColor = Color.White;
                            item.SubItems[4].BackColor = Color.White;
                        }
                    }
                }
                //We are filtering by selected adding.
                else
                {
                    //Clear the list view
                    this.listView1.Items.Clear();
                    //For each item check if they are a hit if so add them.
                    foreach (ListViewItem item in this.ListViewData)
                    {
                        if (MatchItemToText(filtertext, item))
                        {
                            item.SubItems[1].BackColor = Color.White;
                            item.SubItems[2].BackColor = Color.White;
                            item.SubItems[3].BackColor = Color.White;
                            item.SubItems[4].BackColor = Color.White;
                            MainForm.listView1.Items.Add(item);
                        }
                    }
                }
                //While filtering dissable the up/down buttons (tough this should no longer be needed).
                MainForm.button1.Enabled = false;
                MainForm.button2.Enabled = false;
            }
        }

        //Check if given listviewitem can be matched to a string.
        private bool MatchItemToText(string filtertext, ListViewItem item)
        {
            if
                (
                    item.SubItems[1].Text.ToLower().StartsWith(filtertext) ||
                    item.SubItems[2].Text.ToLower().StartsWith(filtertext) ||
                    item.SubItems[3].Text.ToLower().StartsWith(filtertext) ||
                    item.SubItems[1].Text.ToLower().Contains(filtertext) ||
                    item.SubItems[2].Text.ToLower().Contains(filtertext) ||
                    item.SubItems[3].Text.ToLower().Contains(filtertext)
                )
            {
                return true;
            }
            return false;
        }

        //Filter or Highlight checkbox on tick action
        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox1.Checked)
            {
                this.listView1.Items.Clear();
                foreach (ModItem item in this.ListViewData)
                {
                    listView1.Items.Add(item);
                }
            }
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
                    logic.ColorItemsOnOverridingData(ListViewData);
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
            if (Utils.StringNullEmptyOrWhiteSpace(selectedMod))
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

            if (Utils.StringNullEmptyOrWhiteSpace(selectedMod))
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

            if (Utils.StringNullEmptyOrWhiteSpace(SelectedMod) ||
                Utils.StringNullEmptyOrWhiteSpace(SelectedModDisplayName)
                )
                return;

            HandleOverrding(SelectedMod);
            HandleDependencies(listView1.SelectedItems[0], SelectedModDisplayName);
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
                ListViewItem item = new ListViewItem
                {
                    Text = "No Dependencies"
                };
                return;
            }

            foreach (string mod in Dependencies)
            {
                ListViewItem item = new ListViewItem
                {
                    Text = mod
                };
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
            if (MovingItem || this.filtered || this.LoadingAndFilling)
            {
                return;
            }

            logic.UpdateNewModOverrideData(ListViewData, ListViewData[e.Item.Index]);
            logic.CheckRequires(ListViewData);
            HandleOverrding(e.Item.SubItems[2].Text);
            HandleDependencies(e.Item, e.Item.SubItems[1].Text);
        }

        //Check for mod overrding data
        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            this.logic.GetOverridingData(ListViewData);
        }

        //Check for mod requirements/dependencies
        private void checkBox3_CheckedChanged(object sender, EventArgs e)
        {
            this.logic.CheckRequires(ListViewData);
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

        #region background workers for zipping up files
        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            // Get the BackgroundWorker that raised this event.
            BackgroundWorker worker = sender as BackgroundWorker;
            this.logic.PackModsToZip(worker, e);
        }

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
                string parent = Directory.GetParent(this.logic.BasePath[0]).ToString();
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
            MainForm.textBox1.Invoke((MethodInvoker)delegate
            {
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
            foreach (ListViewItem item in this.ListViewData)
            {
                item.Checked = true;
            }
            this.MovingItem = false;
            this.logic.GetOverridingData(this.ListViewData);
            this.logic.CheckRequires(this.ListViewData);
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
            this.logic.GetOverridingData(ListViewData);
            this.logic.CheckRequires(ListViewData);
        }

        private void toolStripMenuItem1_Click(object sender, EventArgs e)
        {
            Form5 form5 = new Form5(this);
            form5.ShowDialog(this);
        }

        private void tabPage3_Click(object sender, EventArgs e)
        {

        }

        //Text in the preset save naming text box has changed
        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            this.listBox4.SelectedIndex = -1;
        }

        //Load preset
        private void button11_Click(object sender, EventArgs e)
        {
            if (listBox4.SelectedItem == null)
                return;
            string selected = listBox4.SelectedItem.ToString();
            if (Utils.StringNullEmptyOrWhiteSpace(selected))
                return;
            this.LoadPreset(selected);
        }

        //Save preset
        private void button7_Click(object sender, EventArgs e)
        {
            if (textBox2.Text == null)
                return;

            bool Overriding = false;
            string selected = textBox2.Text;

            if (listBox4.SelectedIndex != -1)
            {
                selected = listBox4.SelectedItem.ToString();
                string message = selected + " selected do you want to override?";
                string caption = "Override?";
                MessageBoxButtons buttons = MessageBoxButtons.YesNo;
                DialogResult result = MessageBox.Show(message, caption, buttons);
                if (result != DialogResult.Yes)
                {
                    return;
                }
                Overriding = true;
            }

            if (Utils.StringNullEmptyOrWhiteSpace(selected))
                return;

            if (this.listBox4.Items.Contains(selected) & !Overriding)
            {
                //No duplicates for your own god damn sake!
                string message = "For your own sake don't save two presets with the same name.";
                string caption = "I'm sorry, Mechwarrior. I'm afraid I can't do that.";
                MessageBoxButtons buttons = MessageBoxButtons.OK;
                MessageBox.Show(message, caption, buttons);
                return;
            }

            this.SavePreset(selected);
            if (!Overriding)
            {
                this.listBox4.Items.Add(selected);
                this.listBox4.SelectedIndex = this.listBox4.Items.Count - 1;
            }
            this.textBox2.Text = "";

        }

        //Delete preset
        private void button12_Click(object sender, EventArgs e)
        {
            if (listBox4.SelectedItem == null)
                return;
            string selected = listBox4.SelectedItem.ToString();
            if (Utils.StringNullEmptyOrWhiteSpace(selected))
                return;
            this.logic.Presets.Remove(selected);
            int index = listBox4.SelectedIndex;
            this.listBox4.Items.RemoveAt(index);
            //where we the only item?
            if (listBox4.Items.Count != 0)
            {
                //No
                //where we the top item?
                if (index == 0)
                {
                    listBox4.SelectedIndex = 0;
                }
                //where we the last item?
                else if (index == listBox4.Items.Count)
                {
                    listBox4.SelectedIndex = listBox4.Items.Count - 1;
                }
                else
                {
                    //there are items above and below us
                    listBox4.SelectedIndex = index;
                }
            }
            this.logic.SavePresets();
        }

        //For unselecting items in listbox4
        void listBox4_OnMouseClick(object sender, MouseEventArgs e)
        {
            int index = this.listBox4.IndexFromPoint(e.Location);
            if (listBox4.SelectedIndex == index)
                listBox4.SelectedIndex = -1;
        }

        //Unused
        private void listBox4_SelectedIndexChanged(object sender, EventArgs e)
        {

        }
    }
}