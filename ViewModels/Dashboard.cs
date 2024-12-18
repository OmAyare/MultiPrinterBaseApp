﻿using BaseApp.Models;
using ClosedXML.Excel;
using DocumentFormat.OpenXml.Wordprocessing;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;
using WpfHelpers;
using WpfHelpers.Controls;
using System.ComponentModel;


namespace BaseApp.ViewModels
{
    public class Dashboard : ViewModelBase
    {
        public ConnectionService _connectionService { get; private set; }

        public ICommand OpenFileDialog { get; set; }
        public ICommand ConnectCommand { get; set; }
        public ICommand StartCommand { get; set; }
        public ICommand StopCommand { get; set; }
        public ICommand SendCommand { get; }

        private string _ipAddress;
        public string IpAddress
        {
            get => _ipAddress;
            set
            {
                _ipAddress = value;
                OnPropertyChanged(nameof(IpAddress));
            }
        }
        private int _port;
        public int Port
        {
            get => _port;
            set
            {
                _port = value;
                OnPropertyChanged(nameof(Port));
            }
        }


        private string filePath;
        public string FilePath
        {
            get
            {
                return filePath;
            }
            set
            {
                filePath = value;
                OnPropertyChanged("FilePath");
            }
        }

        private DataTable _ExcelData;
        public DataTable ExcelData
        {
            get => _ExcelData;
            set
            {
                _ExcelData = value;
                OnPropertyChanged(nameof(ExcelData));
            }
        }

        private bool _isStartButtonEnabled = true;
        public bool IsStartButtonEnabled
        {
            get { return _isStartButtonEnabled; }
            set
            {
                _isStartButtonEnabled = value;
                OnPropertyChanged(nameof(IsStartButtonEnabled));
            }
        }

        private bool _isStopButtonEnabled = false;
        public bool IsStopButtonEnabled
        {
            get { return _isStopButtonEnabled; }
            set
            {
                _isStopButtonEnabled = value;
                OnPropertyChanged(nameof(IsStopButtonEnabled));
            }
        }

        private ObservableCollection<string> _templates;
        public ObservableCollection<string> Templates
        {
            get { return _templates; }
            set
            {
                _templates = value;
                OnPropertyChanged(nameof(Templates));  // Ensure this triggers the ComboBox update
            }
        }



        private string _selectedTemplate;
        public string SelectedTemplate
        {
            get { return _selectedTemplate; }
            set
            {
                _selectedTemplate = value;
                OnPropertyChanged(nameof(SelectedTemplate));  // Notify of changes to the property
            }
        }

        private bool _isStartCommandSent = false;
        public bool IsStartCommandSent
        {
            get { return _isStartCommandSent; }
            set
            {
                _isStartCommandSent = value;
                OnPropertyChanged(nameof(IsStartCommandSent));
            }
        }

        private int _printedRowCount;

        public int PrintedRowCount
        {
            get => _printedRowCount;
            set
            {
                _printedRowCount = value;
                OnPropertyChanged(nameof(PrintedRowCount));

            }

        }

        private string _pName;
        public string PName
        {
            get { return _pName; }
            set
            {
                _pName = value;
                OnPropertyChanged(nameof(PName)); // Notify UI of property change
            }
        }

        private ObservableCollection<Settings> _settings;
        public ObservableCollection<Settings> Settings
        {
            get { return _settings; }
            set
            {
                _settings = value;
                OnPropertyChanged(nameof(Settings));
            }
        }


        public Dashboard()
        {
            // Assuming you fetch settings from the database
            FetchAllSettings();

            _connectionService = new ConnectionService();
            Templates = new ObservableCollection<string>();
            StartCommand = new RelayCommand(SendStartCommand);
            StopCommand = new RelayCommand(SendStopCommand);
            SendCommand = new RelayCommand(SendRowToServer);

            OpenFileDialog = new DelegateCommand(async (param) =>
            {
                var openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
                openFileDialog1.Filter = "CSV (*.csv,*.csv)|*.csv;*.csv|Excel (*.xlsx,*.xlsx)|*.xlsx;*.xlsx|Excel (*.xls,*.xls)|*.xls;*.xls";
                System.Windows.Forms.DialogResult result = openFileDialog1.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    FilePath = openFileDialog1.FileName;
                    ExcelData = ReadExcel(FilePath);
                }
            });



