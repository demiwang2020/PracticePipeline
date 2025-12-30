using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace THTestLib
{
    class THTestResults
    {
        public bool Result; //Overall result
        public bool HasWarning; //Indicate if there are warning results
        public List<DataTable> ResultDetails; //results of each case
        public List<bool> ResultDetailSummaries; //true or false results of each case

        public THTestResults()
        {
            Result = true;
            HasWarning = false;
            ResultDetails = new List<DataTable>();
            ResultDetailSummaries = new List<bool>();
        }
    }
}
