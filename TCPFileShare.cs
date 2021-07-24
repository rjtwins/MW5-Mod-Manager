//using System;
//using System.Net;
//using System.IO;
//using System.Net.Sockets;
//using System.Windows.Forms;
//using System.Threading;
//using System.ComponentModel;
//using System.IO.Compression;
//using System.Collections.Generic;

//namespace MW5_Mod_Manager
//{
//    //For handling file upload
//    public class TCPFileShare
//    {
//        //max buffer is 8192 but lets keep some for possible overhead
//        readonly int BufferSize = 8000;
//        readonly int PortN = 20;
//        string RecieverIP = "";
//        string FilePath = "";
//        int NoOfPackets = 0;
//        int CurrentPacketNr = 0;
//        int TotalLength = 0;

//        string ProgramDataPath = "";
//        string OutputFoler = "";
//        public bool StopDownloadOrUnpack = false;

//        Form5 form5;
//        Form6 form6;
//        public MainLogic logic;
//        public Form1 MainForm;
//        public BackgroundWorker Runner = new BackgroundWorker();
//        public BackgroundWorker Listener = new BackgroundWorker();


//        public TCPFileShare(MainLogic logic, Form1 MainForm)
//        {
//            this.MainForm = MainForm;
//            Runner.WorkerReportsProgress = true;
//            Runner.WorkerSupportsCancellation = true;
//            Runner.DoWork += new DoWorkEventHandler(Runner_DoWork);
//            Runner.RunWorkerCompleted += new RunWorkerCompletedEventHandler(Runner_RunWorkerCompleted);
//            Runner.ProgressChanged += new ProgressChangedEventHandler(Runner_ProgressChanged);

//            Listener.WorkerReportsProgress = true;
//            Listener.WorkerSupportsCancellation = true;
//            Listener.DoWork += new DoWorkEventHandler(Listener_DoWork);
//            Listener.RunWorkerCompleted += new RunWorkerCompletedEventHandler(Listener_RunWorkerCompleted);
//            Listener.ProgressChanged += new ProgressChangedEventHandler(Listener_ProgressChanged);

//            this.ProgramDataPath = System.Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData)
//                                + @"\MW5LoadOrderManager";
//            this.logic = logic;
//            this.form6 = new Form6(this);
//        }

//        //For canceling the upload process
//        internal void CancelUpload()
//        {
//            Runner.CancelAsync();
//        }

//        #region TCPSend background workers
//        private void Runner_ProgressChanged(object sender, ProgressChangedEventArgs e)
//        {
//            form5.progressBar1.Value = e.ProgressPercentage;
//            form5.label1.Text = "Sending File Packet " + CurrentPacketNr + " of " + NoOfPackets;
//        }

//        private void Runner_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
//        {
//            form5.button3.Enabled = false;
//            form5.button2.Enabled = true;
//        }

//        private void Runner_DoWork(object sender, DoWorkEventArgs e)
//        {
//            // Get the BackgroundWorker that raised this event.
//            BackgroundWorker worker = sender as BackgroundWorker;

//            byte[] SendingBuffer = null;
//            TcpClient client = null;
//            NetworkStream netstream = null;
//            Console.WriteLine("Trying connecting to: " + this.RecieverIP + " on port: " + this.PortN);

//            try
//            {
//                client = new TcpClient(this.RecieverIP, this.PortN);
//                netstream = client.GetStream();
//                Console.WriteLine("Connected to the Server...");
//                FileStream Fs = new FileStream(FilePath, FileMode.Open, FileAccess.Read);
//                NoOfPackets = Convert.ToInt32(Math.Ceiling(Convert.ToDouble(Fs.Length) / Convert.ToDouble(BufferSize)));
//                TotalLength = (int)Fs.Length;
//                int CurrentPacketLength = 0;
//                CurrentPacketNr = 0;

//                Console.WriteLine("SENDING: NrP " + NoOfPackets + " File Lenght" + TotalLength);

//                for (int i = 0; i < NoOfPackets; i++)
//                {
//                    CurrentPacketNr = i;
//                    //If we wanna stop the upload process we do it here.
//                    if (Runner.CancellationPending || e.Cancel == true)
//                    {
//                        netstream.Close();
//                        client.Close();
//                        Fs.Close();
//                        if (File.Exists(FilePath))
//                            File.Delete(FilePath);
//                        return;
//                    }

//                    if (TotalLength > BufferSize)
//                    {
//                        CurrentPacketLength = BufferSize;
//                        TotalLength = TotalLength - CurrentPacketLength;
//                    }
//                    else
//                    {
//                        CurrentPacketLength = TotalLength;
//                    }
//                    SendingBuffer = new byte[CurrentPacketLength];
//                    Fs.Read(SendingBuffer, 0, CurrentPacketLength);
//                    netstream.Write(SendingBuffer, 0, (int)SendingBuffer.Length);

//                    //If we report for every packet we end up locking up the UI Thread
//                    if ((i % 10) == 0)
//                    {
//                        int PercentageComplete = CurrentPacketNr * 100 / NoOfPackets;
//                        worker.ReportProgress(PercentageComplete);
//                        //Console.WriteLine("SENDING: PNr " + CurrentPacketNr + " Of " + NoOfPackets + " Completed " + PercentageComplete + "%");
//                    }

