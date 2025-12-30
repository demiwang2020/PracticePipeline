using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ScorpionDAL
{
    public class Context
    {
        public List<TMatrix> ListTestMatrix { get; set; }
        public List<TestSenario> ListTestSenario { get; set; }

        //Setup Test specific
        public List<TTestMatrix> ListSetupTestMatrix { get; set; }

//        public int PatchFileID { get; private set; }

        public string TestFramework { get; set; }
        public string FileName { get; set; }

        public Context()
        {
            ListTestMatrix = new List<TMatrix>();
            ListTestSenario = new List<TestSenario>();
        }

        //public bool IsContextInUse(string strContextName)
        //{
        //    foreach (TestSenario objTestSenario in TestCase)
        //    {
        //        if (objTestSenario.ContextBlockName.Equals(strContextName))
        //            return true;
        //    }
        //    return false;
        //}

    }
}
