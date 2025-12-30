using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WUTestManagerLib
{
    public class KBGroup
    {
        //I have no idea why these aren't visible publically 
        private string kb;
        private List<KBToTest> groupkbs;
        public List<KBToTest> GroupKBs
        {
            get { return groupkbs; }
            set { groupkbs = value; }
        }
        public string KB
        {
            get { return kb; }
        }

        public KBGroup(string kb)
        {
            this.kb = kb.Replace("KB", "");
            groupkbs = new List<KBToTest>();
        }
    }
}
