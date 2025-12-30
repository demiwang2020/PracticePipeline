using PubsuiteStaticTestLib.DbClassContext;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PubsuiteStaticTestLib
{
    public class ReleaseTypeDetector
    {
        public static int ParseReleaseTypeFromTitle(string title)
        {
            using (var db = new WUSAFXDbContext())
            {
                foreach (var p in db.TReleaseTypes)
                {
                    if (!String.IsNullOrEmpty(p.Keyword) && title.Contains(p.Keyword))
                    {
                        return p.ID;
                    }
                }

                return db.TReleaseTypes.First().ID;
            }
        }

        // Keep this same as DB
        public static bool IsSecurityRelease(int releaseType)
        {
            // Preview, Catalog(Preview) and Promotion
            if (releaseType == 4 || releaseType == 7 || releaseType == 8)
                return false;

            return true;
        }

        public static int Name2ID(string reName)
        {
            using (var db = new WUSAFXDbContext())
            {
                return db.TReleaseTypes.Where(p => p.Name.Equals(reName)).First().ID;
            }
        }
    }
}
