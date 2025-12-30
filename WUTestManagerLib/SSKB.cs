using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WUTestManagerLib
{
    public class SSKB
    {
        private string kbnum;
        private Guid guid;
        public string KBNumber
        {
            get { return kbnum; }
        }
        public Guid GUID
        {
            get { return guid; }
        }
        public SSKB(string kbnum, Guid guid)
        {
            this.kbnum = kbnum;
            this.guid = guid;
        }
    }
}
