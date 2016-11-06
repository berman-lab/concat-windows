using System;
using System.Diagnostics;
using System.Windows.Forms;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.ComponentModel;


namespace Concatapp
{
    public partial class Form1 : Form
    {
        private string[] acceptedExt = new string[4] { ".txt", ".fastq", ".zip", ".gz"};

        // background worker for concatenate
        private Thread thread;

        public Form1()
        {
            InitializeComponent();
        }

        private void btn_browse_Click(object sender, EventArgs e)
        {
            if (folderBrowser.ShowDialog() == DialogResult.OK)
            {
                txtFolder.Text = folderBrowser.SelectedPath;
            }
        }

        private void startBtn_Click(object sender, EventArgs e)
        {
            // clearing output text box
            txtResult.Text = "";
            // checking if arguments exist
            if (String.IsNullOrEmpty(txtFolder.Text) || String.IsNullOrEmpty(txtOutput.Text))
                txtResult.Text += "Error - please supply arguments";
            else
            {
                txtResult.Text += "Starting\r\n";
                // getting files and printing
                DirectoryInfo d = new DirectoryInfo(txtFolder.Text);
                FileInfo[] Files = d.GetFiles();
                string fileNames = "";
                foreach (FileInfo file in Files)
                {
                    fileNames = fileNames + file.Name + " ";
                }
                txtResult.Text += "Concatenating: " + fileNames + "\r\n";
                // check if files have valid extension
                if (validate(Files))
                {
                    // creating temp folder
                    string tempFolderPath = Path.Combine(txtFolder.Text, "concatTemp");
                    Directory.CreateDirectory(tempFolderPath);
                    // .txt and .fastq 
                    if (Files[0].Extension.Equals(".txt") || Files[0].Extension.Equals(".fastq"))
                    {
                        // concatenating
                        using (FileStream outputStream = File.Create(tempFolderPath + @"\" + txtOutput.Text + Files[0].Extension))
                        {
                            foreach (FileInfo file in Files)
                            {
                                using (FileStream inputStream = file.Open(FileMode.Open))
                                {
                                    Thread concatthread = new Thread(() => inputStream.CopyTo(outputStream));
                                    concatthread.Start();
                                    // waiting for finish
                                    concatthread.Join();
                                }
                                txtResult.Text += "The file " + file.Name + " has been processed.\r\n";
                            }
                        }
                        // compressing 
                        Thread thread = new Thread(() => Compress(new FileInfo(tempFolderPath + @"\" + txtOutput.Text + Files[0].Extension)));
                        thread.Start();
                        // waiting for finish
                        thread.Join();
                        // moving compressed file to parent directory
                        FileInfo compressed = new FileInfo(tempFolderPath + @"\" + txtOutput.Text + Files[0].Extension + ".gz");
                        compressed.CopyTo(compressed.Directory.Parent.FullName + "\\" + compressed.Name);
                    }
                    // .zip files
                    else if (Files[0].Extension.Equals(".zip"))
                    {
                        // extarcting
                        foreach (FileInfo file in Files)
                        {
                            txtResult.Text += "Extracting -" + file.Name + "\r\n";
                            Thread unzipthread = new Thread(() => ZipFile.ExtractToDirectory(file.FullName, tempFolderPath));
                            unzipthread.Start();
                            // waiting for finish
                            unzipthread.Join();
                        }
                        // getting temp directory file info
                        DirectoryInfo tempD = new DirectoryInfo(tempFolderPath);
                        FileInfo[] tempFiles = tempD.GetFiles();
                        // validating after extraction
                        if (validate(tempFiles))
                        {
                            // printing decompressed files
                            string tempfileNames = "";
                            foreach (FileInfo file in tempFiles)
                            {
                                tempfileNames = tempfileNames + file.Name + " ";
                            }
                            txtResult.Text += "Extracted: " + tempfileNames + "\r\n";
                            // concatenating
                            using (FileStream outputStream = File.Create(tempFolderPath + @"\" + txtOutput.Text + tempFiles[0].Extension))
                            {
                                foreach (FileInfo file in tempFiles)
                                {
                                    using (FileStream inputStream = file.Open(FileMode.Open))
                                    {
                                        Thread concatthread = new Thread(() => inputStream.CopyTo(outputStream));
                                        concatthread.Start();
                                        // waiting for finish
                                        concatthread.Join();
                                    }
                                    txtResult.Text += "The file " + file.Name + " has been processed.\r\n";
                                }
                            }
                            //Concat(txtResult, tempFolderPath, tempFiles, txtOutput.Text);
                            // compressing 
                            Thread thread = new Thread(() => Compress(new FileInfo(tempFolderPath + @"\" + txtOutput.Text + tempFiles[0].Extension)));
                            thread.Start();
                            // waiting for finish
                            thread.Join();
                            // moving compressed file to parent directory
                            FileInfo compressed = new FileInfo(tempFolderPath + @"\" + txtOutput.Text + tempFiles[0].Extension + ".gz");
                            compressed.CopyTo(compressed.Directory.Parent.FullName + @"\" + compressed.Name);
                        }
                    }
                    // .gz files 
                    if (Files[0].Extension.Equals(".gz"))
                    {
                        string filesFullNames = "";
                        // building filenames
                        foreach (FileInfo file in Files)
                        {   
                            if (file != Files[Files.Length -1])
                                filesFullNames = filesFullNames + "\"" + file.FullName + "\"" + " + ";
                            else
                                filesFullNames = filesFullNames + "\"" +  file.FullName + "\"" + " ";
                        }
                        // concatenating
                        txtResult.Text += "Concatenating\r\n";
                        ProcessStartInfo startInfo = new ProcessStartInfo();
                        startInfo.WindowStyle = ProcessWindowStyle.Hidden;
                        startInfo.FileName = "cmd.exe";
                        startInfo.Arguments = "/C copy /b  " + filesFullNames + "\"" + txtFolder.Text + @"\" + txtOutput.Text + ".gz" + "\"";
                        txtResult.Text += startInfo.Arguments;
                        Process process = Process.Start(startInfo);
                        // waiting till process is finished
                        process.WaitForExit();
                    }

                    // deleting temp directory
                    DirectoryInfo di = new DirectoryInfo(tempFolderPath);
                    di.Delete(true);
                    txtResult.Text += "\r\n-------finished-------\r\n";
                }
            }
        }

        // used to validate the input
        private bool validate(FileInfo[] Files)
        {
            bool valid = true;
            // checking if the extension is in the valid extension list
            foreach (FileInfo file in Files)
            {
                if (Array.IndexOf(acceptedExt, file.Extension) == -1)
                {
                    valid = false;
                    txtResult.Text += "Error - the file " + file.Name + "has invalid extension\r\n";
                    break;
                }
            }
            // continue only if we have valid extension, now checking if all extension are the same
            if (valid)
            {
                string [] otherfiles = Directory.GetFiles(Files[0].Directory.FullName).Where(x => Path.GetExtension(x) != Files[0].Extension).ToArray();
                if (otherfiles.Length > 0)
                {
                    txtResult.Text += "Error - the files/files inside zip have different extensions";
                    valid = false;
                }
            }
            return valid;
        }

        // compressing file to .gz
        public static void Compress(FileInfo fi)
        {
            // Get the stream of the source file.
            using (FileStream inFile = fi.OpenRead())
            {
                // Prevent compressing hidden and 
                // already compressed files.
                if ((File.GetAttributes(fi.FullName)
                    & FileAttributes.Hidden)
                    != FileAttributes.Hidden & fi.Extension != ".gz")
                {
                    // Create the compressed file.
                    using (FileStream outFile = File.Create(fi.FullName + ".gz"))
                    {
                        using (GZipStream Compress = new GZipStream(outFile, CompressionMode.Compress))
                        {
                            // Copy the source file into 
                            // the compression stream.
                            inFile.CopyTo(Compress);

                            //Console.WriteLine("Compressed {0} from {1} to {2} bytes.",
                              //  fi.Name, fi.Length.ToString(), outFile.Length.ToString());
                        }
                    }
                }
            }
        }

        private void UpdateStatus()
        {
            txtResult.Text += ".";
        }

    }
}
