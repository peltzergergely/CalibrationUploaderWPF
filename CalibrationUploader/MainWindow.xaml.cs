using Npgsql;
using System;
using System.Configuration;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;

namespace CalibrationUploader
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// xml html serialnumber
    /// </summary>
    public partial class MainWindow : Window
    {
        public bool IsDmValidated { get; set; } = false;

        public bool AllFieldsValidated { get; set; } = false;

        public DateTime? StartedOn { get; set; } = null;

        public MainWindow()
        {
            Loaded += (sender, e) => MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
            InitializeComponent();
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

            if (Keyboard.FocusedElement == SaveBtn)
            {
                SaveBtn_Click(sender, e);
            }
            DmValidator();
        }

        public bool RegexValidation(string dataToValidate, string datafieldName)
        {
            string rgx = ConfigurationManager.AppSettings[datafieldName];
            return (Regex.IsMatch(dataToValidate, rgx));
        }

        private void DmValidator()
        {
            if (RegexValidation(HousingDmTxbx.Text, "HousingDmRegEx") == true)
                IsDmValidated = true;
            else
                IsDmValidated = false;
        }

        private void ResetForm()
        {
            IsDmValidated = false;
            AllFieldsValidated = false;
            HousingDmTxbx.Text = "";
            HousingDmTxbx.Focus();
        }

        private void DbInsert(string table)
        {
            try
            {
                using (NpgsqlConnection conn = new NpgsqlConnection(ConfigurationManager.ConnectionStrings["ltctrace.dbconnectionstring"].ConnectionString))
                {
                    conn.Open();
                    var cmd = new NpgsqlCommand("insert into " + table + " (housing_dm, fb_dm, pc_name, started_on, saved_on) " +
                    "values(:housing_dm, :fb_dm, :pc_name, :started_on, :saved_on)", conn);
                    cmd.Parameters.Add(new NpgsqlParameter("housing_dm", HousingDmTxbx.Text));
                    cmd.Parameters.Add(new NpgsqlParameter("pc_name", Environment.MachineName));
                    cmd.Parameters.Add(new NpgsqlParameter("started_on", StartedOn));
                    cmd.Parameters.Add(new NpgsqlParameter("saved_on", DateTime.Now));
                    //uploading the pictures

                    int result = cmd.ExecuteNonQuery();
                    if (result == 1)
                    {
                        this.Close();
                    }
                    conn.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void HousingDmTxbx_LostFocus(object sender, RoutedEventArgs e)
        {
            StartedOn = DateTime.Now;
        }

        private void MainMenuBtn_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void SaveBtn_Click(object sender, RoutedEventArgs e)
        {
            //DbInsert("housing_fb_assy");
        }
    }
}
