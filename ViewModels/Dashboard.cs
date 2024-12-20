using BaseApp.Models;
using ClosedXML.Excel;
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


namespace BaseApp.ViewModels
{
    public class Dashboard : ViewModelBase
    {
     
        public ICommand OpenFileDialog { get; set; }
        public ICommand ConnectCommand { get; set; }
        public ICommand StartCommand { get; set; }
        public ICommand StopCommand { get; set; }
        public ICommand SendCommand { get; }

        readonly string CRLF = "" + ((char)13) + ((char)10);
        readonly string CR = "" + ((char)13);
        readonly string LF = "" + ((char)10);

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

        private ObservableCollection<Printer> _printer;
        public ObservableCollection<Printer> PrinterList
        {
            get { return _printer; }
            set
            {
                _printer = value;
                OnPropertyChanged(nameof(PrinterList));
            }
        }
        private Printer selectedPrinter;
        public Printer SelectedPrinter
        {
            get { return selectedPrinter; }
            set
            {
                selectedPrinter = value;
                OnPropertyChanged(nameof(SelectedPrinter));
            }
        }

        public Dashboard()
        {
            PrinterList = new ObservableCollection<Printer>();

            FetchAllPrinters();

            selectedPrinter.Templates = new ObservableCollection<string>();
            StartCommand = new DelegateCommand(async (Printer) =>
            {
                SendStartCommand(Printer);
            });
            StopCommand = new DelegateCommand(async (Printer) =>
            {
                SendStopCommand(Printer);
            }); //new RelayCommand(SendStopCommand);
            SendCommand = new DelegateCommand(async (Printer) =>
            {
                SendRowToServer(Printer);
            }); //new RelayCommand(SendRowToServer);


            OpenFileDialog = new DelegateCommand(async (currentPrinter) =>
            {
                var openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
                openFileDialog1.Filter = "CSV (*.csv,*.csv)|*.csv;*.csv|Excel (*.xlsx,*.xlsx)|*.xlsx;*.xlsx|Excel (*.xls,*.xls)|*.xls;*.xls";
                System.Windows.Forms.DialogResult result = openFileDialog1.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    Printer printer = (Printer)currentPrinter;
                    Printer selectedPrinter = PrinterList.FirstOrDefault(p => p.Port == printer.Port);
                    selectedPrinter.FilePath = openFileDialog1.FileName;
                    selectedPrinter.ExcelData = ReadExcel(selectedPrinter.FilePath);
                }
            });


