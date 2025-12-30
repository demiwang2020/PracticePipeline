using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PubsuiteProductRefreshStaticTestLib.UpdateHelper
{
    public class UpdateRoqData
    {
        public bool IsCritical;
        public bool IsTestOnly;
        public string Owner;

        public UpdateRoqData()
        {
            IsCritical = false;
            IsTestOnly = false;
        }
    }
}
