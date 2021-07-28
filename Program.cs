using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PostSharp.Community.Packer;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using Application = System.Windows.Forms.Application;

[assembly: Packer]
namespace MW5_Mod_Manager
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }
    }

    /// <summary>
    /// Contains most of the background logic and operations
    /// Also has some dataobjects to keep track of various interal statuses.
    /// </summary>
    public class MainLogic
    {
        public Form1 MainForm;

        public float Version = 0f;
        public string Vendor = "";
        public string[] BasePath = new string[2];
        public ProgramData ProgramData = new ProgramData();

        public JObject parent;
        public List<string> Directories = new List<string>();
        public Dictionary<string, string> DirectoryToPathDict = new Dictionary<string, string>();
        public Dictionary<string, string> PathToDirectoryDict = new Dictionary<string, string>();

        public Dictionary<string, ModObject> ModDetails = new Dictionary<string, ModObject>();
        public Dictionary<string, bool> ModList = new Dictionary<string, bool>();
        public Dictionary<string, OverridingData> OverrridingData = new Dictionary<string, OverridingData>();
        public Dictionary<string, List<string>> MissingModsDependenciesDict = new Dictionary<string, List<string>>();
        public Dictionary<string, string> Presets = new Dictionary<string, string>();

        public bool CreatedModlist = false;

        public bool InterruptSearch = false;

        public string rawJson;

        /// <summary>
        /// Starts suquence to load all mods from folders, loads modlist, combines modlist with found folders structure
        /// and loads details of each found mod.
        /// </summary>
        public void LoadFromFiles()
        {
            //Check if the Mods directory exits:
            CheckModsDir();
            //find all mod directories and parse them into just folder names:
            ParseDirectories();
            //parse modlist.json
            ModListParser();
            //Combine so we have all mods in the ModList Dict for easy later use and writing to JObject
            CombineDirModList();
            //Load each mods mod.json and store in Dict.
            LoadModDetails();
        }

        /// <summary>
        /// Used to load mods when using a preset or importing a load order string.
        /// Starts suquence to load all mods from folders, loads modlist, checks mod folder names against their possible paths
        /// and adds those paths, combines modlist with found folders structure and loads details of each found mod.
        /// </summary>
        public void LoadFromImportString()
        {
            //find all mod directories and parse them into just folder names:
            ParseDirectories();
            //We need to check if the mod we wanna load from a preset is actually present on the system.
            CheckModDirPresent();
            //We are coming from an string of just modfolder names and no directory paths in the modlist object
            //so we need to convert using the DirectoryToPathDict
            AddPathsToModList();
            //Combine so we have all mods in the ModList Dict for easy later use and writing to JObject
            CombineDirModList();
            //Load each mods mod.json and store in Dict.
            LoadModDetails();
        }

        /// <summary>
        /// Checks for all items in the modlist if they have a possible folder on system they can point to.
        /// If not removes them from the modlist and imforms user.
        /// </summary>
        private void CheckModDirPresent()
        {
            List<string> MissingModDirs = new List<string>();
            foreach (string key in this.ModList.Keys)
            {
                if (Utils.StringNullEmptyOrWhiteSpace(key))
                {
                    ModList.Remove(key);
                    continue;
                }
                //If the folder that this mod needs is not present warn user and remove.
                if (!DirectoryToPathDict.ContainsKey(key))
                {
                    MissingModDirs.Add(key);
                }
            }
            foreach (string key in MissingModDirs)
            {
                this.ModList.Remove(key);
            }
            if (MissingModDirs.Count > 0)
            {
                string message = "ERROR Mods folder not found for the following mods:\n"
                    + string.Join("\n", MissingModDirs)
                    + "\nThese mods will skipped.";
                string caption = "ERROR Finding Mod Directories";
                MessageBoxButtons buttons = MessageBoxButtons.OK;
                MessageBox.Show(message, caption, buttons);
            }
        }

        /// <summary>
        /// Matches each folder name in the modlist to a folder on system.
        /// Then replaces the old modlist with a new one keyed by full folder path.
        /// </summary>
        private void AddPathsToModList()
        {
            Dictionary<string, bool> newModList = new Dictionary<string, bool>();
            foreach (string key in this.ModList.Keys)
            {
                string fullPath = DirectoryToPathDict[key];
                newModList[fullPath] = ModList[key];
            }
            this.ModList = newModList;
        }

        //TODO Write summary
        /// <summary>
        /// Checks if the set mods directory exists, if not creates one.
        /// </summary>
        /// <returns></returns>
        public void CheckModsDir()
        {
            CheckInstalDirModsDir();
            CheckSteamDirModsDir();
        }

        private void CheckSteamDirModsDir()
        {
            if (Utils.StringNullEmptyOrWhiteSpace(this.BasePath[1]))
            {
                return;
            }
            if (Directory.Exists(this.BasePath[1]))
            {
                return;
            }
            string message = "ERROR Mods folder does not exits in : " + this.BasePath[0] + " Do you want to create it?";
            string caption = "ERROR Loading";
            MessageBoxButtons buttons = MessageBoxButtons.YesNo;
            DialogResult Result = MessageBox.Show(message, caption, buttons);
            if (Result == DialogResult.Yes)
            {
                Directory.CreateDirectory(BasePath[1]);
            }
        }

        private void CheckInstalDirModsDir()
        {
            if (Utils.StringNullEmptyOrWhiteSpace(this.BasePath[0]))
            {
                return;
            }
            if (Directory.Exists(this.BasePath[0]))
            {
                return;
            }
            string message = "ERROR Mods folder does not exits in : " + this.BasePath[0] + " Do you want to create it?";
            string caption = "ERROR Loading";
            MessageBoxButtons buttons = MessageBoxButtons.YesNo;
            DialogResult Result = MessageBox.Show(message, caption, buttons);
            if (Result == DialogResult.Yes)
            {
                Directory.CreateDirectory(BasePath[0]);
            }
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

                if (!Utils.StringNullEmptyOrWhiteSpace(this.ProgramData.installdir[0]))
                {
                    this.BasePath[0] = this.ProgramData.installdir[0];
                }
                if (!Utils.StringNullEmptyOrWhiteSpace(this.ProgramData.installdir[1]))
                {
                    this.BasePath[1] = this.ProgramData.installdir[1];
                }
                if (!Utils.StringNullEmptyOrWhiteSpace(this.ProgramData.vendor))
                {
                    this.Vendor = this.ProgramData.vendor;
                }
                if (this.ProgramData.version > 0)
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

            if (this.BasePath[0] != null && this.BasePath[0] != "")
                return true;
            return false;
        }

        //Delete a mod dir from system.
        internal void DeleteMod(string modDir)
        {
            string directory = modDir;
            Directory.Delete(directory, true);
        }

        public void WhipeInstallDirMemory()
        {
            try
            {
                this.ProgramData = new ProgramData();
                string systemPath = System.Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
                string complete = Path.Combine(systemPath, @"MW5LoadOrderManager");
                System.IO.File.WriteAllText(complete + @"\ProgramData.json", "");
            }
            catch (Exception Ex)
            {
                //Console.WriteLine(Ex.Message);
                //Console.WriteLine(Ex.StackTrace);
                return;
            }
        }

        //parse all directories in the basepath mods folder or steam workshop mods folder.
        private void ParseDirectories()
        {
            this.Directories.Clear();

            //Check if basepath is there
            if (BasePath == null)
                return;

            HandleInstalDirDirectories();

            HandleSteamDirectories();

            AddDirectoryPathsToDict();
        }

        private void AddDirectoryPathsToDict()
        {
            for (int i = 0; i < Directories.Count; i++)
            {
                string directory = this.Directories[i];
                Directories[i] = directory;

                //We wanna keep a dict of the directory name pointing to its path because later on we want to look up the directory
                //path based on just a folder name from the mods.json.
                string[] temp = directory.Split('\\');
                string directoryName = temp[temp.Length - 1];
                this.DirectoryToPathDict[directoryName] = directory;
                this.PathToDirectoryDict[directory] = directoryName;
            }
        }

        private void HandleInstalDirDirectories()
        {
            if (Utils.StringNullEmptyOrWhiteSpace(BasePath[0]))
            {
                return;
            }
            this.Directories.AddRange(Directory.GetDirectories(BasePath[0]));
        }

        private void HandleSteamDirectories()
        {
            if (Utils.StringNullEmptyOrWhiteSpace(BasePath[1]))
            {
                return;
            }
            this.Directories.AddRange(Directory.GetDirectories(BasePath[1]));
        }

        public void SaveProgramData()
        {
            this.ProgramData.installdir = this.BasePath;
            this.ProgramData.vendor = this.Vendor;

            string complete = System.Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + @"\MW5LoadOrderManager";
            JsonSerializer serializer = new JsonSerializer
            {
                Formatting = Formatting.Indented
            };
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
                this.rawJson = File.ReadAllText(BasePath[0] + @"\modlist.json");
                this.parent = JObject.Parse(rawJson);
            }
            catch (Exception e)
            {
                string message = "ERROR loading modlist.json in : " + this.BasePath[0] + ". It will be created after locating possible mod directories.";
                string caption = "ERROR Loading";
                MessageBoxButtons buttons = MessageBoxButtons.OK;
                DialogResult Result = MessageBox.Show(message, caption, buttons);
                this.CreatedModlist = true;
                return;
            }
            foreach (JProperty mod in this.parent.Value<JObject>("modStatus").Properties())
            {
                bool enabled = (bool)this.parent["modStatus"][mod.Name]["bEnabled"];
                if (this.DirectoryToPathDict.TryGetValue(mod.Name, out string modDir))
                {
                    this.ModList.Add(modDir, enabled);
                }
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
            this.DirectoryToPathDict = new Dictionary<string, string>();
            this.OverrridingData = new Dictionary<string, OverridingData>();
            this.MissingModsDependenciesDict = new Dictionary<string, List<string>>();
            this.BasePath = new string[2];
        }

        //Check if the mod dir is already present in data loaded from modlist.json, if not add it.
        private void CombineDirModList()
        {
            foreach (string modDir in this.Directories)
            {
                if (this.ModList.ContainsKey(modDir))
                    continue;

                ModList[modDir] = false;
            }
            //Turns out there are sometimes "ghost" entries in the modlist.json for witch there are no directories left, lets remove those.
            List<string> toRemove = new List<string>();
            foreach (KeyValuePair<string, bool> entry in this.ModList)
            {
                if (this.Directories.Contains<string>(entry.Key))
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

        private void LoadModDetails()
        {
            foreach (string modDir in this.Directories)
            {
                try
                {
                    //string modJson = File.ReadAllText(BasePath + @"\" + modDir + @"\mod.json");
                    string modJson = File.ReadAllText(modDir + @"\mod.json");
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
                string modJsonPath = entry.Key + @"\mod.json";
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
            if (this.parent == null)
            {
                this.parent = new JObject();
                this.parent.Add("modStatus", JObject.Parse(@"{}"));
            }
            this.parent.Value<JObject>("modStatus").RemoveAll();
            foreach (KeyValuePair<string, bool> entry in this.ModList)
            {
                string[] temp = entry.Key.Split('\\');
                string modFolderName = temp[temp.Length - 1];
                AddModToJObject(modFolderName, entry.Value);
            }
        }

        public void SaveModListJson()
        {
            string jsonString = this.parent.ToString();
            StreamWriter sw = File.CreateText(BasePath[0] + @"\modlist.json");
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

        #region pack mods to zip

        public void ThreadProc()
        {
            //Get parent dir
            string parent = Directory.GetParent(this.BasePath[0]).ToString();
            //Check if Mods.zip allready exists delete it if so, we need to do this else the ZipFile lib will error.
            if (File.Exists(parent + "\\Mods.zip"))
            {
                File.Delete(parent + "\\Mods.zip");
            }
            ZipFile.CreateFromDirectory(this.BasePath[0], parent + "\\Mods.zip", CompressionLevel.Fastest, false);
        }

        public void PackModsToZip(BackgroundWorker worker, DoWorkEventArgs e)
        {
            //Console.WriteLine("Starting zip compression");
            string parent = Directory.GetParent(this.BasePath[0]).ToString();

            Thread t = new Thread(new ThreadStart(ThreadProc));
            t.Start();
            while (t.IsAlive)
            {
                System.Threading.Thread.Sleep(500);
                if (worker.CancellationPending || e.Cancel)
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
                Thread.Yield();
            }
            //Open folder where we stored the zip file
            e.Result = "DONE";
        }

        #endregion pack mods to zip

        //Reset the orriding data between two mods and check if after mods are still overriding/beeing overrriden
        public void ResetOverrdingBetweenMods(ModItem itemA, ModItem itemB)
        {
            string modA = itemA.SubItems[2].Text;
            string modB = itemB.SubItems[2].Text;

            if (this.OverrridingData.ContainsKey(modA))
            {
                if (this.OverrridingData[modA].overriddenBy.ContainsKey(modB))
                    this.OverrridingData[modA].overriddenBy.Remove(modB);
                if (this.OverrridingData[modA].overrides.ContainsKey(modB))
                    this.OverrridingData[modA].overrides.Remove(modB);
                if (this.OverrridingData[modA].overrides.Count == 0)
                    this.OverrridingData[modA].isOverriding = false;
                if (this.OverrridingData[modA].overriddenBy.Count == 0)
                    this.OverrridingData[modA].isOverriden = false;
            }
            if (this.OverrridingData.ContainsKey(modA))
            {
                if (this.OverrridingData[modB].overriddenBy.ContainsKey(modA))
                    this.OverrridingData[modB].overriddenBy.Remove(modA);
                if (this.OverrridingData[modB].overrides.ContainsKey(modA))
                    this.OverrridingData[modB].overrides.Remove(modA);
                if (this.OverrridingData[modB].overrides.Count == 0)
                    this.OverrridingData[modB].isOverriding = false;
                if (this.OverrridingData[modB].overriddenBy.Count == 0)
                    this.OverrridingData[modB].isOverriden = false;
            }
            //Console.WriteLine("ResetOverrdingBetweenMods modA: " + modA + " " + this.OverrridingData[modA].isOverriding + " " + this.OverrridingData[modA].isOverriden);
            //Console.WriteLine("ResetOverrdingBetweenMods modB: " + modB + " " + this.OverrridingData[modB].isOverriding + " " + this.OverrridingData[modB].isOverriden);
        }

        //Save presets from memory to file for use in next session.
        internal void SavePresets()
        {
            string JsonFile = System.Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + @"\MW5LoadOrderManager\presets.json";
            string JsonString = JsonConvert.SerializeObject(this.Presets, Formatting.Indented);

            if (File.Exists(JsonFile))
                File.Delete(JsonFile);

            //Console.WriteLine(JsonString);
            StreamWriter sw = File.CreateText(JsonFile);
            sw.WriteLine(JsonString);
            sw.Flush();
            sw.Close();
        }

        //Load prests from file
        public void LoadPresets()
        {
            string JsonFile = System.Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + @"\MW5LoadOrderManager\presets.json";
            //parse to dict of strings.

            if (!File.Exists(JsonFile))
                return;

            Dictionary<string, string> temp;
            try
            {
                string json = File.ReadAllText(JsonFile);
                temp = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
                //Console.WriteLine("OUTPUT HERE!");
                //Console.WriteLine(JsonConvert.SerializeObject(temp, Formatting.Indented));
            }
            catch (Exception Ex)
            {
                string message = "There was an error in decoding the presets file!";
                string caption = "Presets File Decoding Error";
                MessageBoxButtons buttons = MessageBoxButtons.OK;
                MessageBox.Show(message, caption, buttons);
                return;
            }
            this.Presets = temp;
        }

        //Used to update the override data when a new item is added or removed to/from the mod list instead of checking all items agains each other again.
        public void UpdateNewModOverrideData(List<ModItem> items, ModItem newItem)
        {
            string modA = newItem.SubItems[2].Text;
            ////Console.WriteLine("UpdateNewModOverrideData");
            ////Console.WriteLine("Mod checked or unchecked: " + modA);

            if (!newItem.Checked)
            {
                ////Console.WriteLine("--Unchecked");
                if (this.OverrridingData.ContainsKey(modA))
                    this.OverrridingData.Remove(modA);

                foreach (string key in this.OverrridingData.Keys)
                {
                    if (OverrridingData[key].overriddenBy.ContainsKey(modA))
                        OverrridingData[key].overriddenBy.Remove(modA);

                    if (OverrridingData[key].overrides.ContainsKey(modA))
                        OverrridingData[key].overrides.Remove(modA);

                    if (OverrridingData[key].overrides.Count == 0)
                        OverrridingData[key].isOverriding = false;

                    if (OverrridingData[key].overriddenBy.Count == 0)
                        OverrridingData[key].isOverriden = false;
                }
            }
            else
            {
                ////Console.WriteLine("--Unchecked");
                if (!this.OverrridingData.ContainsKey(modA))
                {
                    this.OverrridingData[modA] = new OverridingData
                    {
                        mod = modA,
                        overrides = new Dictionary<string, List<string>>(),
                        overriddenBy = new Dictionary<string, List<string>>()
                    };
                }

                //check each mod for changes
                foreach (ModItem item in items)
                {
                    string modB = item.SubItems[2].Text;

                    //Again dont compare mods to themselves.
                    if (modA == modB)
                        continue;

                    if (!this.OverrridingData.ContainsKey(modB))
                    {
                        this.OverrridingData[modB] = new OverridingData
                        {
                            mod = modB,
                            overrides = new Dictionary<string, List<string>>(),
                            overriddenBy = new Dictionary<string, List<string>>()
                        };
                    }
                    GetModOverridingData(newItem, item, items.Count, this.OverrridingData[modA], this.OverrridingData[modB]);
                }
            }

            ColorItemsOnOverridingData(items);
        }

        //used to update the overriding data when a mod is moved ONE up or ONE down.
        public void UpdateModOverridingdata(List<ModItem> items, ModItem movedMod, bool movedUp)
        {
            string modA = movedMod.SubItems[2].Text;

            //Console.WriteLine("UpdateModOverridingdata");
            //Console.WriteLine("--" + modA);

            int indexToCheck = 0;
            if (movedUp)
                indexToCheck = movedMod.Index + 1;
            else
                indexToCheck = movedMod.Index - 1;

            ModItem itemB = items[indexToCheck];
            string modB = itemB.SubItems[2].Text;
            //Console.WriteLine("++" + modB);

            if (!this.OverrridingData.ContainsKey(modA))
            {
                this.OverrridingData[modA] = new OverridingData
                {
                    mod = modA,
                    overrides = new Dictionary<string, List<string>>(),
                    overriddenBy = new Dictionary<string, List<string>>()
                };
            }
            if (!this.OverrridingData.ContainsKey(modB))
            {
                this.OverrridingData[modB] = new OverridingData
                {
                    mod = modB,
                    overrides = new Dictionary<string, List<string>>(),
                    overriddenBy = new Dictionary<string, List<string>>()
                };
            }

            ResetOverrdingBetweenMods(movedMod, itemB);

            GetModOverridingData(movedMod, items[indexToCheck], items.Count, OverrridingData[modA], OverrridingData[modA]);

            OverridingData A = OverrridingData[modA];
            OverridingData B = OverrridingData[modB];

            ColorItemsOnOverridingData(items);
        }

        //See if items A and B are interacting in terms of manifest and return the intersect
        public void GetModOverridingData(ModItem itemA, ModItem itemB, int itemCount, OverridingData A, OverridingData B)
        {
            string modA = itemA.SubItems[2].Text;
            string modB = itemB.SubItems[2].Text;

            if (modA == modB)
                return;

            int priorityA = itemCount - itemA.Index;
            int priorityB = itemCount - itemB.Index;

            //Now we have a mod that is not the mod we are looking at is enbabled.
            //Lets compare the manifest!
            List<string> manifestA = this.ModDetails[this.DirectoryToPathDict[modA]].manifest;
            List<string> manifestB = this.ModDetails[this.DirectoryToPathDict[modB]].manifest;
            List<string> intersect = manifestA.Intersect(manifestB).ToList();

            //If the intersects elements are greater then zero we have shared parts of the manifest
            if (intersect.Count() == 0)
                return;

            ////Console.WriteLine("---Intersection: " + modB + " : " + priorityB.ToString());

            //If we are loaded after the mod we are looking at we are overriding it.
            if (priorityA > priorityB)
            {
                if (!(A.mod == modB))
                {
                    A.isOverriding = true;
                    A.overrides[modB] = intersect;
                }
                if (!(B.mod == modA))
                {
                    B.isOverriden = true;
                    B.overriddenBy[modA] = intersect;
                }
            }
            else
            {
                if (!(A.mod == modB))
                {
                    A.isOverriden = true;
                    A.overriddenBy[modB] = intersect;
                }
                if (!(B.mod == modA))
                {
                    B.isOverriding = true;
                    B.overrides[modA] = intersect;
                }
            }
            this.OverrridingData[modA] = A;
            this.OverrridingData[modB] = B;
        }

        //Return a dict of all overriden mods with a list of overriden files as values.
        //else returns an empty string.
        public void GetOverridingData(List<ModItem> items)
        {
            ////Console.WriteLine(Environment.StackTrace);
            ////Console.WriteLine("Starting Overriding data check");
            this.OverrridingData.Clear();

            foreach (ModItem itemA in items)
            {
                //We only wanna check this for items actually enabled.
                if (!itemA.Checked)
                    continue;

                string modA = itemA.FolderName;
                int priorityA = items.Count - items.IndexOf(itemA);

                //Check if we allready have this mod in the dict if not create an entry for it.
                if (!this.OverrridingData.ContainsKey(modA))
                {
                    this.OverrridingData[modA] = new OverridingData
                    {
                        mod = modA,
                        overrides = new Dictionary<string, List<string>>(),
                        overriddenBy = new Dictionary<string, List<string>>()
                    };
                }
                OverridingData A = this.OverrridingData[modA];

                //Console.WriteLine("Checking: " + modA + " : " + priorityA.ToString());
                foreach (ModItem itemB in items)
                {
                    string modB = itemB.FolderName;

                    if (modA == modB)
                        continue;

                    if (!itemB.Checked)
                        continue;

                    //If we have allready seen modb in comparison to modA we don't need to compare because the comparison is bi-directionary.
                    if (
                        A.overriddenBy.ContainsKey(modB) ||
                        A.overrides.ContainsKey(modB)
                        )
                    {
                        ////Console.WriteLine("--" + modA + "has allready been compared to: " + modB);
                        continue;
                    }

                    //Check if we have allready seen modB before.
                    if (this.OverrridingData.ContainsKey(modB))
                    {
                        //If we have allready seen modB and we have allready compared modB and modA we don't need to compare because the comparison is bi-directionary.
                        if (
                            this.OverrridingData[modB].overriddenBy.ContainsKey(modA) ||
                            this.OverrridingData[modB].overrides.ContainsKey(modA)
                            )
                        {
                            ////Console.WriteLine("--" + modB + "has allready been compared to: " + modA);
                            continue;
                        }
                    }
                    else
                    {
                        //If we have not make a new modB overridingDatas
                        this.OverrridingData[modB] = new OverridingData
                        {
                            mod = modB,
                            overrides = new Dictionary<string, List<string>>(),
                            overriddenBy = new Dictionary<string, List<string>>()
                        };
                    }
                    GetModOverridingData(itemA, itemB, items.Count, this.OverrridingData[modA], this.OverrridingData[modB]);
                }
            }

            #region debug output

            //Debug output
            //foreach(string key in this.OverrridingData.Keys)
            //{
            //    //Console.WriteLine("MOD: " + key);
            //    //Console.WriteLine("--Overriden:");
            //    foreach (string mod in OverrridingData[key].overriddenBy.Keys)
            //    {
            //        //Console.WriteLine("----" + OverrridingData[key].isOverriden);
            //    }
            //    //Console.WriteLine("--Overrides:");
            //    foreach (string mod in OverrridingData[key].overrides.Keys)
            //    {
            //        //Console.WriteLine("----" + OverrridingData[key].isOverriding);
            //    }
            //}

            #endregion debug output

            ColorItemsOnOverridingData(items);
        }

        //Check color of a single mod.
        public void ColorItemOnOverrdingData(ModItem item)
        {
            ColorItemsOnOverridingData(new List<ModItem>() { item });
        }

        //Color the list view items based on data
        public void ColorItemsOnOverridingData(List<ModItem> items)
        {
            foreach (ListViewItem item in items)
            {
                string mod = item.SubItems[2].Text;

                //market for removal so don't color.
                if (item.SubItems[1].ForeColor == Color.Red)
                {
                    continue;
                }

                ////Console.WriteLine("Coloring mod: " + mod);
                if (!this.OverrridingData.ContainsKey(mod))
                {
                    item.SubItems[1].ForeColor = Color.Black;
                    ////Console.WriteLine("Black");

                    continue;
                }
                OverridingData A = OverrridingData[mod];
                if (A.isOverriden)
                {
                    ////Console.WriteLine("OrangeRed");
                    item.SubItems[1].ForeColor = Color.OrangeRed;
                }
                if (A.isOverriding)
                {
                    ////Console.WriteLine("Green");
                    item.SubItems[1].ForeColor = Color.Green;
                }
                if (A.isOverriding && A.isOverriden)
                {
                    ////Console.WriteLine("Orange");
                    item.SubItems[1].ForeColor = Color.Orange;
                }
                if (!A.isOverriding && !A.isOverriden)
                {
                    ////Console.WriteLine("Black");
                    item.SubItems[1].ForeColor = Color.Black;
                }
            }
        }

        //Check for all active mods in list provided if the mods in the required section are also active.
        public Dictionary<string, List<string>> CheckRequires(List<ModItem> items)
        {
            ////Console.WriteLine("Checking mods Requires");
            this.MissingModsDependenciesDict = new Dictionary<string, List<string>>();

            //For each mod check if their requires list is a sub list of the active mods list... aka see if the required mods are active.
            foreach (ModItem item in items)
            {
                //Console.WriteLine("---" + item.SubItems[1].Text);
                if (!item.Checked)
                {
                    item.SubItems[5].BackColor = Color.White;
                    item.SubItems[5].Text = "---";
                    continue;
                }

                string modDisplayName = item.SubItems[1].Text;
                string modFolderName = this.DirectoryToPathDict[item.SubItems[2].Text];

                if (!ModDetails.ContainsKey(modFolderName))
                    continue;

                if (ModDetails[modFolderName].Requires == null)
                {
                    item.SubItems[5].BackColor = Color.White;
                    item.SubItems[5].Text = "NONE";
                    continue;
                }

                List<string> Requires = ModDetails[modFolderName].Requires;
                List<string> activeMods = new List<string>();

                foreach (ModItem itemB in items)
                {
                    if (!itemB.Checked)
                        continue;
                    if (!(items.IndexOf(itemB) > items.IndexOf(item)))
                        continue;
                    ////Console.WriteLine(itemB.SubItems[1].Text);
                    activeMods.Add(itemB.SubItems[1].Text);
                }

                //Make a list of all mods we need but are not in the active mods.
                List<string> missingMods = Requires.Except(activeMods).ToList<string>();

                if (missingMods.Count == 0)
                {
                    ////Console.WriteLine("All subset items found!");
                    item.SubItems[5].BackColor = Color.Green;
                    item.SubItems[5].Text = "FOUND";
                    continue;
                }
                ////Console.WriteLine("Not all subset items found!");
                item.SubItems[5].BackColor = Color.Red;
                item.SubItems[5].Text = "MISSING";
                MissingModsDependenciesDict[modDisplayName] = missingMods;
            }
            return MissingModsDependenciesDict;
        }

        //Get display names of all dependencies of given mod.
        public List<string> GetModDependencies(string selectedMod)
        {
            selectedMod = this.DirectoryToPathDict[selectedMod];
            if (!ModDetails.ContainsKey(selectedMod))
            {
                return new List<string>();
            }
            return ModDetails[selectedMod].Requires;
        }

        //Monitor the size of a given zip file
        public void MonitorZipSize(BackgroundWorker worker, DoWorkEventArgs e)
        {
            string zipFile = Directory.GetParent(this.BasePath[0]).ToString() + "\\Mods.zip";
            long folderSize = Utils.DirSize(new DirectoryInfo(BasePath[0]));
            //zip usually does about 60 percent but we dont wanna complete at like 85 or 90 lets overestimate
            long compressedFolderSize = (long)Math.Round(folderSize * 0.35);
            //Console.WriteLine("Starting file size monitor, FolderSize: " + compressedFolderSize.ToString());
            while (!e.Cancel && !worker.CancellationPending)
            {
                while (!File.Exists(zipFile))
                {
                    System.Threading.Thread.Sleep(1000);
                }
                long zipFileSize = new FileInfo(zipFile).Length;
                int progress = Math.Min((int)((zipFileSize * (long)100) / compressedFolderSize), 100);
                //Console.WriteLine("--" + zipFileSize.ToString());
                //Console.WriteLine("--" + progress.ToString());
                worker.ReportProgress(progress);
                System.Threading.Thread.Sleep(500);
            }
        }
    }
}