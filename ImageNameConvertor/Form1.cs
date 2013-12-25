using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Drawing.Imaging;
using System.Text.RegularExpressions;

namespace ImageNameConvertor
{
    public partial class Form1 : Form
    {
        private List<string> sourceFiles = null;
        public string folderName { get; set; }

        public string FolderN { get; set; }

        private int counter = 0;
        private BackgroundWorker worker = null;

        private static Regex r = new Regex(":");

        public Form1()
        {
            InitializeComponent();
            sourceFiles = new List<string>();
            toolStripStatusLabel2.Text = "0";
            folderName = "D:\\";
            //textBox2.DataBindings.Add("Text", this, "FolderN", false, DataSourceUpdateMode.OnPropertyChanged);
            //textBox2.DataBindings.Add(new Binding("Text", this, "FolderN", false, DataSourceUpdateMode.OnPropertyChanged));
        }

        private void button2_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog folderD = new FolderBrowserDialog();
            if (folderD.ShowDialog() == DialogResult.OK)
            {
                folderName = folderD.SelectedPath;
                FolderN = folderD.SelectedPath;
                textBox2.Text = folderName;
                FolderN = "100";
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            OpenFileDialog openD = new OpenFileDialog();
            openD.Multiselect = true;
            openD.Filter = "Image Files (.jpg,.bmp,.png)|*.jpg;*.bmp;*.png|All Files|*.*";
            if (openD.ShowDialog() == DialogResult.OK)
            {
                StringBuilder sb = new StringBuilder();
                sourceFiles.Clear();
                foreach (string path in openD.FileNames)
                {
                    if (".JPG" == Path.GetExtension(path).ToUpper())
                    {
                        sourceFiles.Add(path);
                        sb.Append(Path.GetFileName(path)).Append(";");
                    }
                }
                textBox1.Text = sb.ToString();
                toolStripStatusLabel2.Text = sourceFiles.Count.ToString();
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (sourceFiles.Count <= 0 || String.IsNullOrEmpty(folderName))
            {
                return;
            }

            button1.Enabled = false;
            button2.Enabled = false;
            button3.Enabled = false;

            counter = 0;
            worker = new BackgroundWorker();
            worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(worker_RunWorkerCompleted);
            worker.ProgressChanged += new ProgressChangedEventHandler(worker_ProgressChanged);
            worker.DoWork += new DoWorkEventHandler(worker_DoWork);
            worker.WorkerReportsProgress = true;
            worker.WorkerSupportsCancellation = true;
            worker.RunWorkerAsync();
        }

        private void worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            toolStripStatusLabel3.Text = e.ProgressPercentage.ToString() + "%";
        }

        private void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            button1.Enabled = true;
            button2.Enabled = true;
            button3.Enabled = true;
        }

        private void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            string extension = "";
            string newfileName = "";
            Image image = null;
            foreach (string path in sourceFiles)
            {
                counter++;
                worker.ReportProgress(counter * 100 / sourceFiles.Count);
                extension = Path.GetExtension(path);
                newfileName = Path.GetFileNameWithoutExtension(path);
                try
                {
                    /*
                    image = null;
                    image = Image.FromFile(path);
                    if (image.PropertyIdList.Contains<int>(36867))
                    {
                        PropertyItem item = image.GetPropertyItem(36867);   //date taken
                        if (item != null)
                        {
                            DateTime dateTaken = DateTime.Parse(r.Replace(Encoding.UTF8.GetString(item.Value), "-", 2));
                            newfileName = string.Format("{0}{1}{2} {3}{4}{5}",
                                dateTaken.Year,
                                dateTaken.Month.ToString("#00"),
                                dateTaken.Day.ToString("#00"),
                                dateTaken.Hour.ToString("#00"),
                                dateTaken.Minute.ToString("#00"),
                                dateTaken.Second.ToString("#00"));
                        }
                    }
                     */
                    DateTime dateTaken = GetDateTakenFromImage(path);
                    if (dateTaken != DateTime.MinValue)
                    {
                        newfileName = string.Format("{0}{1}{2} {3}{4}{5}",
                                                       dateTaken.Year,
                                                       dateTaken.Month.ToString("#00"),
                                                       dateTaken.Day.ToString("#00"),
                                                       dateTaken.Hour.ToString("#00"),
                                                       dateTaken.Minute.ToString("#00"),
                                                       dateTaken.Second.ToString("#00"));
                    }
                }
                catch (Exception ex)
                {
                }
                finally
                {
                    if (image != null)
                        image.Dispose();
                }

                CheckFileName(ref newfileName, extension);
                File.Copy(path, String.Format("{0}\\{1}{2}", folderName, newfileName, extension), true);
                System.Threading.Thread.Sleep(1);   //sleep 1 mini-second to update main view.
            }
        }

        private void CheckFileName(ref string filename, string extension)
        {
            if (File.Exists(String.Format("{0}\\{1}{2}", folderName, filename, extension)))
            {
                string format = filename + " ({0})";
                string tmpName = filename;
                for (var i = 1; i <= 10; i++)
                {
                    tmpName = string.Format(format, i);
                    if (File.Exists(String.Format("{0}\\{1}{2}", folderName, tmpName, extension)))
                    {
                        continue;
                    }
                    break;
                }
                filename = tmpName;
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (worker != null && worker.IsBusy)
            {
                DialogResult result = MessageBox.Show("图片正在处理中，确认要退出程序？", "", MessageBoxButtons.YesNo);
                if (result == DialogResult.Yes)
                {
                    worker.CancelAsync();
                }
                else
                {
                    e.Cancel = true;
                }
            }
        }

        //retrieves the datetime WITHOUT loading the whole image
        private DateTime GetDateTakenFromImage(string path)
        {
            using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                using (Image myImage = Image.FromStream(fs, false, false))
                {
                    if (myImage.PropertyIdList.Contains<int>(36867))
                    {
                        PropertyItem propItem = myImage.GetPropertyItem(36867);
                        string dateTaken = r.Replace(Encoding.UTF8.GetString(propItem.Value), "-", 2);
                        return DateTime.Parse(dateTaken);
                    }
                    return DateTime.MinValue;
                }
            }
        }
    }
}
