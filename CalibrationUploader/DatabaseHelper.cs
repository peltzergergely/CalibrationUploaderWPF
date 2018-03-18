using Npgsql;
using System;
using System.Configuration;
using System.Windows;

namespace CalibrationUploader
{
    public class DatabaseHelper
    {
        //Counts rows in table
        public int CountRowInDB(string tableToSearch, string columnToSearch, string dataToFind)
        {
            using (new WaitCursor())
            {
                try
                {
                    string connstring = ConfigurationManager.ConnectionStrings["LTCTrace.DBConnectionString"].ConnectionString;
                    var conn = new NpgsqlConnection(connstring);
                    conn.Open();
                    var cmd = new NpgsqlCommand("SELECT COUNT(*) FROM " + tableToSearch + " WHERE " + columnToSearch + " = :dataToFind", conn);
                    cmd.Parameters.Add(new NpgsqlParameter("dataToFind", dataToFind));
                    Int32 countProd = Convert.ToInt32(cmd.ExecuteScalar());
                    conn.Close();

                    return countProd;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                    return -1;
                }
            }
        }
    }
}