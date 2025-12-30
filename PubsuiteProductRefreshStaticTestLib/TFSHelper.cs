using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.TeamFoundation.WorkItemTracking.Client;
using PubsuiteProductRefreshStaticTestLib.DbClassContext;

namespace PubsuiteProductRefreshStaticTestLib
{
    public class TFSHelper
    {
        private static string _tfsuri;

        public static string TFSURI
        {
            get
            {
                if (String.IsNullOrEmpty(_tfsuri))
                {
                    using (var db = new WUSAFXDbContext())
                    {
                        _tfsuri = db.TPropertyMappings.Where(p => p.Name == "TFSURI").Single().MappedContent;
                    }
                }

                return _tfsuri;
            }
        }
    }
}