//                }
//                CurrentPacketNr = NoOfPackets;
//                worker.ReportProgress(100);
//                Fs.Close();
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine(ex.Message);
//            }
//            finally
//            {
//                netstream.Close();
//                client.Close();
//            }
//        }
//        #endregion

//        #region TCPListener background workers
//        private void Listener_ProgressChanged(object sender, ProgressChangedEventArgs e)
//        {
//            BackgroundWorker worker = sender as BackgroundWorker;
//            string message = (string)e.UserState;
//            if(message == "CD")
//            {
//                string m = "The download has been canceled.";
//                string c = "DOWNLOAD CANCELED";
//                MessageBoxButtons buttons = MessageBoxButtons.OK;
//                MessageBox.Show(m, c, buttons);

//                //Canceled during download
//                this.form6.label1.Text = "DOWNLOAD CANCELED";
//                this.form6.Close();
//                return;
//            }
//            else if(message == "CU")
//            {
//                string m = "Your mods are very likely to now contain corrupted files.";
//                string c = "WARNING UNPACK INTERRUPTED!";
//                MessageBoxButtons buttons = MessageBoxButtons.OK;
//                MessageBox.Show(m, c, buttons);

//                //Canceled during unpack
//                this.form6.label1.Text = "WARNING UNPACK INTERRUPTED!\n Your mods are very likely to now contain corrupted files.";
//                form6.button3.Enabled = false;
//                form6.button2.Enabled = true;
//                this.form6.Close();
//                return;
//            }

//            if (StopDownloadOrUnpack)
//                return;

//            int progress = e.ProgressPercentage;
//            form6.progressBar1.Step = 250;
//            form6.progressBar1.Maximum = 1000;
//            if (message == "REC1")
//            {
//                form6.ShowDialog(MainForm);
//            }
//            else if (message == "REC")
//            {
//                if (form6.progressBar1.Value == 1000)
//                {
//                    form6.progressBar1.Value = 0;
//                }
//                form6.progressBar1.PerformStep();
//                form6.label1.Text = "Received " + String.Format("{0:0.0}", ((double)progress / 1048576)) + "MB";
//            }
//            else if (message == "DONE")
//            {
//                form6.progressBar1.Maximum = 100;
//                form6.progressBar1.Value = 100;
//                form6.button3.Enabled = false;
//                form6.button2.Enabled = true;
//            }
//            else
//            {
//                //we are now unpacking
//                form6.progressBar1.Maximum = 101;
//                form6.progressBar1.Value = progress;
//                form6.label1.Text = "Unpacking: \n" + message;
//            }
//        }

//        private void Listener_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
//        {
//            BackgroundWorker worker = sender as BackgroundWorker;
//            //THIS SHOULS NEVER EVER HAPPEN UNLESS WE TERMINATE THE WORKER BY MISTAKE
//        }

//        private void Listener_DoWork(object sender, DoWorkEventArgs e)
//        {
//            Form6 form6 = new Form6(this);
//            // Get the BackgroundWorker that raised this event.
//            BackgroundWorker worker = sender as BackgroundWorker;
//            this.ReceiveTCP(worker, e);
//        }

//        #endregion

//        //Prepare to sent the mods folder via TCP to a receiver
//        //Pack all mods into a zip file before sending them off.
//        //We use the preexisting code to pack stuff here.
//        //Before we start packing check if we are not allready bizzy sending.
//        public void prepareSentTCP(Form1 MainForm, Form5 form5)
//        {
//            this.form5 = form5;
//            this.RecieverIP = form5.textBox1.Text;
//            string parent = Directory.GetParent(MainForm.logic.BasePath).ToString();
//            this.FilePath = parent + @"\Mods.zip";
//            //this.FilePath = @"C:\ProgramData\MW5LoadOrderManager\Mods.zip";
//            bool ValidateIP = IPAddress.TryParse(RecieverIP, out IPAddress ip);
//            Console.WriteLine("Valid IP: " + ValidateIP);
//            this.OutputFoler = MainForm.logic.BasePath;

//            if (!ValidateIP)
//                return;

//            if (Runner.IsBusy)
//                return;

//            if (File.Exists(FilePath))
//            {
//                FileInfo info = new FileInfo(FilePath);
//                string message = "A previously packed version of Mods.Zip was detected, it was last edited at:\n" + info.LastWriteTime.ToString() + "\n Do you want to use this instead packing your mods into a new one?";
//                string caption = "Mods.zip Detected";
//                MessageBoxButtons buttons = MessageBoxButtons.YesNo;
//                if(MessageBox.Show(message, caption, buttons) == DialogResult.No)
//                {
//                    MainForm.JustPacking = false;
//                    MainForm.exportModsFolderToolStripMenuItem_Click(null, null);
//                }
//            }
//            else
//            {
//                MainForm.JustPacking = false;
//                MainForm.exportModsFolderToolStripMenuItem_Click(null, null);
//            }
//            Runner.RunWorkerAsync();
//        }

