using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FxbvtLibrary
{
    public class RunInfo
    {
        public string Team {  get; set; }
        public string Title { get; set; }
        public string ID { get; set; }
        public float TestsPassRate { get; set; }
        public int TestsExecuted { get; set; }
        public int TestsPassed { get; set; }
        public int TestsFailed { get; set; }
        public int UnanalyzedFailures { get; set; }
        public float TestsScenariosPassRate { get; set; }
        public int TestsScenariosExecuted { get; set; }
        public int TestsScenariosPassed { get; set; }
        public int TestsScenariosFailed { get; set; }
        public float CompletionRate { get; set; }
        public int TestsTotal { get; set; }
        public int ScenariosTotal { get; set; }
        public string ScheduleID { get; set; }
    }
}
