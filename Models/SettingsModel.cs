using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaseApp.Models
{
    public class SettingsModel
    {
        public int Id { get; set; }

        private string _ipAddress;
        public string IpAddress
        {
            get => _ipAddress;
            set
            {
                _ipAddress = value;
            }
        }
        private int _port;
        public int Port
        {
            get => _port;
            set
            {
                _port = value;
            }
        }
        private string _ExcelPath;
        public string ExcelPath
        {
            get
            {
                return this._ExcelPath;
            }
            set
            {
                this._ExcelPath = value;
            }
        }
        private string pName;
        public string PName
        {
            get { return pName; }
            set { pName = value; }
        }

    }
}
