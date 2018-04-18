using Npgsql;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using System.Xml.Linq;
using System.Linq;
using Microsoft.Win32;

namespace CalibrationUploader
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// internalserial, HTML, XML FULL FILE PATH needed.
    /// </summary>
    public partial class MainWindow : Window
    {
        public bool IsDmValidated { get; set; } = false;

        public bool AllFieldsValidated { get; set; } = true;

        public DateTime? StartedOn { get; set; } = null;

        public string[] args = Environment.GetCommandLineArgs();

        public string HTMLFilePath { get; set; }

        public string XMLFilePath { get; set; }

        public MainWindow()
        {
            Loaded += (sender, e) => MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
            InitializeComponent();

            if (args.Length > 1)
            {
                TestIDTxbx.Text = args[1];
            }

            System.Threading.Thread.Sleep(2000);
            GetLatestXMLFile();
            GetLatestHTMLFile();
        }

        private void OnKeyUpEvent(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                Close();

            if (e.Key == Key.Tab)
            {
                e.Handled = true;
                return;
            }

            if (e.Key == Key.Enter && HousingDmTxbx.Text.Length > 0)
            {
                TraversalRequest tRequest = new TraversalRequest(FocusNavigationDirection.Next);
                UIElement keyboardFocus = Keyboard.FocusedElement as UIElement;

                if (keyboardFocus != null)
                {
                    keyboardFocus.MoveFocus(tRequest);
                }
                e.Handled = true;
            }

            DmValidator();
            if (Keyboard.FocusedElement == SaveBtn)
            {
                SaveBtn_Click(sender, e);
            }
        }

        private void DmValidator()
        {
            if (HousingDmTxbx.Text.Length > 3
                && TestIDTxbx.Text.Length > 3
                && HTMLResultChkbx.IsChecked == true
                && XMLResultChkbx.IsChecked == true)
                IsDmValidated = true;
            else
                IsDmValidated = false;
        }

        private void GetLatestXMLFile()
        {
            try
            {
                if (TestIDTxbx.Text.Length > 3)
                {
                    DirectoryInfo info = new DirectoryInfo(ConfigurationManager.AppSettings["XMLFilePath"]);
                    FileInfo file = info.GetFiles("*" + TestIDTxbx.Text + "*").OrderByDescending(f => f.LastWriteTime).First();
                    XMLFilePath = file.FullName;
                    TestResult(file.FullName);
                    XMLResultChkbx.IsChecked = true;
                }
            }
            catch (Exception ex)
            {
                //MessageBox.Show(ex.ToString());
            }
        }

        private void GetLatestHTMLFile()
        {
            try
            {
                if (TestIDTxbx.Text.Length > 3)
                {
                    DirectoryInfo info = new DirectoryInfo(ConfigurationManager.AppSettings["HTMLFilePath"]);
                    FileInfo file = info.GetFiles("*" + TestIDTxbx.Text + "*").OrderByDescending(f => f.LastWriteTime).First();
                    HTMLFilePath = file.FullName;
                    HTMLResultChkbx.IsChecked = true;
                }
            }
            catch (Exception ex)
            {
                //MessageBox.Show(ex.ToString());
            }
        }

        private void TestResult(string filepath)
        {
            try
            {
                string filename = filepath;

                XNamespace tr = "urn:IEEE-1636.1:2011:01:TestResults";

                XElement xmlRootElement = XElement.Load(filename);
                IEnumerable<XElement> getOutcome = from resultOutcomes in xmlRootElement.Descendants(tr + "Outcome") select resultOutcomes;

                string result = (string)getOutcome.FirstOrDefault().Attribute("value");

                if (result == "Passed")
                {
                    TestResultChkbx.IsChecked = true;
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show("Error with XML parsing: " + ex.Message);
            }
        }

        private void DbInsert(string table)
        {
            try
            {
                FileStream file1 = new FileStream(HTMLFilePath, FileMode.Open, FileAccess.Read);
                var fileToByteArr = new byte[file1.Length];
                file1.Read(fileToByteArr, 0, Convert.ToInt32(file1.Length));
                file1.Close();

                FileStream file2 = new FileStream(XMLFilePath, FileMode.Open, FileAccess.Read);
                var fileToByteArr2 = new byte[file2.Length];
                file2.Read(fileToByteArr2, 0, Convert.ToInt32(file2.Length));
                file2.Close();

                using (NpgsqlConnection conn = new NpgsqlConnection(ConfigurationManager.ConnectionStrings["ltctrace.dbconnectionstring"].ConnectionString))
                {
                    conn.Open();
                    var cmd = new NpgsqlCommand("INSERT INTO " + table + " (housing_dm, test_result, internal_id, pc_name, started_on, saved_on, filename, file, filename1, file1) " +
                    "VALUES(:housing_dm, :test_result, :internal_id, :pc_name, :started_on, :saved_on, :filename, :file, :filename1, :file1)", conn);
                    cmd.Parameters.Add(new NpgsqlParameter("housing_dm", HousingDmTxbx.Text));
                    cmd.Parameters.Add(new NpgsqlParameter("test_result", TestResultChkbx.IsChecked));
                    cmd.Parameters.Add(new NpgsqlParameter("internal_id", TestIDTxbx.Text));
                    cmd.Parameters.Add(new NpgsqlParameter("pc_name", Environment.MachineName));
                    cmd.Parameters.Add(new NpgsqlParameter("started_on", StartedOn));
                    cmd.Parameters.Add(new NpgsqlParameter("saved_on", DateTime.Now));
                    cmd.Parameters.Add(new NpgsqlParameter("filename", Path.GetFileName(HTMLFilePath)));
                    cmd.Parameters.Add(new NpgsqlParameter("file", fileToByteArr));
                    cmd.Parameters.Add(new NpgsqlParameter("filename1", Path.GetFileName(XMLFilePath)));
                    cmd.Parameters.Add(new NpgsqlParameter("file1", fileToByteArr2));
                    cmd.ExecuteNonQuery();
                    //closing connection ASAP
                    conn.Close();
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in SQL uploading: " + ex.Message);
            }
        }

        private void HousingDmTxbx_LostFocus(object sender, RoutedEventArgs e)
        {
            if (HousingDmTxbx.Text.Length > 0)
            {
                var preCheck = new DatabaseHelper();
                if (preCheck.CountRowInDB(ConfigurationManager.AppSettings["prevtablename"], "housing_dm", HousingDmTxbx.Text) == 0)
                {
                    responseLbl.Text = "Figyelem! A termék nem szerepelt a HiPot I. teszten!";
                }
                StartedOn = DateTime.Now;
            }
        }

        private void MainMenuBtn_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void SaveBtn_Click(object sender, RoutedEventArgs e)
        {
            DmValidator();
            if (IsDmValidated)
            {
                DbInsert(ConfigurationManager.AppSettings["tablename"]);
            }
            else
            {
                MessageBox.Show("Hiányos adatok!");
            }
        }

        OpenFileDialog openFileDialog = new OpenFileDialog();

        private void LaunchFiledialog(string initialDir)
        {
            openFileDialog.Filter = "All files (*.*)|*.*";
            openFileDialog.InitialDirectory = initialDir;
            openFileDialog.ShowDialog();
        }

        private void XMLloader_Click(object sender, RoutedEventArgs e)
        {
            LaunchFiledialog(ConfigurationManager.AppSettings["XMLFilePath"]);
            if (openFileDialog.FileName != "")
            {
                XMLResultChkbx.IsChecked = true;
                TestResult(openFileDialog.FileName);
                XMLFilePath = openFileDialog.FileName;
            }
        }

        private void HTMLLoaderBtn_Click(object sender, RoutedEventArgs e)
        {
            LaunchFiledialog(ConfigurationManager.AppSettings["HTMLFilePath"]);
            if (openFileDialog.FileName != "")
            {
                HTMLResultChkbx.IsChecked = true;
                HTMLFilePath = openFileDialog.FileName;
            }
        }

        private void TestIDTxbx_LostFocus(object sender, RoutedEventArgs e)
        {
            GetLatestXMLFile();
            GetLatestHTMLFile();
        }
    }
}
