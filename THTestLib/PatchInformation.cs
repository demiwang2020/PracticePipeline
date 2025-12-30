using Helper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace THTestLib
{
    class PatchInformation
    {
        public Architecture Arch { get; set; }
        public string PatchLocation { get; set; }
        public string PatchVersion { get; set; }
        public string LCUPatchPath { get; set; }
        public DataTable ActualBinaries { get; set; }
        public string ExtractLocation { get; set; }
        public string LCUExtractLocation { get; set; }
    }
}
