using NETCoreMUStaticLib.Model;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NETCoreMUStaticLib.DbClassContext
{
    public class DtgpatchtestContext : DbContext
    {
        public DbSet<TNETCoreMUBundle> BundleInfos { get; set; }
        public DbSet<TNETCoreMUMSI> MSIInfos { get; set; }
    }
}
