using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Windows.Forms;

namespace FileHasher
{
    public partial class Form1 : Form
    {
        public string arg;
        public int HashType = 0;
        public Form1()
        {
            InitializeComponent();
        }

        public Form1(string argument)
        {
            arg = argument;
            InitializeComponent();
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            if (string.IsNullOrEmpty((string)e.Argument))
            {
                e.Result = Properties.Resources.InvalidFilenameErrorText;
                return;
            }

            byte[] buffer = new byte[] { };
            using (var stream = File.OpenRead((string)e.Argument))
            {
                switch (HashType)
                {
                    case 0:
                        buffer = MD5.Create().ComputeHash(stream);
                        break;
                    case 1:
                        buffer = SHA1.Create().ComputeHash(stream);
                        break;
                    case 2:
                        buffer = SHA256.Create().ComputeHash(stream);
                        break;
                }
            }
            e.Result = GetHashStringFromArray(buffer);
        }

        private string GetHashStringFromArray(byte[] data)
        {
            return BitConverter.ToString(data).Replace("-", "");
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if ((string)e.Result == Properties.Resources.InvalidFilenameErrorText)
            {
                progressBar1.Maximum = 100;
                progressBar1.Value = 0;
                progressBar1.Style = ProgressBarStyle.Continuous;
                this.filenameTextBox.Text = (string)e.Result;
                MessageBox.Show((string)e.Result, "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                groupBox3.Enabled = false;
            }
            else
            {
                progressBar1.Maximum = 100;
                progressBar1.Value = 100;
                progressBar1.Style = ProgressBarStyle.Continuous;
                resultTextBox.Text = (string)e.Result;
                groupBox3.Enabled = true;
            }

            openFileButton.Enabled = true;
            computeButton.Enabled = true;
        }

        private void openFileButton_Click(object sender, EventArgs e)
        {
            var ofdresult = openFileDialog1.ShowDialog();
            if (ofdresult == DialogResult.OK && !string.IsNullOrEmpty(openFileDialog1.FileName))
            {
                filenameTextBox.Text = openFileDialog1.FileName;
            }
        }

        private void computeButton_Click(object sender, EventArgs e)
        {
            openFileButton.Enabled = false;
            computeButton.Enabled = false;
            progressBar1.Style = ProgressBarStyle.Marquee;
            backgroundWorker1.RunWorkerAsync(filenameTextBox.Text);
        }

        private void filenameTextBox_TextChanged(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(filenameTextBox.Text) || filenameTextBox.Text == "" || filenameTextBox.Text == Properties.Resources.FilenameTextBoxDefaultText)
            {
                groupBox2.Enabled = false;
                groupBox3.Enabled = false;
            }
            else
            {
                groupBox2.Enabled = true;
                groupBox3.Enabled = false;
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            groupBox1.Text = Properties.Resources.Group1Header;
            group1Label.Text = Properties.Resources.Group1Label;
            openFileButton.Text = Properties.Resources.BrowseButtonDefaultText;
            filenameTextBox.Text = Properties.Resources.FilenameTextBoxDefaultText;

            groupBox2.Text = Properties.Resources.Group2Header;
            group2Label.Text = Properties.Resources.Group2Label;
            computeButton.Text = Properties.Resources.ComputeButtonDefaultText;
            progressBar1.Style = ProgressBarStyle.Blocks;

            groupBox3.Text = Properties.Resources.Group3Header;
            group3Label.Text = Properties.Resources.Group3Label;
            copyButton.Text = Properties.Resources.CopyButtonDefaultText;
            resultLabel.Text = Properties.Resources.ResultsLable;
            saveButton.Text = Properties.Resources.SaveButtonDefaultText;
            compareLabel.Text = Properties.Resources.CompareLabel;

            saveFileDialog1.Filter = "Text|*.txt";
            saveFileDialog1.Title = Properties.Resources.SaveDialogTitle;

            openFileDialog1.FileName = "";

            enableCMenuCheckBox.Checked = GlobalSettings.WinContextMenuEnabled;

            if (!string.IsNullOrEmpty(arg))
            {
                filenameTextBox.Text = arg;
                computeButton_Click(sender, e);
            }
        }

        private void copyButton_Click(object sender, EventArgs e)
        {
            Clipboard.SetText(this.resultTextBox.Text);
        }

        private void saveButton_Click(object sender, EventArgs e)
        {
        RETRY:
            var dialogresult = saveFileDialog1.ShowDialog(this);
            if (!string.IsNullOrEmpty(saveFileDialog1.FileName) && dialogresult != DialogResult.Cancel)
            {
                try
                {
                    File.WriteAllText(saveFileDialog1.FileName, this.resultTextBox.Text);
                }
                catch (Exception ex)
                {
                    if (MessageBox.Show(this, Properties.Resources.FileSaveErrorText + " \r\n" + ex.Message, Properties.Resources.SaveDialogTitle, MessageBoxButtons.RetryCancel, MessageBoxIcon.Error, MessageBoxDefaultButton.Button2) == DialogResult.Retry)
                        goto RETRY;
                }
            }

            saveFileDialog1.FileName = "";
        }

        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton1.Checked)
                HashType = 0;
            if (radioButton2.Checked)
                HashType = 1;
            if (radioButton3.Checked)
                HashType = 2;

