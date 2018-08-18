using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using Pri.LongPath;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SmokeMonster_Romset_Parser
{
    public partial class Form1 : Form
    {
        //vars needed for drag drop window
        private const int WM_NCHITTEST = 0x84;
        private const int HTCLIENT = 0x1;
        private const int HTCAPTION = 0x2;

        bool hasInputFolderSet = false;
        bool hasOutputFolderSet = false;
        bool hasSMDBfileSet = false;
        List<Rom> romList = new List<Rom>();
        int sha256MatchesFound = 0;
        int files_copied = 0;



        public Form1()
        {
            InitializeComponent();
            label6.Hide();
            progressBar1.Hide();
            ParseButton.Hide();
        }


        //drag drop window
        protected override void WndProc(ref Message m)
        {
            switch (m.Msg)
            {
                case WM_NCHITTEST:
                    base.WndProc(ref m);
                    if ((int)m.Result == HTCLIENT)
                    {
                        m.Result = (IntPtr)HTCAPTION;
                    }
                    return;
            }
            base.WndProc(ref m);
        }

        //close application
        private void ClosePic_Click(object sender, EventArgs e)
        {
            DialogResult dialogResult = MessageBox.Show("Is this goodbye, now?", "Exit the program?", MessageBoxButtons.YesNo);
            if (dialogResult == DialogResult.Yes)
            {
                if (System.Windows.Forms.Application.MessageLoop)
                {
                    System.Windows.Forms.Application.Exit();
                }
                else
                {
                    System.Environment.Exit(1);
                }
            }         
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {

        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog folderDlg = new FolderBrowserDialog();
            folderDlg.ShowNewFolderButton = true;
            DialogResult result = folderDlg.ShowDialog();

            if (result == DialogResult.OK)
            {
                textBox1.Text = folderDlg.SelectedPath;
                Environment.SpecialFolder root = folderDlg.RootFolder;
            }
            hasInputFolderSet = true;
            ShowParseButton();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog folderDlg = new FolderBrowserDialog();
            folderDlg.ShowNewFolderButton = true;
            DialogResult result = folderDlg.ShowDialog();

            if (result == DialogResult.OK)
            {
                textBox2.Text = folderDlg.SelectedPath;
                Environment.SpecialFolder root = folderDlg.RootFolder;
            }
            hasOutputFolderSet = true;
            ShowParseButton();
        }

        public void ShowParseButton()
        {
            if (textBox1.Text != "" && textBox2.Text != "" && textBox3.Text != "" &&
                (radioButton1.Checked || radioButton2.Checked))
            {
                ParseButton.Show();
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            DialogResult result = openFileDialog1.ShowDialog();

            if (result == DialogResult.OK) // Test result.
            {
                textBox3.Text = openFileDialog1.FileName;
            }
            hasSMDBfileSet = true;

            if (parseSMDB())
            {
                var henk = romList;
                progressBar1.Maximum = romList.Count + 1;
                progressBar1.Step = 1;
                romsInListLabel.Text = romList.Count + " roms found in SMDB txt file";
                ShowParseButton();
            }

        }
        public void disableControls()
        {
            textBox1.Enabled = false;
            textBox2.Enabled = false;
            textBox3.Enabled = false;
            ParseButton.Enabled = false;
            radioButton1.AutoCheck = false;
            radioButton2.AutoCheck = false;
            button1.Enabled = false;
            button2.Enabled = false;
            button4.Enabled = false;
        }


        public bool parseSMDB()
        {
            string[] delimitedByTab1 = Pri.LongPath.File.ReadAllText(textBox3.Text).Split('\n').ToArray();

            foreach (string line in delimitedByTab1)
            {
                string[] delimitedByTab2 = line.Split('\t').ToArray();

                Rom currentRom = new Rom();
                if(delimitedByTab2.Length > 2 && delimitedByTab2 != null)
                {
                    currentRom.FileName = delimitedByTab2[1];
                    currentRom.Checksum = delimitedByTab2[0];

                    romList.Add(currentRom);
                }

            }

            return true;
        }

        private void ParseButton_Click(object sender, EventArgs e)
        {
            if (backgroundWorker1.IsBusy == false)
            {
                //disable control buttons while we process.
                disableControls();

                progressBar1.Show();
                label6.Show();
                backgroundWorker1.RunWorkerAsync();
            }
            else {
                MessageBox.Show("I am still processing, please wait!");
            }
        }


        // Compute the file's hash.
        private byte[] GetHashSha256(string filename)
        {
            SHA256 Sha256 = SHA256.Create();

            using (FileStream stream = Pri.LongPath.File.OpenRead(filename))
            {
                return Sha256.ComputeHash(stream);
            }
        }

        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            ShowParseButton();
        }

        private void radioButton2_CheckedChanged(object sender, EventArgs e)
        {
            ShowParseButton();
        }

        private void BackgroundWorker1_DoWork_1(object sender, DoWorkEventArgs e)
        {
            string[] files = Pri.LongPath.Directory.GetFiles(textBox1.Text,"*.*",SearchOption.AllDirectories);

            MessageBox.Show("Make sure your output directory is empty!");

            foreach (string file in files)
            {
                string sha256result = "";
                byte[] bytes = GetHashSha256(file);

                //get sha256 string
                foreach (byte b in bytes) sha256result += b.ToString("x2");

                foreach (Rom rom in romList)
                {
                    //check if file has a entry in SMDB
                    if (rom.Checksum == sha256result)
                    {
                        if (rom.ShaAlreadyMatched == false)
                        {
                            //only count up when we first encouter a rom (for romset completion percentage).
                            backgroundWorker1.ReportProgress(1);
                        }

                        var destinationFile = textBox2.Text + "\\" + rom.FileName.Replace("/", "\\");
                        var DestinationFolder = Pri.LongPath.Path.GetDirectoryName(destinationFile);
                        System.IO.DirectoryInfo di = System.IO.Directory.CreateDirectory(DestinationFolder);

                        if (System.IO.File.Exists(destinationFile))
                        {
                            try
                            {
                                System.IO.File.Delete(destinationFile);
                            } catch (Exception ex)
                            {
                                continue;
                            }
                        }

                        if (copyRom(file, destinationFile))
                        {
                            files_copied++;
                        }

                        rom.ShaAlreadyMatched = true;
                     }
                }
            }

            int missingCount = 0;
            List<Rom> romListMissing = new List<Rom>();

            //afterwards we ask the user to compile a missing files txt
            foreach (Rom rom in romList)
            {
                if (rom.ShaAlreadyMatched == false)
                {
                    romListMissing.Add(rom);
                }
            }

            if (romListMissing.Count > 0)
            {
                DialogResult dialogResultmissing = MessageBox.Show("You are missing " + romListMissing.Count + " roms in your set. \n" +
                    "Do you wish to generate a txt file describing the missing rom(s)? \nThis txt file can be used as input for this program again. \nThe txt will be generated in your Output folder.",
                    "Your romset is not complete", MessageBoxButtons.YesNo
                    );
                if (dialogResultmissing == DialogResult.Yes)
                {
                    //afterwards we ask the user to compile a missing files txt
                    foreach (Rom rommissing in romListMissing)
                    {
                        Pri.LongPath.File.AppendAllText(textBox2.Text + "\\" + "missing.txt", rommissing.Checksum + "\t" + rommissing.FileName + Environment.NewLine);
                    }
                }
                else if (dialogResultmissing == DialogResult.No)
                {
                    //nothing for now
                }
            }
        }

        public bool copyRom(string from, string to)
        {
            try
            {
               Pri.LongPath.File.Copy(from, to);                
            }
            catch (Exception e)
            {
                MessageBox.Show("Failed to copy - " + from + " - " + e.ToString());
            }

            return true;
        }

        // This event handler updates the progress indicator in the GUI
        private void backgroundWorker1_ProgressChanged_1(object sender, ProgressChangedEventArgs e)
        {
            progressBar1.Value = sha256MatchesFound++;
            label7.Text = "Files copied to a folder:" + files_copied;
            shaMatchesFoundLabel.Text = "Matched roms: " + sha256MatchesFound;
            shaMatchesFoundLabel.Invalidate();
            shaMatchesFoundLabel.Update();
            shaMatchesFoundLabel.Refresh();
            Application.DoEvents();
        }
        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void progressBar1_Click(object sender, EventArgs e)
        {

        }

        private void backgroundWorker2_DoWork(object sender, DoWorkEventArgs e)
        {

        }
    }

}
