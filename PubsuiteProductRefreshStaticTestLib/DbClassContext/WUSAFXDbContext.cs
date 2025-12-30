using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PubsuiteProductRefreshStaticTestLib.Model;
using KeyVaultManagementLib;

namespace PubsuiteProductRefreshStaticTestLib.DbClassContext
{
    public class WUSAFXDbContext : DbContext
    {
        public WUSAFXDbContext()
        {
            Database.Connection.ConnectionString = KeyVaultAccess.GetServicingKVSecret("WUSAFXConnString");
        }
        public DbSet<TCPU> TCPUs { get; set; }
        public DbSet<TOperatingSystem> TOperatingSystems { get; set; }
        public DbSet<TNETSKU> TNetSkus { get; set; }
        public DbSet<TCategoriesMapping> TCategoriesMappings { get; set; }
        public DbSet<TPropertyMapping> TPropertyMappings { get; set; }
        public DbSet<TPrerequisitesMapping> TPrerequisitesMappings { get; set; }
        public DbSet<TProductRefreshPrerequisitesMapping> TProductRefreshPrerequisitesMappings { get; set; }
        public DbSet<TTestCaseInfo> TTestcaseInfos { get; set; }
        public DbSet<TFixedUpdate> TFixedUpdates { get; set; }
        public DbSet<TAdditionalChildUpdate> TAdditionalChildUpdates { get; set; }
        public DbSet<TApplicabilityRules> TApplicabilityRulesCollection { get; set; }
        public DbSet<TMSI> TMSIs { get; set; }
        public DbSet<TReleaseType> TReleaseTypes { get; set; }
        public DbSet<TCsidl> TCsidls { get; set; }
        public DbSet<TDetectoid> TDetectoids { get; set; }
        public DbSet<TLocalizedProperty> TLocalizedProperties { get; set; }
    }
}