            resultTextBox.Text = "";
            groupBox3.Enabled = false;
        }

        private void compareTextBox_TextChanged(object sender, EventArgs e)
        {
            if(string.IsNullOrEmpty(compareTextBox.Text))
            {
                compareTextBox.BackColor = System.Drawing.SystemColors.Control;
                return;
            }
            if (compareTextBox.Text.ToUpper() == resultTextBox.Text.ToUpper())
                compareTextBox.BackColor = System.Drawing.Color.LightGreen;
            else
                compareTextBox.BackColor = System.Drawing.Color.LightCoral;

        }

        private void enableCMenuCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (!IsAdmin())
            {
                RestartElevated(arg);
                return;
            }

            if (enableCMenuCheckBox.Checked)
            {
                RegistryKey regmenu = null;
                RegistryKey regcmd = null;
                try
                {
                    regmenu = Registry.ClassesRoot.CreateSubKey("*\\shell\\Checksum");
                    if (regmenu != null)
                        regmenu.SetValue("", "Checksum");
                    regcmd = Registry.ClassesRoot.CreateSubKey("*\\shell\\Checksum\\command");
                    if (regcmd != null)
                        regcmd.SetValue("", Application.ExecutablePath + " %1");

                    GlobalSettings.WinContextMenuEnabled = true;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, ex.ToString());
                }
                finally
                {
                    if (regmenu != null)
                        regmenu.Close();
                    if (regcmd != null)
                        regcmd.Close();
                }

            }
            else
            {
                try
                {
                    RegistryKey reg = Registry.ClassesRoot.OpenSubKey("*\\shell\\Checksum\\command");
                    if (reg != null)
                    {
                        reg.Close();
                        Registry.ClassesRoot.DeleteSubKey("*\\shell\\Checksum\\command");
                    }
                    reg = Registry.ClassesRoot.OpenSubKey("*\\shell\\Checksum");
                    if (reg != null)
                    {
                        reg.Close();
                        Registry.ClassesRoot.DeleteSubKey("*\\shell\\Checksum");
                    }

                    GlobalSettings.WinContextMenuEnabled = false;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, ex.ToString());
                }
            }

            Application.DoEvents();
            enableCMenuCheckBox.Checked = GlobalSettings.WinContextMenuEnabled;
        }

        static internal bool IsAdmin()
        {
            WindowsIdentity id = WindowsIdentity.GetCurrent();
            WindowsPrincipal p = new WindowsPrincipal(id);
            return p.IsInRole(WindowsBuiltInRole.Administrator);
        }

        internal static void RestartElevated(string arg)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.UseShellExecute = true;
            startInfo.WorkingDirectory = Environment.CurrentDirectory;
            startInfo.FileName = Application.ExecutablePath;
            startInfo.Arguments = arg;
            startInfo.Verb = "runas";
            try
            {
                Process p = Process.Start(startInfo);
            }
            catch (System.ComponentModel.Win32Exception ex)
            {
                return;
            }

            Application.Exit();
        }
    }
}
