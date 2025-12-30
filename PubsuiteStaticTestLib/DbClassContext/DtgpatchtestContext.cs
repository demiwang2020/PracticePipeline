using PubsuiteStaticTestLib.Model;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KeyVaultManagementLib;
using System.Configuration;

namespace PubsuiteStaticTestLib.DbClassContext
{
    public class DtgpatchtestContext : DbContext
    {
        public DtgpatchtestContext()
        {
            //Database.Connection.ConnectionString = KeyVaultAccess.GetGoFXKVSecret("PatchTestDBConnString", UpdateHelper.UpdateBuilder.GetManagedId());
            Database.Connection.ConnectionString = "Data Source=dotnetpatchtest;Initial Catalog=PatchTestDatabase;Integrated Security=True;Connect Timeout=60;";
       
        }
        public DbSet<SANFileLocation> FileLocations { get; set; }
    }
}
