using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NETCoreMUStaticLib.Model;
using KeyVaultManagementLib;

namespace NETCoreMUStaticLib.DbClassContext
{
    public class NetCoreWUSAFXDbContext : DbContext
    {
        public NetCoreWUSAFXDbContext()
        {
            Database.Connection.ConnectionString = KeyVaultAccess.GetServicingKVSecret("NetCoreWUSAFXConnString");
        }
        public DbSet<TTestCaseInfo> TTestcaseInfos { get; set; }
        public DbSet<TNETCoreConfigMapping> TNETCoreConfigMappings { get; set; }
        public DbSet<TDetectoid> TDetectoids { get; set; }
        public DbSet<TTestedUpdate> TTestedUpdates { get; set; }
    }
}
