using System;
using System.Diagnostics;
using System.Windows.Forms;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;


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

        private async void startBtn_Click(object sender, EventArgs e)
        {
            startBtn.Enabled = false;
            abortBtn.Enabled = true;
            abortBtn.Visible = true;
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
                                    await inputStream.CopyToAsync(outputStream);
                                }
                                txtResult.Text += "The file " + file.Name + " has been concatenated.\r\n";
                            }
                        }
                        // compressing 
                        txtResult.Text += "Compressing\r\n";
                        await Compress(new FileInfo(tempFolderPath + @"\" + txtOutput.Text + Files[0].Extension));
                        // moving compressed file to parent directory
                        txtResult.Text += "Copying from temp directory to base directory\r\n";
                        FileInfo compressed = new FileInfo(tempFolderPath + @"\" + txtOutput.Text + Files[0].Extension + ".gz");
                        compressed.MoveTo(compressed.Directory.Parent.FullName + "\\" + compressed.Name);
                    }
                    // .zip files
                    else if (Files[0].Extension.Equals(".zip"))
                    {
                        // extarcting
                        foreach (FileInfo file in Files)
                        {
                            txtResult.Text += "Extracting -" + file.Name + "\r\n";
                            await Task.Run(() => ZipFile.ExtractToDirectory(file.FullName, tempFolderPath));
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
                            txtResult.Text += "Concatenating: " + tempfileNames + "\r\n";
                            using (FileStream outputStream = File.Create(tempFolderPath + @"\" + txtOutput.Text + tempFiles[0].Extension))
                            {
                                foreach (FileInfo file in tempFiles)
                                {
                                    using (FileStream inputStream = file.Open(FileMode.Open))
                                    {
                                        await inputStream.CopyToAsync(outputStream);
                                    }
                                    txtResult.Text += "The file " + file.Name + " has been processed.\r\n";
                                }
                            }
                            //Concat(txtResult, tempFolderPath, tempFiles, txtOutput.Text);
                            // compressing 
                            txtResult.Text += "Compressing\r\n";
                            await Compress(new FileInfo(tempFolderPath + @"\" + txtOutput.Text + tempFiles[0].Extension));
                            // moving compressed file to parent directory
                            txtResult.Text += "Copying from temp directory to base directory\r\n";
                            FileInfo compressed = new FileInfo(tempFolderPath + @"\" + txtOutput.Text + tempFiles[0].Extension + ".gz");
                            compressed.MoveTo(compressed.Directory.Parent.FullName + @"\" + compressed.Name);
                        }
                    }
                    // .gz files 
                    else if (Files[0].Extension.Equals(".gz"))
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
                        txtResult.Text += "Concatenating...(might take a few minutes)\r\n";
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
            startBtn.Enabled = true;
            abortBtn.Enabled = false;
            abortBtn.Visible = false;
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
        public static async Task<int> Compress(FileInfo fi)
        {
            // Get the stream of the source file.
            using (FileStream inFile = fi.OpenRead())
            {
                // Prevent compressing hidden and already compressed files.
                if ((File.GetAttributes(fi.FullName) & FileAttributes.Hidden) != FileAttributes.Hidden & fi.Extension != ".gz")
                {
                    // Create the compressed file.
                    using (FileStream outFile = File.Create(fi.FullName + ".gz"))
                    {
                        using (GZipStream Compress = new GZipStream(outFile, CompressionMode.Compress))
                        {
                            // Copy the source file into 
                            // the compression stream.
                            await inFile.CopyToAsync(Compress);
                        }
                    }
                }
            }
            return 0;
        }

        private void UpdateStatus()
        {
            txtResult.Text += ".";
        }

        private void abortBtn_Click_1(object sender, EventArgs e)
        {
            Environment.Exit(Environment.ExitCode);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            txtResult.Text += "\r\n-----App still working-----\r\n";
        }

        private void button2_Click(object sender, EventArgs e)
        {
            string message = " ----- Welcome to concat app -----\r\nThis app can concatenate .fastq,.txt,.zip,.txt files.\r\nPlease select the folder that contains the files you would like to concat and the output name and press start\r\nNote: The folder should contain only the files to concatenate and all files must have the same extension";
            MessageBox.Show(message);
        }
    }
}
