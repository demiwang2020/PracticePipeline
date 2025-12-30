using Azure.Core;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using KeyVaultManagementLib;
using NetFxServicing.SASPackageManagerLib;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace THTestLib
{
    public class WinCloudPackage
    {
        private GetPackage getWindowsPackage;
        public WinCloudPackage()
        {
            //string keyVaulUri = ConfigurationManager.AppSettings["DevDivServicingKeyVaultUri"];
            string keyVaulUri = ConfigurationManager.AppSettings["GoFxKeyVaultUri"];
            string certificateName = "netfxservicing-onecert";
            var credOptions = new DefaultAzureCredentialOptions()
            {
                ManagedIdentityClientId = ConfigurationManager.AppSettings["ManagedIdentityClientId"]
            };
            var credentials = new DefaultAzureCredential(credOptions);
            //string clientId = ConfigurationManager.AppSettings["ManagedIdentityClientId"];
            //TokenCredential credentials = new ManagedIdentityCredential(clientId);
            SecretClient client = new SecretClient(new Uri(keyVaulUri), credentials);
            //string clientid = client.GetSecret("DotNetServicingClientID").Value.Value;
            string clientid = KeyVaultAccess.GetGoFXKVSecret("DotNetServicingClientID", GetManagedId());
            getWindowsPackage = new GetPackage(clientid, KeyVaultAccess.GetGoFXKVCertificate(ConfigurationManager.AppSettings["DotNetServicingClientCert"], GetManagedId()));
            //getWindowsPackage = new GetPackage(clientid, KeyVaultAccess.GetGoFXKVCertificate(certificateName));

        }

        public static string GetManagedId()
        {
            if (Regex.Match(Environment.GetEnvironmentVariable("COMPUTERNAME"), "DotNetPatchTest", RegexOptions.IgnoreCase).Success)
            {
                return ConfigurationManager.AppSettings["gofxservinfra01ManagedId"];
            }
            return string.Empty;
        }

        public List<string> DownloadWindowsPackages(int jobid, PackageType packageType, string destinationpath)
        {
            var restults = getWindowsPackage.GetPackagesForJob(jobid, packageType, destinationpath);
            return restults;
        }
        public void DownloadJobArtifact(int jobid, string artifactname, string kbnumber, string destination, string targetarch=null)
        {
            Architecture archtotarget = Architecture.All;
            if (!String.IsNullOrEmpty(targetarch))
            {
                if (Enum.TryParse(targetarch, out Architecture arch))
                {
                    archtotarget = arch;
                }
            }
            getWindowsPackage.DownloadJobArtifact(jobid, artifactname, kbnumber, destination, archtotarget);
        }
    }
}
