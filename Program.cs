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
        public ProgramData ProgramData = new ProgramData();

        public JObject parent;
        public string[] Directories;

        public Dictionary<string, ModObject> ModDetails = new Dictionary<string, ModObject>();
        public Dictionary<string, bool> ModList = new Dictionary<string, bool>();
        public Dictionary<string, OverridingData> OverrridingData = new Dictionary<string, OverridingData>();
        public Dictionary<string, List<string>> MissingModsDependenciesDict = new Dictionary<string, List<string>>();
        public Dictionary<string, string> Presets = new Dictionary<string, string>();

        public bool CreatedModlist = false;

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

                //Console.WriteLine("Finshed loading ProgramData.json:" 
                    //+ " Vendor: " + this.ProgramData.vendor 
                    //+ " Version: " + this.ProgramData.version 
                    //+ " Installdir: " + this.ProgramData.installdir);

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
                //Console.WriteLine("ERROR: Something went wrong while loading ProgramData.json");
                //Console.WriteLine(e.Message);
                //Console.WriteLine(e.StackTrace);
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
                //Console.WriteLine(Ex.Message);
                //Console.WriteLine(Ex.StackTrace);
                return;
            }
        }

        public void ParseDirectories()
        {
            this.Directories = Directory.GetDirectories(BasePath);
            for (int i = 0; i < Directories.Length; i++)
            {
                string directory = this.Directories[i];
                string[] temp = directory.Split('\\');
                Directories[i] = temp[temp.Length - 1];
            }

            //We don't need to do this because we now have a steam base path assigned on selecting the install dir.
            //if (this.Vendor == "STEAM")
            //{
            //    if (WorkshopPath == "")
            //    {
            //        //Console.WriteLine("Found Steam version");
            //        string workshopPath = BasePath;
            //        workshopPath = workshopPath.Remove(workshopPath.Length - 46, 46);
            //        //Console.WriteLine($"trimmed path is {workshopPath}");
            //        workshopPath += ("workshop\\content\\784080");
            //        //Console.WriteLine($"full workshop path is {workshopPath}");
            //        WorkshopPath = workshopPath;
            //    }
            //    if (!Directory.Exists(WorkshopPath))
            //        return;
            //    this.WorkshopDirectories = Directory.GetDirectories(WorkshopPath);
            //    for (int i = 0; i < WorkshopDirectories.Length; i++)
            //    {
            //        string directory = this.WorkshopDirectories[i];
            //        string[] temp = directory.Split('\\');
            //        WorkshopDirectories[i] = temp[temp.Length - 1];
            //    }
            //}
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
            //if (this.Vendor == "STEAM" && this.WorkshopDirectories != null)
            //    foreach (string modDir in this.WorkshopDirectories)
            //    {
            //        if (this.ModList.ContainsKey(modDir))
            //            continue;

            //        ModList.Add(modDir, false);
            //    }

            //Turns out there are sometimes "ghost" entries in the modlist.json for witch there are no directories left, lets remove those.
            List<string> toRemove = new List<string>();
            foreach (KeyValuePair<string, bool> entry in this.ModList)
            {
                if (this.Directories.Contains<string>(entry.Key))
                    continue;
                //else if (this.Vendor == "STEAM" && this.WorkshopDirectories != null)
                //    if (this.WorkshopDirectories.Contains<string>(entry.Key))
                //        continue;
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
            //if (this.Vendor == "STEAM" && this.WorkshopDirectories != null)
            //    foreach (string modDir in this.WorkshopDirectories)
            //    {
            //        try
            //        {
            //            string modJson = File.ReadAllText(WorkshopPath + @"\" + modDir + @"\mod.json");
            //            ModObject mod = JsonConvert.DeserializeObject<ModObject>(modJson);
            //            this.ModDetails.Add(modDir, mod);
            //        }
            //        catch (Exception e)
            //        {
            //            string message = "ERROR loading mod.json in : " + modDir +
            //                " folder will be skipped. " +
            //                " If this is not a mod folder you can ignore ths message.";
            //            string caption = "ERROR Loading";
            //            MessageBoxButtons buttons = MessageBoxButtons.OK;
            //            MessageBox.Show(message, caption, buttons);

            //            if (ModList.ContainsKey(modDir))
            //            {
            //                ModList.Remove(modDir);
            //            }
            //            if (ModDetails.ContainsKey(modDir))
            //            {
            //                ModDetails.Remove(modDir);
            //            }
            //        }
            //    }         
        }

        public void SaveModDetails()
        {
            foreach (KeyValuePair<string, ModObject> entry in this.ModDetails)
            {
                string modJsonPath = BasePath + @"\" + entry.Key + @"\mod.json";
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

        #region pack mods to zip
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
            //Console.WriteLine("Starting zip compression");
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
        }
        #endregion

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
            //Console.WriteLine(scrambled);
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
            //Console.WriteLine(unscrambled);
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
            string JsonString = JsonConvert.SerializeObject(Logic.Presets, Formatting.Indented);

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
            List<string> manifestA = this.ModDetails[modA].manifest;
            List<string> manifestB = this.ModDetails[modB].manifest;
            List<string> intersect = manifestA.Intersect(manifestB).ToList();

            //If the intersects elements are greater then zero we have shared parts of the manifest
            if (intersect.Count() == 0)
                return;

            ////Console.WriteLine("---Intersection: " + modB + " : " + priorityB.ToString());

            //If we are loaded after the mod we are looking at we are overriding it.
            if (priorityA > priorityB)
            {
                if(!(A.mod == modB))
                {
                    A.isOverriding = true;
                    A.overrides[modB] = intersect;
                }
                if(!(B.mod == modA))
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
            #endregion

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
        public Dictionary<string, List<string>> CheckRequires (List<ModItem> items)
        {
            ////Console.WriteLine("Checking mods Requires");
            this.MissingModsDependenciesDict = new Dictionary<string, List<string>>();

            //For each mod check if their requires list is a sub list of the active mods list... aka see if the required mods are active.
            foreach(ModItem item in items)
            {
                Console.WriteLine("---" + item.SubItems[1].Text);
                if (!item.Checked)
                {
                    item.SubItems[5].BackColor = Color.White;
                    item.SubItems[5].Text = "---";
                    continue;
                }

                string modDisplayName = item.SubItems[1].Text;
                string modFolderName = item.SubItems[2].Text;

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
            if (!ModDetails.ContainsKey(selectedMod))
            {
                return new List<string>();
            }
            return ModDetails[selectedMod].Requires;
        }

        //Monitor the size of a given zip file
        public void MonitorZipSize(BackgroundWorker worker, DoWorkEventArgs e)
        {
            string zipFile = Directory.GetParent(this.BasePath).ToString() + "\\Mods.zip";
            long folderSize = Utils.DirSize(new DirectoryInfo(BasePath));
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
                int progress = Math.Min((int)((zipFileSize * (long)100) / compressedFolderSize ), 100);
                //Console.WriteLine("--" + zipFileSize.ToString());
                //Console.WriteLine("--" + progress.ToString());
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

    public class ModItem : ListViewItem
    {

        public ModItem() : base("", 0)
        {
            //other stuff here
        }

        public string DisplayName 
            {
                get { return this.SubItems[1].Text; }
                set { this.SubItems[1].Text = value; } 
            }

        public string FolderName
        {
            get { return this.SubItems[2].Text; }
            set { this.SubItems[2].Text = value; }
        }
    }
}
