using BaseApp.ViewModels;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using WpfHelpers;

namespace BaseApp.Models
{
    public class SettingService : ViewModelBase
    {
        SqlConnection ObjSqlConnection;
        SqlCommand ObjSqlCommand;

        private static List<Settings> ObjSettingList;
        public SettingService()
        {
            ObjSqlConnection = new SqlConnection(ConfigurationManager.ConnectionStrings["DBConnection"].ConnectionString);
            ObjSqlCommand = new SqlCommand();
            ObjSqlCommand.Connection = ObjSqlConnection;
            ObjSqlCommand.CommandType = CommandType.StoredProcedure;

        }

        public async Task<List<Settings>> GetAll()
        {
            List<Settings> ObjSettingList = new List<Settings>();
            try
            {
                ObjSqlCommand.Parameters.Clear();
                ObjSqlCommand.CommandText = "GetAllValues";
                ObjSqlConnection.Open();

                var ObjSqlDataReader = ObjSqlCommand.ExecuteReader();
                if (ObjSqlDataReader.HasRows)
                {
                    Settings ObjSetting = null;
                    while (ObjSqlDataReader.Read())
                    {
                        ObjSetting = new Settings();
                        ObjSetting.Id = ObjSqlDataReader.GetInt32(0);
                        ObjSetting.PName = ObjSqlDataReader.GetString(1);
                        ObjSetting.IpAddress = ObjSqlDataReader.GetString(2);
                        ObjSetting.Port = ObjSqlDataReader.GetInt32(3);
                        ObjSetting.ExcelPath = ObjSqlDataReader.GetString(4);

                        ObjSettingList.Add(ObjSetting);
                    }
                }
                ObjSqlDataReader.Close();
            }
            catch (SqlException ex)
            {
                throw ex;
            }
            finally
            {
                ObjSqlConnection.Close();
            }
            return ObjSettingList;
        }


        public async Task<bool> Save(Settings ObjSetting)
        {
            return await Task.Run(() =>
            {
                bool isSaved = false;

                try
                {
                    ObjSqlCommand.Parameters.Clear();
                    ObjSqlCommand.CommandText = "upsertSetting"; // Use the stored procedure for both insert and update.

                    // Check if Id is provided (if not, insert a new record)
                    if (ObjSetting.Id == 0 || ObjSetting.Id == null)
                    {
                        ObjSqlCommand.Parameters.AddWithValue("@Id", DBNull.Value); // NULL value for insert
                    }
                    else
                    {
                        ObjSqlCommand.Parameters.AddWithValue("@Id", ObjSetting.Id); // Existing Id for update
                    }

                    // Add the rest of the parameters
                    ObjSqlCommand.Parameters.AddWithValue("@PName", ObjSetting.PName);
                    ObjSqlCommand.Parameters.AddWithValue("@IP", ObjSetting.IpAddress);
                    ObjSqlCommand.Parameters.AddWithValue("@Port", ObjSetting.Port);
                    ObjSqlCommand.Parameters.AddWithValue("@FPath", ObjSetting.ExcelPath);


                    // Execute the command and check how many rows are affected
                    ObjSqlConnection.Open();
                    int rowsAffected = ObjSqlCommand.ExecuteNonQuery();
                    isSaved = rowsAffected > 0;
                }
                catch (SqlException ex)
                {
                    System.Windows.MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    ObjSqlConnection.Close();
                }

                return isSaved;
            });
        }


    }
}
