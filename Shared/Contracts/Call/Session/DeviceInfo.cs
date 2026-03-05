using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.Call.Session
{
    public class DeviceInfo
    {
        public string DeviceType { get; set; } // Mobile, Web, Desktop
        public string Os { get; set; }
        public string Browser { get; set; } // للـ Web
        public string AppVersion { get; set; }
    }
}