            ConnectCommand = new DelegateCommand(async (param) =>
            {
                try
                {

                    Printer printer = (Printer)param;
                    Printer selectedPrinter = PrinterList.FirstOrDefault(p => p.Port == printer.Port);
                    //var settingService = new SettingService();
                    //var settingsList = await settingService.GetAll();

                    //if (settingsList == null || !settingsList.Any())
                    //{
                    //    System.Windows.MessageBox.Show("No settings found in the database. Please ensure the settings are configured.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    //    return;
                    //}

                    // Loop through each printer setting in the list
                    // foreach (var printer in settingsList)
                    {
                        try
                        {
                            string ipAddress = selectedPrinter.IpAddress;
                            int port = selectedPrinter.Port;
                            string printerName = selectedPrinter.PName;

                            System.Windows.MessageBox.Show($"Connecting to Printer: {printerName} (IP: {ipAddress}, Port: {port})", "Connecting", MessageBoxButton.OK, MessageBoxImage.Information);

                            bool isConnected = selectedPrinter.SocketConnection.Connect(ipAddress, port);

                            if (isConnected)
                            {
                                System.Windows.MessageBox.Show($"Successfully connected to {printerName}!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);

                                if (selectedPrinter.SocketConnection.IsConnected)
                                {
                                    // Send the command after successful connection
                                    string command = "GJL" + CR;
                                    selectedPrinter.SocketConnection.Send(command);

                                    // Receive response and parse it
                                    string response = await selectedPrinter.SocketConnection.Receive();
                                    ParseTemplatesFromResponse(response, ref selectedPrinter);
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
        private void ParseTemplatesFromResponse(string response, ref Printer printerParameter)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(response)) return;

                string[] parts = response.Split('|');

                if (parts.Length < 3) return;

                var templates = parts.Skip(2).Take(parts.Length - 3);
                int port = printerParameter.Port;
                Printer selectedPrinter = PrinterList.FirstOrDefault(p => p.Port == port);

                if (selectedPrinter != null)
                {
                    if (selectedPrinter.Templates == null)
                    {
                        selectedPrinter.Templates = new ObservableCollection<string>();
                    }
                    //selectedPrinter.Templates.Clear();
                    List<string> selTemplates = new List<string>();
                    foreach (var template in templates)
                    {
                        selTemplates.Add(template);
                    }
                    selectedPrinter.Templates = new ObservableCollection<string>(selTemplates);

                    //System.Windows.MessageBox.Show("Templates updated successfully.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
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
        private async void SendStartCommand(object printer)
        {
            try
            {
                Printer selectedPrinter = (Printer)printer;
               // Printer start = PrinterList.FirstOrDefault(p => p.Port == selectedPrinter.Port);

                if (selectedPrinter.SocketConnection != null && selectedPrinter.SocketConnection.IsConnected)
                {
                    if (string.IsNullOrEmpty(selectedPrinter.SelectedTemplate))
                    {
                        System.Windows.MessageBox.Show("Please select a template before starting", "Template Required", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                        string Startcommand = $"SST|1|" + CR;
                        string Selcommand = $"SEL|{selectedPrinter.SelectedTemplate}|" + CR;

                        selectedPrinter.SocketConnection.Send(Selcommand);
                        // wait for ACK
                        string response = await selectedPrinter.SocketConnection.Receive();
                        if (response == "ACK")
                        {
                            selectedPrinter.SocketConnection.Send(Startcommand);
                            // Set the flag to true when the Start command is sent successfully
                            IsStartCommandSent = true;
                        }
                        // Disable Start button and enable Stop button
                        selectedPrinter.IsStartButtonEnabled = false;
                        selectedPrinter.IsStopButtonEnabled = true;
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
        private async void SendStopCommand(object printer)
        {
            try
            {
                Printer selectedPrinter = (Printer)printer;
                if (selectedPrinter.SocketConnection != null && selectedPrinter.SocketConnection.IsConnected)
                {
                    string command = "SST|4|" + CR;
                    selectedPrinter.SocketConnection.Send(command);

                    // Fetch folder path from the database
                    //var settingService = new SettingService();
                    //var settingsList = await settingService.GetAll();

                    //if (settingsList == null || !settingsList.Any())
                    //{
                    //    System.Windows.MessageBox.Show("No settings found in the database. Please ensure the settings are configured.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    //    return;
                    //}

                    //var selectedSetting = settingsList.FirstOrDefault();
                    //if (selectedSetting == null || string.IsNullOrEmpty(selectedSetting.ExcelPath))
                    //{
                    //    System.Windows.MessageBox.Show("Folder path not found in the database. Please ensure the database contains valid settings.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    //    return;
                    //}
                    string folderPath = selectedPrinter.ExcelPath;

                    if (!Directory.Exists(folderPath))
                    {
                        System.Windows.MessageBox.Show($"Directory {folderPath} does not exist.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                    // Generate the filename based on the current date and time
                    string fileName = $"Data_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                    string newFilePath = Path.Combine(folderPath, fileName);
                    // Store the selected data in the new file
                    StoreDataInNewFile(newFilePath , selectedPrinter);
                    // Disable Stop button and enable Start button
                    selectedPrinter.IsStartButtonEnabled = true;
                    selectedPrinter.IsStopButtonEnabled = false;

                    RemoveSelectedDataFromOriginal(selectedPrinter);
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
        private async void SendRowToServer(object printer)
        {
            Printer selectedPrinter = (Printer)printer;
            if (selectedPrinter.ExcelData == null || selectedPrinter.ExcelData.Rows.Count == 0)
            {
                System.Windows.MessageBox.Show("No data to send. Please load an Excel file first.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (!selectedPrinter.SocketConnection.IsConnected)
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
                if (!selectedPrinter.ExcelData.Columns.Contains("Status"))
                {
                    selectedPrinter.ExcelData.Columns.Add("Status", typeof(string)); // Add a Status column if not present
                }
                while (currentRowIndex < selectedPrinter.ExcelData.Rows.Count)
                {
                    DataRow currentRow = selectedPrinter.ExcelData.Rows[currentRowIndex];

                    string message = FormatRowForServer(currentRow);
                    selectedPrinter.SocketConnection.Send(message);
                    string response = await selectedPrinter.SocketConnection.Receive();  // Await the server response
                    if (response == "PRC")
                    {
                        selectedPrinter.PrintedRowCount++;
                        currentRow["Status"] = "Acknowledged";

                        selectedPrinter.ExcelData.AcceptChanges();
                        currentRowIndex++;  // Only move to the next row if we received the correct acknowledgment
                    }
                    else
                    {
                        System.Windows.MessageBox.Show("Unexpected response from server: " + response, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        break;
                    }
                }

                if (currentRowIndex >= selectedPrinter.ExcelData.Rows.Count)
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
            formattedRow.Append("|" + CR);
            return formattedRow.ToString();
        }
        private void StoreDataInNewFile(string newFilePath , Printer printer)
        {
          //  Printer selectedPrinter = (Printer)printer;
            Printer selectedPrinter = PrinterList.FirstOrDefault(p => p.Port == printer.Port);
            try
            {
                var newWorkbook = new XLWorkbook();
                var newWorksheet = newWorkbook.AddWorksheet("StoredData");

                // Assuming "Status" column is used to highlight rows
                var selectedRows = selectedPrinter.ExcelData.Select("Status = 'Acknowledged'");

                if (selectedRows.Length > 0)
                {
                    var selectedDataTable = selectedRows.CopyToDataTable();

                    var range = newWorksheet.Cell(1, 1).InsertData(selectedDataTable);

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
        private void RemoveSelectedDataFromOriginal(Printer printer)
        {
            Printer selectedPrinter = PrinterList.FirstOrDefault(p => p.Port == printer.Port);
        
            try
            {
                var rowsToRemove = selectedPrinter.ExcelData.Select("Status = 'Acknowledged'").ToList();

                foreach (var row in rowsToRemove)
                {
                    selectedPrinter.ExcelData.Rows.Remove(row);
                }

                selectedPrinter.ExcelData.AcceptChanges();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Failed to remove selected data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private async void FetchAllPrinters()
        {
            var settingService = new SettingService();
            var settingsList = await settingService.GetAll();

            if (settingsList != null && settingsList.Any())
            {
                var printerList = settingsList.Select(s => new Printer
                {
                    PName = s.PName,
                    IpAddress = s.IpAddress,
                    Port = s.Port,
                }).ToList();

                PrinterList = new ObservableCollection<Printer>(printerList);
            }

        }
    }
}