//        //Listener for TCP Connections (on port set as class variable)
//        public void ReceiveTCP(BackgroundWorker worker, DoWorkEventArgs e)
//        {
//            int portN = this.PortN;
//            TcpListener Listener = null;
//            try
//            {
//                Listener = new TcpListener(IPAddress.Any, portN);
//                Listener.Start();
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine(ex.Message);
//            }

//            byte[] RecData = new byte[BufferSize];
//            int RecBytes;

//            while (true)
//            {
//                this.StopDownloadOrUnpack = false;
//                TcpClient client = null;
//                NetworkStream netstream = null;
//                bool ConnectionDenied = false;
//                try
//                {
//                    string message = "File incoming from: ";
//                    string caption = "Incoming Connection";
//                    MessageBoxButtons buttons = MessageBoxButtons.YesNo;

//                    if (Listener.Pending())
//                    {
//                        client = Listener.AcceptTcpClient();
//                        netstream = client.GetStream();
//                        IPEndPoint remoteEndPoint = client.Client.RemoteEndPoint as IPEndPoint;
//                        IPEndPoint localEndPoint = client.Client.LocalEndPoint as IPEndPoint;
//                        string remoteAddress = remoteEndPoint.Address.ToString();
//                        string localPort = localEndPoint.Port.ToString();

//                        message += remoteAddress + " at port: " + localPort;

//                        //Super ugly but we wanna run this on the UI thread else it will complain.
//                        Thread t = (new Thread(() => {

//                            if (MessageBox.Show(message, caption, buttons) != DialogResult.Yes)
//                            {
//                                //Deny the connection
//                                netstream.Close();
//                                client.Close();
//                                ConnectionDenied = true;
//                            }
//                        }));
//                        t.SetApartmentState(ApartmentState.STA);
//                        t.Start();
//                        t.Join();

//                        if (ConnectionDenied)
//                        {
//                            continue;
//                        }
//                        //Report that we are now starting to receive.
//                        worker.ReportProgress(0, "REC1");
//                        //Write out the filestream to a file
//                        int totalrecbytes = 0;
//                        FileStream Fs = new FileStream(ProgramDataPath + "\\Mods.zip", FileMode.OpenOrCreate, FileAccess.Write);
//                        while ((RecBytes = netstream.Read(RecData, 0, RecData.Length)) > 0)
//                        {
//                            if (this.StopDownloadOrUnpack)
//                            {
//                                worker.ReportProgress(0, "CD");
//                                //just stop now and warn the user in work completed.
//                                break;
//                            }

//                            Fs.Write(RecData, 0, RecBytes);
//                            totalrecbytes += RecBytes;

//                            //Every 524288 bytes (half an mb) report the total number of bytes received.
//                            if (totalrecbytes % 524288 == 0)
//                                worker.ReportProgress(totalrecbytes, "REC");
//                        }
//                        Fs.Close();
//                        if(this.StopDownloadOrUnpack)
//                        {
//                            //Cleanup
//                            if(File.Exists(ProgramDataPath + "\\Mods.zip"))
//                            {
//                                File.Delete(ProgramDataPath + "\\Mods.zip");
//                            }

//                            //don't do the rest just stop and go back to the while loop.
//                            continue;
//                        }

//                        Console.WriteLine("Unpacking Mods.zip to : " + OutputFoler);
//                        //report that we are now unpacking
//                        worker.ReportProgress(0, "");

//                        //Unpack file and cleanup
//                        ZipArchive zip = ZipFile.Open(ProgramDataPath + "\\Mods.zip", ZipArchiveMode.Read);
//                        int nrEntries = zip.Entries.Count;
//                        int currentEntry = 0;

//                        foreach (ZipArchiveEntry entry in zip.Entries)
//                        {
//                            if (this.StopDownloadOrUnpack)
//                            {
//                                worker.ReportProgress(0, "CU");
//                                //just stop now and warn the user in work completed.
//                                break;
//                            }

//                            currentEntry += 1;
//                            int percentageComplete = (currentEntry * 100) / nrEntries;
//                            worker.ReportProgress(percentageComplete, entry.FullName);
//                            //Console.WriteLine("Unpacking: " + entry.FullName);
//                            string FilePath = OutputFoler + "\\" + entry.FullName;
//                            FilePath = FilePath.Replace("/", "\\");
//                            string[] SplitFilePath = FilePath.Split('\\');
//                            Array.Resize(ref SplitFilePath, SplitFilePath.Length - 1);
//                            string FolderPath = String.Join("\\", SplitFilePath);

//                            if (!Directory.Exists(FolderPath))
//                            {
//                                Directory.CreateDirectory(FolderPath);
//                            }
//                            entry.ExtractToFile(FilePath, true);
//                        }
//                        zip.Dispose();
//                        worker.ReportProgress(0, "DONE");
//                    }
//                }
//                catch (Exception ex)
//                {
//                    Console.WriteLine(ex.Message);
//                    //netstream.Close();
//                }
//            }

//        }
//    }
//}
