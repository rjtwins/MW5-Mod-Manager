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

namespace MW5_Mod_Manager
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// 
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
        public ProgramData ProgramData = new ProgramData();
        public string Vendor = "";
        public bool CreatedModlist = false;
        public JObject parent;
        public string CurrentFolderInsearch;
        public bool InterruptSearch = false;
        public string BasePath = "";
        public string[] Directories;
        public Dictionary<string, bool> ModList = new Dictionary<string, bool>();

        public Dictionary<string, ModObject> ModDetails = new Dictionary<string, ModObject>();
        public string rawJson;

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

        //Try and load install dir path from stored data files.
        public bool TryLoadInstallDir()
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

                if (this.ProgramData.installdir != null && this.ProgramData.installdir != "")
                {
                    this.BasePath = this.ProgramData.installdir;
                }
                if (this.ProgramData.vendor != null && this.ProgramData.vendor != "")
                {
                    this.Vendor = this.ProgramData.vendor;
                }
            }
            catch (Exception e)
            {

            }

            if (this.BasePath != null && this.BasePath != "")
                return true;
            return false;
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

            //Turns out there are sometimes "ghost" entries in the modlist.json for witch there are no directories left, lets remove those.
            List<string> toRemove = new List<string>();
            foreach (KeyValuePair<string, bool> entry in this.ModList)
            {
                if (this.Directories.Contains<string>(entry.Key))
                {
                    continue;
                }
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

        public string FindInstallDir(BackgroundWorker worker, DoWorkEventArgs e)
        {
            //No dir set from previous session locate install dir
            //first lets look in common install folders to minimize search time:
            foreach (DriveInfo d in DriveInfo.GetDrives().Where(x => x.IsReady == true))
            {
                foreach (string folder in this.CommonFolders)
                {
                    string found = Findfile(d.Name, "MechWarrior.exe", worker, e);
                    if (found != null && found != "")
                    {
                        this.BasePath = found + @"\MW5Mercs\Mods";
                        return BasePath;
                    }
                }
            }
            //install location not found, look EVERYWHERE:
            foreach (DriveInfo d in DriveInfo.GetDrives().Where(x => x.IsReady == true))
            {
                string found = Findfile(d.Name, "MechWarrior.exe", worker, e);
                if (found != null && found != "")
                {
                    this.BasePath = found + @"\MW5Mercs\Mods";
                    return BasePath;
                }
            }
            //return some sort of error in locating the install location.
            return "";
        }

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
    }

    public class ModObject
    {
        public string displayName { set; get; }
        public string version { set; get; }
        public string description { set; get; }
        public string author { set; get; }
        public string authorURL { set; get; }
        public float defaultLoadOrder { set; get; }
        public string gameVersion { set; get; }
        public List<string> manifest { get; set; }
    }

    public class ProgramData
    {
        public string vendor { set; get; }
        public float version { set; get; }
        public string installdir { set; get; }
    }
}
