using Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NETCoreMUStaticLib
{
    class InnerData
    {
        public string Title;
        public string UpdateID;
        public bool IsSecurityRelease;
        public bool IsServerBundle;
        public bool IsAUBundle;
        public Architecture Arch;
        public string ReleaseNumber;
        public string MajorRelease;
        public string ReleaseDate;

        public Dictionary<string, string> Parameters = new Dictionary<string, string>();
    }
}