            ConnectCommand = new DelegateCommand(async (param) =>
            {
                try
                {
                    var settingService = new SettingService();
                    var settingsList = await settingService.GetAll();

                    if (settingsList == null || !settingsList.Any())
                    {
                        System.Windows.MessageBox.Show("No settings found in the database. Please ensure the settings are configured.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    // Loop through each printer setting in the list
                    foreach (var printer in settingsList)
                    {
                        try
                        {
                            string ipAddress = printer.IpAddress;
                            int port = printer.Port;
                            string printerName = printer.PName;

                            // Show a message box indicating which printer we are connecting to
                            System.Windows.MessageBox.Show($"Connecting to Printer: {printerName} (IP: {ipAddress}, Port: {port})", "Connecting", MessageBoxButton.OK, MessageBoxImage.Information);

                            // Now connect to each printer using the respective IP and Port
                            bool isConnected = _connectionService.Connect(ipAddress, port);

                            if (isConnected)
                            {
                                // Show success message
                                System.Windows.MessageBox.Show($"Successfully connected to {printerName}!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);

                                if (_connectionService.IsConnected)
                                {
                                    // Send the command after successful connection
                                    string command = "GJL<CR>"; // Sample command, replace with your actual command
                                    _connectionService.Send(command);

                                    // Receive response and parse it
                                    string response = await _connectionService.Receive();
                                    ParseTemplatesFromResponse(response);
                                }
                            }
                            else
                            {
                                // Show failure message if connection fails
                                System.Windows.MessageBox.Show($"Failed to connect to {printerName} at {ipAddress}:{port}.", "Connection Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                        }
                        catch (Exception innerEx)
                        {
                            // Handle individual printer connection errors
                            System.Windows.MessageBox.Show($"Error while connecting to {printer.PName}: {innerEx.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Handle overall connection errors
                    System.Windows.MessageBox.Show($"Error while connecting to the server: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            });

        }


        private void ParseTemplatesFromResponse(string response)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(response)) return;

                string[] parts = response.Split('|');

                // Validate that we have a correct response format
                if (parts.Length < 3) return;

                // The templates are located from index 2 onwards
                var templates = parts.Skip(2).Take(parts.Length - 3); // Skip "JBL" and the count, and exclude the <CR>

                // Clear any existing templates and add the new ones
                Templates.Clear();
                foreach (var template in templates)
                {
                    Templates.Add(template);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error parsing response: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private DataTable ReadExcel(string filePath)
        {
            try
            {
                var workbook = new XLWorkbook(filePath);
                var worksheet = workbook.Worksheet(1);
                return worksheet.RangeUsed().AsTable().AsNativeDataTable();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Failed to read Excel file: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return null;
            }
        }

        //Send Start Command.
        private async void SendStartCommand()
        {
            try
            {
                if (_connectionService != null && _connectionService.IsConnected)
                {
                    if (string.IsNullOrEmpty(SelectedTemplate))
                    {
                        System.Windows.MessageBox.Show("Please select a template before starting", "Template Required", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                    string Startcommand = $"SST|1|<CR>";
                    string Selcommand = $"SEL|{SelectedTemplate}|<CR>";

                    _connectionService.Send(Selcommand);

                    // wait for ACK
                    string response = await _connectionService.Receive();
                    if (response == "ACK")
                    {
                        _connectionService.Send(Startcommand);
                        // Set the flag to true when the Start command is sent successfully
                        IsStartCommandSent = true;
                    }

                    // Disable Start button and enable Stop button
                    IsStartButtonEnabled = false;
                    IsStopButtonEnabled = true;
                    //System.Windows.MessageBox.Show("Printer Start.", "Send the Data to Printer to print.", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    System.Windows.MessageBox.Show("Please connect to the server before starting the printer.", "Connection Required", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Failed to send command: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private async void SendStopCommand()
        {
            try
            {
                if (_connectionService != null && _connectionService.IsConnected)
                {
                    string command = "SST|4|<CR>";
                    _connectionService.Send(command);

                    // Fetch folder path from the database
                    var settingService = new SettingService();
                    var settingsList = await settingService.GetAll();

                    if (settingsList == null || !settingsList.Any())
                    {
                        System.Windows.MessageBox.Show("No settings found in the database. Please ensure the settings are configured.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    // Assuming you want to use the first setting in the list
                    var selectedSetting = settingsList.FirstOrDefault();
                    if (selectedSetting == null || string.IsNullOrEmpty(selectedSetting.ExcelPath))
                    {
                        System.Windows.MessageBox.Show("Folder path not found in the database. Please ensure the database contains valid settings.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    string folderPath = selectedSetting.ExcelPath;

                    if (!Directory.Exists(folderPath))
                    {
                        System.Windows.MessageBox.Show($"Directory {folderPath} does not exist.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    // Generate the filename based on the current date and time
                    string fileName = $"Data_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                    string newFilePath = Path.Combine(folderPath, fileName);

                    // Store the selected data in the new file
                    StoreDataInNewFile(newFilePath);

                    // Disable Stop button and enable Start button
                    IsStartButtonEnabled = true;
                    IsStopButtonEnabled = false;

                    RemoveSelectedDataFromOriginal();
                }
                else
                {
                    System.Windows.MessageBox.Show("Please connect to the server before starting the printer.", "Connection Required", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Failed to send command: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        //Send Excel Data.

        private int currentRowIndex = 0;
        private async void SendRowToServer()
        {
            if (ExcelData == null || ExcelData.Rows.Count == 0)
            {
                System.Windows.MessageBox.Show("No data to send. Please load an Excel file first.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!_connectionService.IsConnected)
            {
                System.Windows.MessageBox.Show("Please connect to the server before sending data.", "Connection Required", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!IsStartCommandSent)
            {
                System.Windows.MessageBox.Show("Please start the printer by pressing the Start button before sending data.", "Start Command Required", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                if (!ExcelData.Columns.Contains("Status"))
                {
                    ExcelData.Columns.Add("Status", typeof(string)); // Add a Status column if not present
                }

                while (currentRowIndex < ExcelData.Rows.Count)
                {
                    DataRow currentRow = ExcelData.Rows[currentRowIndex];

                    // Format the row as per the required protocol
                    string message = FormatRowForServer(currentRow);

                    // Send the data to the server
                    _connectionService.Send(message);

                    // Wait for the PRC<CR> acknowledgment
                    string response = await _connectionService.Receive();  // Await the server response


                    if (response == "PRC")
                    {
                        // Increase print count
                        PrintedRowCount++;

                        currentRow["Status"] = "Acknowledged";

                        // Refresh the DataGrid
                        ExcelData.AcceptChanges();

                        currentRowIndex++;  // Only move to the next row if we received the correct acknowledgment
                    }
                    else
                    {
                        System.Windows.MessageBox.Show("Unexpected response from server: " + response, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        break;
                    }
                }

                if (currentRowIndex >= ExcelData.Rows.Count)
                {
                    System.Windows.MessageBox.Show("All rows have been sent.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error sending row: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        // Row format
        private string FormatRowForServer(DataRow row)
        {
            // Format: JDI|VAR1=C1|VAR2=C2|VAR3=C3|<CR>
            StringBuilder formattedRow = new StringBuilder("JDI");
            for (int i = 0; i < row.Table.Columns.Count; i++)
            {
                formattedRow.Append($"|VAR{i + 1}={row[i]}");
            }
            formattedRow.Append("|<CR>");
            return formattedRow.ToString();
        }

        private void StoreDataInNewFile(string newFilePath)
        {
            try
            {

                var newWorkbook = new XLWorkbook();
                var newWorksheet = newWorkbook.AddWorksheet("StoredData");

                // Assuming "Status" column is used to highlight rows
                var selectedRows = ExcelData.Select("Status = 'Acknowledged'");

                if (selectedRows.Length > 0)
                {
                    // Create a DataTable for the selected rows
                    var selectedDataTable = selectedRows.CopyToDataTable();

                    // Insert the DataTable into the worksheet starting from cell A1
                    var range = newWorksheet.Cell(1, 1).InsertData(selectedDataTable);

                    // Save the workbook to the specified file path
                    newWorkbook.SaveAs(newFilePath);

                    System.Windows.MessageBox.Show($"Data saved to {newFilePath}", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    System.Windows.MessageBox.Show("No highlighted data found to store.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Failed to store data in new file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        // Remove the selected data from the original ExcelData DataTable
        private void RemoveSelectedDataFromOriginal()
        {
            try
            {

                // Assuming the "Status" column is used to identify highlighted rows
                var rowsToRemove = ExcelData.Select("Status = 'Acknowledged'").ToList();

                foreach (var row in rowsToRemove)
                {
                    ExcelData.Rows.Remove(row);
                }

                // Refresh the DataGrid or any UI element
                ExcelData.AcceptChanges();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Failed to remove selected data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private async void FetchAllSettings()
        {
            var settingService = new SettingService();
            var settingsList = await settingService.GetAll();

            if (settingsList != null && settingsList.Any())
            {
                Settings = new ObservableCollection<Settings>(settingsList);

            }

        }
    }
}