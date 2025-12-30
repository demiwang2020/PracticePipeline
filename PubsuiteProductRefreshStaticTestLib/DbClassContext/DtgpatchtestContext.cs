using PubsuiteProductRefreshStaticTestLib.Model;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PubsuiteProductRefreshStaticTestLib.DbClassContext
{
    public class DtgpatchtestContext : DbContext
    {
        public DbSet<SANFileLocation> FileLocations { get; set; }
    }
}
