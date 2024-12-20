using BaseApp.ViewModels;
using DocumentFormat.OpenXml.Wordprocessing;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WpfHelpers;

namespace BaseApp.Models
{
    public class Printer : ViewModelBase
    {
        private string pName;
        public string PName
        {
            get { return pName; }
            set { pName = value; OnPropertyChanged(nameof(PName)); }
        }

        private int id;
        public int Id
        {
            get { return id; }
            set { id = value; OnPropertyChanged(nameof(Id)); }
        }

        private string ipAddress;
        public string IpAddress
        {
            get { return ipAddress; }
            set { ipAddress = value; OnPropertyChanged(nameof(IpAddress)); }
        }

        private int port;
        public int Port
        {
            get { return port; }
            set { port = value; OnPropertyChanged(nameof(Port)); }
        }

        private string excelPath;
        public string ExcelPath
        {
            get { return excelPath; }
            set { excelPath = value; OnPropertyChanged(nameof(ExcelPath)); }
        }

        private string filePath;
        public string FilePath
        {
            get { return filePath; }
            set { filePath = value; OnPropertyChanged(nameof(FilePath)); }
        }
        private DataTable excelData;

        public DataTable ExcelData
        {
            get { return excelData; }
            set { excelData = value; OnPropertyChanged(nameof(ExcelData)); }
        }

        private ConnectionService socketConnection;
        public ConnectionService SocketConnection
        {
            get { return socketConnection; }
            set { socketConnection = value; OnPropertyChanged(nameof(SocketConnection)); }
        }

        private ObservableCollection<string> templates;
        public ObservableCollection<string> Templates
        {
            get { return templates; }
            set
            {
                templates = value;
                OnPropertyChanged(nameof(Templates));
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

        public Printer()
        {
            socketConnection = new ConnectionService();
        }

    }
}
