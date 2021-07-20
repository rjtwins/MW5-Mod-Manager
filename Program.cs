using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.ComponentModel;
using System.Reflection;
using Application = System.Windows.Forms.Application;
using System.Drawing;
using System.IO.Compression;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Threading;

namespace MW5_Mod_Manager
{
    static class Utils
    {
        public static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
        {
            // Get the subdirectories for the specified directory.
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);

            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException(
                    "Source directory does not exist or could not be found: "
                    + sourceDirName);
            }

            DirectoryInfo[] dirs = dir.GetDirectories();

            // If the destination directory doesn't exist, create it.       
            Directory.CreateDirectory(destDirName);

            // Get the files in the directory and copy them to the new location.
            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                string tempPath = Path.Combine(destDirName, file.Name);
                file.CopyTo(tempPath, false);
            }

            // If copying subdirectories, copy them and their contents to new location.
            if (copySubDirs)
            {
                foreach (DirectoryInfo subdir in dirs)
                {
                    string tempPath = Path.Combine(destDirName, subdir.Name);
                    DirectoryCopy(subdir.FullName, tempPath, copySubDirs);
                }
            }
        }

        public static long DirSize(DirectoryInfo d)
        {
            long size = 0;
            // Add file sizes.
            FileInfo[] fis = d.GetFiles();
            foreach (FileInfo fi in fis)
            {
                size += fi.Length;
            }
            // Add subdirectory sizes.
            DirectoryInfo[] dis = d.GetDirectories();
            foreach (DirectoryInfo di in dis)
            {
                size += DirSize(di);
            }
            return size;
        }
    }

    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
            Application.Run(new Form1());
        }

        private static System.Reflection.Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("EmbedAssembly.Newtonsoft.Json.dll"))
            {
                byte[] assemblyData = new byte[stream.Length];
                stream.Read(assemblyData, 0, assemblyData.Length);
                return Assembly.Load(assemblyData);
            }
        }
    }

    public class MainLogic
    {
        public Form1 MainForm;
        public MainLogic Logic;

        public float Version = 0f;
        public string Vendor = "";
        public string BasePath = "";
        public string WorkshopPath = "";
        public ProgramData ProgramData = new ProgramData();

        public JObject parent;
        public string[] Directories;
        public string[] WorkshopDirectories;

        public Dictionary<string, ModObject> ModDetails = new Dictionary<string, ModObject>();
        public Dictionary<string, bool> ModList = new Dictionary<string, bool>();
        public Dictionary<string, OverridingData> OverrridingData = new Dictionary<string, OverridingData>();
        public Dictionary<string, List<string>> MissingModsDependenciesDict = new Dictionary<string, List<string>>();

        public bool CreatedModlist = false;

        public string CurrentFolderInsearch;
        public bool InterruptSearch = false;

        public string rawJson;

        public MainLogic()
        {
            this.Logic = this;
        }

        public void Loadstuff()
        {
            //Check if the Mods directory exits:
            if (!this.CheckModsDir())
                return;

            //find all mod directories and parse them into just folder names:
            ParseDirectories();
            //parse modlist.json
            ModListParser();
            //Combine so we have all mods in the ModList Dict for easy later use and writing to JObject
            CombineDirModList();
            //Load each mods mod.json and store in Dict.
            LoadModDetails();
        }

        public void LoadStuff2()
        {
            //find all mod directories and parse them into just folder names:
            ParseDirectories();
            //Combine so we have all mods in the ModList Dict for easy later use and writing to JObject
            CombineDirModList();
            //Load each mods mod.json and store in Dict.
            LoadModDetails();
        }

        private bool CheckModsDir()
        {
            if (this.BasePath == null || this.BasePath == "" || this.BasePath == " ")
                return false;
            if (!Directory.Exists(this.BasePath))
            {
                string message = "ERROR Mods folder does not exits in : " + this.BasePath + " Do you want to create it?";
                string caption = "ERROR Loading";
                MessageBoxButtons buttons = MessageBoxButtons.YesNo;
                DialogResult Result = MessageBox.Show(message, caption, buttons);
                if(Result == DialogResult.Yes)
                {
                    Directory.CreateDirectory(BasePath);
                }
                return false;
            }
            return true;
        }

        //Try and load data from previous sessions
        public bool TryLoadProgramData()
        {
            //Load install dir from previous session:
            string systemPath = System.Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            string complete = Path.Combine(systemPath, @"MW5LoadOrderManager");
            if (!File.Exists(complete))
            {
                System.IO.Directory.CreateDirectory(complete);
            }

            try
            {
                string json = File.ReadAllText(complete + @"\ProgramData.json");
                this.ProgramData = JsonConvert.DeserializeObject<ProgramData>(json);

                Console.WriteLine("Finshed loading ProgramData.json:" 
                    + " Vendor: " + this.ProgramData.vendor 
                    + " Version: " + this.ProgramData.version 
                    + " Installdir: " + this.ProgramData.installdir);

                if (this.ProgramData.installdir != null && this.ProgramData.installdir != "")
                {
                    this.BasePath = this.ProgramData.installdir;
                }
                if (this.ProgramData.vendor != null && this.ProgramData.vendor != "")
                {
                    this.Vendor = this.ProgramData.vendor;
                }
                if(this.ProgramData.version > 0)
                {
                    this.Version = ProgramData.version;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("ERROR: Something went wrong while loading ProgramData.json");
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
            }

            if (this.BasePath != null && this.BasePath != "")
                return true;
            return false;
        }

        //Delete a mod dir from system.
        internal void DeleteMod(string modDir)
        {
            string directory = BasePath + @"\" + modDir;
            Directory.Delete(directory, true);
        }

        public void WhipeInstallDirMemory()
        {
            try
            {
                this.ProgramData = new ProgramData();
                string systemPath = System.Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
                string complete = Path.Combine(systemPath, @"MW5LoadOrderManager");
                System.IO.File.WriteAllText(complete + @"\ProgramData.json", " ");
            }catch(Exception Ex)
            {
                Console.WriteLine(Ex.Message);
                Console.WriteLine(Ex.StackTrace);
                return;
            }
        }

        public void ParseDirectories()
        {
            this.Directories = Directory.GetDirectories(BasePath);
            this.WorkshopDirectories = null; //instantiated as null for later checks
            for (int i = 0; i < Directories.Length; i++)
            {
                string directory = this.Directories[i];
                string[] temp = directory.Split('\\');
                Directories[i] = temp[temp.Length - 1];
            }
            if (this.Vendor == "STEAM")
            {
                if (WorkshopPath == "")
                {
                    Console.WriteLine("Found Steam version");
                    string workshopPath = BasePath;
                    workshopPath = workshopPath.Remove(workshopPath.Length - 46, 46);
                    Console.WriteLine($"trimmed path is {workshopPath}");
                    workshopPath += ("workshop\\content\\784080");
                    Console.WriteLine($"full workshop path is {workshopPath}");
                    WorkshopPath = workshopPath;
                }
                if (!Directory.Exists(WorkshopPath))
                    return;
                this.WorkshopDirectories = Directory.GetDirectories(WorkshopPath);
                for (int i = 0; i < WorkshopDirectories.Length; i++)
                {
                    string directory = this.WorkshopDirectories[i];
                    string[] temp = directory.Split('\\');
                    WorkshopDirectories[i] = temp[temp.Length - 1];
                }
            }
        }

        public void SaveProgramData()
        {
            this.ProgramData.installdir = this.BasePath;
            this.ProgramData.vendor = this.Vendor;

            string complete = System.Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + @"\MW5LoadOrderManager";
            JsonSerializer serializer = new JsonSerializer();
            serializer.Formatting = Formatting.Indented;
            using (StreamWriter sw = new StreamWriter(complete + @"\ProgramData.json"))
            using (JsonWriter writer = new JsonTextWriter(sw))
            {
                serializer.Serialize(writer, this.ProgramData);
            }
        }

        public void ModListParser()
        {
            try
            {
                this.rawJson = File.ReadAllText(BasePath + @"\modlist.json");
                this.parent = JObject.Parse(rawJson);
            }
            catch (Exception e)
            {
                string message = "ERROR loading modlist.json in : " + this.BasePath + ". It will be created after locating possible mod directories.";
                string caption = "ERROR Loading";
                MessageBoxButtons buttons = MessageBoxButtons.OK;
                DialogResult Result = MessageBox.Show(message, caption, buttons);
                this.CreatedModlist = true;
                return;
            }
            foreach (JProperty mod in this.parent.Value<JObject>("modStatus").Properties())
            {
                bool enabled = (bool)this.parent["modStatus"][mod.Name]["bEnabled"];
                this.ModList.Add(mod.Name, enabled);
            }
        }

        public void SaveToFiles()
        {
            UpdateJObject();
            SaveModDetails();
            SaveModListJson();
        }

        public void ClearAll()
        {
            this.ModDetails = new Dictionary<string, ModObject>();
            this.ModList = new Dictionary<string, bool>();
            this.ProgramData = new ProgramData();
            this.BasePath = "";
            this.WorkshopPath = "";
        }

        //Check if the mod dir is already present in data loaded from modlist.json, if not add it.
        public void CombineDirModList()
        {
            foreach (string modDir in this.Directories)
            {
                if (this.ModList.ContainsKey(modDir))
                    continue;

                ModList.Add(modDir, false);
            }
            if (this.Vendor == "STEAM" && this.WorkshopDirectories != null)
                foreach (string modDir in this.WorkshopDirectories)
                {
                    if (this.ModList.ContainsKey(modDir))
                        continue;

                    ModList.Add(modDir, false);
                }

            //Turns out there are sometimes "ghost" entries in the modlist.json for witch there are no directories left, lets remove those.
            List<string> toRemove = new List<string>();
            foreach (KeyValuePair<string, bool> entry in this.ModList)
            {
                if (this.Directories.Contains<string>(entry.Key))
                    continue;
                else if (this.Vendor == "STEAM" && this.WorkshopDirectories != null)
                    if (this.WorkshopDirectories.Contains<string>(entry.Key))
                        continue;
                toRemove.Add(entry.Key);
            }
            foreach (string key in toRemove)
            {
                this.ModList.Remove(key);
            }
            if (this.CreatedModlist)
            {
                UpdateJObject();
                SaveModListJson(); 
            }
        }

        public void LoadModDetails()
        {
            foreach (string modDir in this.Directories)
            {
                try
                {
                    string modJson = File.ReadAllText(BasePath + @"\" + modDir + @"\mod.json");
                    ModObject mod = JsonConvert.DeserializeObject<ModObject>(modJson);
                    this.ModDetails.Add(modDir, mod);
                }
                catch (Exception e)
                {
                    string message = "ERROR loading mod.json in : " + modDir + 
                        " folder will be skipped. " +
                        " If this is not a mod folder you can ignore ths message.";
                    string caption = "ERROR Loading";
                    MessageBoxButtons buttons = MessageBoxButtons.OK;
                    MessageBox.Show(message, caption, buttons);

                    if (ModList.ContainsKey(modDir))
                    {
                        ModList.Remove(modDir);
                    }
                    if (ModDetails.ContainsKey(modDir))
                    {
                        ModDetails.Remove(modDir);
                    }
                }
            }
            if (this.Vendor == "STEAM" && this.WorkshopDirectories != null)
                foreach (string modDir in this.WorkshopDirectories)
                {
                    try
                    {
                        string modJson = File.ReadAllText(WorkshopPath + @"\" + modDir + @"\mod.json");
                        ModObject mod = JsonConvert.DeserializeObject<ModObject>(modJson);
                        this.ModDetails.Add(modDir, mod);
                    }
                    catch (Exception e)
                    {
                        string message = "ERROR loading mod.json in : " + modDir +
                            " folder will be skipped. " +
                            " If this is not a mod folder you can ignore ths message.";
                        string caption = "ERROR Loading";
                        MessageBoxButtons buttons = MessageBoxButtons.OK;
                        MessageBox.Show(message, caption, buttons);

                        if (ModList.ContainsKey(modDir))
                        {
                            ModList.Remove(modDir);
                        }
                        if (ModDetails.ContainsKey(modDir))
                        {
                            ModDetails.Remove(modDir);
                        }
                    }
                }         
        }

        public void SaveModDetails()
        {
            foreach (KeyValuePair<string, ModObject> entry in this.ModDetails)
            {
                string modJsonPath = BasePath + @"\" + entry.Key + @"\mod.json";
                if (this.Vendor == "STEAM" && this.WorkshopDirectories != null)
                    if (this.WorkshopDirectories.Contains(entry.Key))
                        modJsonPath = WorkshopPath + @"\" + entry.Key + @"\mod.json";

                JsonSerializer serializer = new JsonSerializer();
                serializer.Formatting = Formatting.Indented;
                using (StreamWriter sw = new StreamWriter(modJsonPath))
                using (JsonWriter writer = new JsonTextWriter(sw))
                {
                    serializer.Serialize(writer, entry.Value);
                }
            }
        }

        public void UpdateJObject()
        {
            if(this.parent == null)
            {
                this.parent = new JObject();
                this.parent.Add("modStatus", JObject.Parse(@"{}"));
            }
            this.parent.Value<JObject>("modStatus").RemoveAll();
            foreach (KeyValuePair<string, bool> entry in this.ModList)
            {
                AddModToJObject(entry.Key, entry.Value);
            }
        }

        public void SaveModListJson()
        {
            string jsonString = this.parent.ToString();
            StreamWriter sw = File.CreateText(BasePath + @"\modlist.json");
            sw.WriteLine(jsonString);
            sw.Flush();
            sw.Close();
        }

        public void AddModToJObject(string ModName, bool status)
        {
            //ugly but I'm lazy today
            if (status)
            {
                (this.parent["modStatus"] as JObject).Add(ModName, JObject.Parse(@"{""bEnabled"": true}"));
            }
            else
            {
                (this.parent["modStatus"] as JObject).Add(ModName, JObject.Parse(@"{""bEnabled"": false}"));
            }
        }

        public void SetModInJObject(string ModName, bool status)
        {
            this.parent["modStatus"][ModName]["bEnabled"] = status;
        }

        public string Findfile(string folder, string fname, BackgroundWorker worker, DoWorkEventArgs e)
        {
            foreach (string newFolder in Directory.GetDirectories(folder))
            {
                this.CurrentFolderInsearch = newFolder;
                worker.ReportProgress(0, newFolder);

                if (worker.CancellationPending)
                {
                    e.Cancel = true;
                    return "";
                }
                try
                {
                    string found = Findfile(newFolder, fname, worker, e);
                    if (found != null && found != "")
                    {
                        return found;
                    }
                }
                catch (Exception ex)
                {
                }
            }
            if (File.Exists(folder + @"\" + fname) == true)
            {
                try
                {
                    //ExeFilePath = folder;
                    return folder;
                }
                catch (Exception ex)
                {
                }
            }
            return "";

        }

        public void ThreadProc()
        {
            //Get parent dir
            string parent = Directory.GetParent(Logic.BasePath).ToString();

            //Check if Mods.zip allready exists delete it if so, we need to do this else the ZipFile lib will error.
            if (File.Exists(parent + "\\Mods.zip"))
            {
                File.Delete(parent + "\\Mods.zip");
            }
            ZipFile.CreateFromDirectory(this.BasePath, parent + "\\Mods.zip", CompressionLevel.Fastest, false);
        }

        public void PackModsToZip(BackgroundWorker worker, DoWorkEventArgs e)
        {
            string parent = Directory.GetParent(Logic.BasePath).ToString();

            Thread t = new Thread(new ThreadStart(ThreadProc));
            t.Start();
            while (t.IsAlive)
            {
                System.Threading.Thread.Sleep(500);
                if(worker.CancellationPending || e.Cancel)
                {
                    t.Abort();
                    t.Join();
                    e.Result = "ABORTED";
                    if (File.Exists(parent + "\\Mods.zip"))
                    {
                        File.Delete(parent + "\\Mods.zip");
                    }
                    return;
                }
            }
            //Open folder where we stored the zip file
            e.Result = "DONE";
            Process.Start(parent);
        }

        /* unused
        public string Scramble(string input)
        {
            input = input.Replace(" ", "");
            input = input.Replace("\n", "");
            input = input.Replace("\t", "");
            char[] chars = input.ToArray();
            Random r = new Random(69); //he he he...
            for (int i = 0; i < chars.Length; i++)
            {
                int randomIndex = r.Next(0, chars.Length);
                char temp = chars[randomIndex];
                chars[randomIndex] = chars[i];
                chars[i] = temp;
            }
            string scrambled = new string(chars);
            Console.WriteLine(scrambled);
            return scrambled;
        }

        public string UnScramble(string scrambled)
        {
            Random r = new Random(69);
            char[] scramChars = scrambled.ToArray();
            List<int> swaps = new List<int>();
            for (int i = 0; i < scramChars.Length; i++)
            {
                swaps.Add(r.Next(0, scramChars.Length));
            }
            for (int i = scramChars.Length - 1; i >= 0; i--)
            {
                char temp = scramChars[swaps[i]];
                scramChars[swaps[i]] = scramChars[i];
                scramChars[i] = temp;
            }
            string unscrambled = new string(scramChars);
            Console.WriteLine(unscrambled);
            return unscrambled;
        }
        */

        //Searched first when looking for file names
        public List<string> CommonFolders = new List<string>()
        {
            @"games\",
            @"Program Files\",
            @"Program Files (x86)\",
            @"Epic\",
            @"EpicGames\",
            @"Epic Games\",
            @"Epic_Games\"
        };

        //Return a dict of all overriden mods with a list of overriden files as values.
        //else returns an empty string.
        public void GetOverridingData(ListView.ListViewItemCollection items)
        {
            //Console.WriteLine("Starting Overriding data check");
            this.OverrridingData.Clear();
            foreach (ListViewItem item in items)
            {
                //We only wanna check this for items actually enabled.
                if (!item.Checked)
                    continue;

                string modA = item.SubItems[2].Text;
                int priorityA = items.Count - item.Index;
                OverridingData A = new OverridingData();
                A.mod = modA;
                A.overrides = new Dictionary<string, List<string>>();
                A.overriddenBy = new Dictionary<string, List<string>>();

                Console.WriteLine("Checking: " + modA + " : " + priorityA.ToString());

                foreach (ListViewItem itemb in items)
                {
                    string modB = itemb.SubItems[2].Text;
                    int priorityB = items.Count - itemb.Index;

                    //we dont need to know of a mod overrides itself..
                    if (modB == modA)
                        continue;

                    //if not active we don't care if its beeing overriden.
                    if (!item.Checked)
                        continue;

                    //Now we have a mod that is not the mod we are looking at is enbabled.
                    //Lets compare the manifest!
                    List<string> manifestA = this.ModDetails[modA].manifest;
                    List<string> manifestB = this.ModDetails[modB].manifest;
                    List<string> intersect = manifestA.Intersect(manifestB).ToList();

                    //If the intersects elements are greater then zero we have shared parts of the manifest
                    if (intersect.Count() == 0)
                        continue;

                    Console.WriteLine("---Intersection: " + modB + " : " + priorityB.ToString());

                    //If we are loaded after the mod we are looking at we are overriding it.
                    if (priorityA > priorityB)
                    {
                        A.isOverriding = true;
                        A.overrides[modB] = intersect;
                    }
                    else
                    {
                        A.isOverriden = true;
                        A.overriddenBy[modB] = intersect;
                    }
                }
                this.OverrridingData[modA] = A;
                if (A.isOverriden)
                    item.SubItems[1].ForeColor = Color.OrangeRed;
                if (A.isOverriding)
                    item.SubItems[1].ForeColor = Color.Green;
                if (A.isOverriding && A.isOverriden)
                    item.SubItems[1].ForeColor = Color.Orange;
            }
        }

        //Check for all active mods in list provided if the mods in the required section are also active.
        public Dictionary<string, List<string>> CheckRequires (ListView.ListViewItemCollection items)
        {
            Console.WriteLine("Checking mods Requires");
            this.MissingModsDependenciesDict = new Dictionary<string, List<string>>();

            //For each mod check if their requires list is a sub list of the active mods list... aka see if the required mods are active.
            foreach(ListViewItem item in items)
            {
                if (!item.Checked)
                    continue;

                string modDisplayName = item.SubItems[1].Text;
                string modFolderName = item.SubItems[2].Text;

                if (ModDetails[modFolderName].Requires == null)
                {
                    item.SubItems[5].BackColor = Color.Green;
                    continue;
                }

                Console.WriteLine(item.SubItems[1].Text);
                Console.WriteLine("List of Requires");
                foreach (string mod in ModDetails[modFolderName].Requires)
                {
                    Console.WriteLine("--" + mod);
                }

                List<string> Requires = ModDetails[modFolderName].Requires;
                List<string> activeMods = new List<string>();

                foreach (ListViewItem itemB in items)
                {
                    if (!itemB.Checked)
                        continue;
                    if (!(itemB.Index > item.Index))
                        continue;
                    //Console.WriteLine(itemB.SubItems[1].Text);
                    activeMods.Add(itemB.SubItems[1].Text);
                }
                
                //Make a list of all mods we need but are not in the active mods.
                List<string> missingMods = Requires.Except(activeMods).ToList<string>();

                if (missingMods.Count == 0)
                {
                    Console.WriteLine("All subset items found!");
                    item.SubItems[5].BackColor = Color.Green;
                    continue;
                }
                Console.WriteLine("Not all subset items found!");
                item.SubItems[5].BackColor = Color.Red;
                MissingModsDependenciesDict[modDisplayName] = missingMods;
            }
            return MissingModsDependenciesDict;
        }

        //Get display names of all dependencies of given mod.
        public List<string> GetModDependencies(string selectedMod)
        {
            return ModDetails[selectedMod].Requires;
        }

        //Monitor the size of a given zip file
        public void MonitorZipSize(BackgroundWorker worker, DoWorkEventArgs e)
        {
            string zipFile = Directory.GetParent(this.BasePath).ToString() + "\\Mods.zip";
            long folderSize = Utils.DirSize(new DirectoryInfo(BasePath));
            //zip usually does about 60 percent but we dont wanna complete at like 85 or 90 lets overestimate
            long compressedFolderSize = (long)Math.Round(folderSize * 0.35);
            Console.WriteLine("Starting file size monitor, FolderSize: " + compressedFolderSize.ToString());
            while (!e.Cancel && !worker.CancellationPending)
            {
                long zipFileSize = new FileInfo(zipFile).Length;
                int progress = Math.Min((int)((zipFileSize * (long)100) / compressedFolderSize ), 100);
                Console.WriteLine("--" + zipFileSize.ToString());
                Console.WriteLine("--" + progress.ToString());
                worker.ReportProgress(progress);
                System.Threading.Thread.Sleep(500);
            }

        }
    }

    public class ModObject
    {
        public string displayName { set; get; }
        public string version { set; get; }
        public int buildNumber { set; get; }
        public string description { set; get; }
        public string author { set; get; }
        public string authorURL { set; get; }
        public float defaultLoadOrder { set; get; }
        public string gameVersion { set; get; }
        public List<string> manifest { get; set; }
        public long steamPublishedFileId { set; get; }
        public long steamLastSubmittedBuildNumber { set; get; }
        public string steamModVisibility { set; get; }
        public List<string> Requires { set; get; }
    }

    public class ProgramData
    {
        public string vendor { set; get; }
        public float version { set; get; }
        public string installdir { set; get; }
    }

    public class OverridingData
    {
        public string mod { set; get; }
        public bool isOverriden { set; get; }
        public bool isOverriding { set; get; }
        public Dictionary<string, List<string>> overrides { set; get; }
        public Dictionary<string, List<string>> overriddenBy { set; get; }
    }
}
