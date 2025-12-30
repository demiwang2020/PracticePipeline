using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogAnalyzer
{
    //public class StepResult
    //{
    //    public int Index { get; set; }
    //    public string Name { get; set; }
    //    public bool Result { get; set; }
    //    public string FailureReason { get; set; }
    //}

    public class Result
    {
        public bool OverallResult { get; set; }
        public string FailReason { get; set; }
    }
}
