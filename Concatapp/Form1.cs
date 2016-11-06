using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.IO.Compression;


namespace Concatapp
{
    public partial class Form1 : Form
    {
        private string[] acceptedExt = new string[4] { ".txt", ".fastq", ".zip", ".gz"};
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
                    string tempFolderPath = System.IO.Path.Combine(txtFolder.Text, "concatTemp");
                    Directory.CreateDirectory(tempFolderPath);
                    // concatenating
                    if (Files[0].Extension.Equals(".txt") || Files[0].Extension.Equals(".fastq"))
                    {
                        using (FileStream outputStream = File.Create(tempFolderPath + @"\" + txtOutput.Text + Files[0].Extension))
                        {
                            foreach (FileInfo file in Files)
                            {
                                using (FileStream inputStream = file.Open(FileMode.Open))
                                {
                                    // Buffer size can be passed as the second argument.
                                    inputStream.CopyTo(outputStream);
                                }
                                txtResult.Text += "The file " + file.Name + " has been processed.\r\n";
                            }
                        }
                        // compressing 
                        Compress(new FileInfo(tempFolderPath + @"\" + txtOutput.Text + Files[0].Extension));
                        // moving compressed file to parent directory
                        FileInfo compressed = new FileInfo(tempFolderPath + @"\" + txtOutput.Text + Files[0].Extension + ".gz");
                        compressed.CopyTo(compressed.Directory.Parent.FullName + "\\" + compressed.Name);
                    }
                    if (Files[0].Extension.Equals(".zip"))
                    {
                        // extarcting
                        foreach (FileInfo file in Files)
                        {
                            txtResult.Text += "Extracting + " + file.Name + "\r\n";
                            ZipFile.ExtractToDirectory(file.FullName, tempFolderPath);
                        }
                        // getting temp directory file info
                        DirectoryInfo tempD = new DirectoryInfo(tempFolderPath);
                        FileInfo[] tempFiles = tempD.GetFiles();
                        // concatenating
                        using (FileStream outputStream = File.Create(tempFolderPath + @"\" + txtOutput.Text + tempFiles[0].Extension))
                        {
                            foreach (FileInfo file in tempFiles)
                            {
                                using (FileStream inputStream = file.Open(FileMode.Open))
                                {
                                    // Buffer size can be passed as the second argument.
                                    inputStream.CopyTo(outputStream);
                                }
                                txtResult.Text += "The file " + file.FullName + " has been processed.\r\n";
                            }
                        }
                        // compressing 
                        Compress(new FileInfo(tempFolderPath + @"\" + txtOutput.Text + tempFiles[0].Extension));
                        // moving compressed file to parent directory
                        FileInfo compressed = new FileInfo(tempFolderPath + @"\" + txtOutput.Text + tempFiles[0].Extension + ".gz");
                        compressed.CopyTo(compressed.Directory.Parent.FullName + @"\" + compressed.Name);
                    }
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
                        System.Diagnostics.Process process = new System.Diagnostics.Process();
                        System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
                        startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                        startInfo.FileName = "cmd.exe";
                        startInfo.Arguments = "/C copy /b  " + filesFullNames + "\"" + txtFolder.Text + @"\" + txtOutput.Text + ".gz" + "\"";
                        txtResult.Text += startInfo.Arguments;
                        process.StartInfo = startInfo;
                        process.Start();
                    }

                    // deleting temp directory
                    DirectoryInfo di = new DirectoryInfo(tempFolderPath);
                    di.Delete(true);
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
                // TODO:
                valid = true;
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
                    using (FileStream outFile =
                                File.Create(fi.FullName + ".gz"))
                    {
                        using (GZipStream Compress =
                            new GZipStream(outFile,
                            CompressionMode.Compress))
                        {
                            // Copy the source file into 
                            // the compression stream.
                            inFile.CopyTo(Compress);

                            Console.WriteLine("Compressed {0} from {1} to {2} bytes.",
                                fi.Name, fi.Length.ToString(), outFile.Length.ToString());
                        }
                    }
                }
            }
        }
    }
}
