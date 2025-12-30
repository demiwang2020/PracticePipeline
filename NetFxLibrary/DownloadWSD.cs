using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NetFxSetupLibrary
{
    class DownloadWSD
    {
        public string DownloadAndInstallPatches(string osBuildNumber, string release, string sku, string arch)
        {
            MTPPatches patches = GetPatchInfo(GenerateURL(osBuildNumber, release, sku)).Result;
            List<Packages> packages = patches.Packages.ToList();
            Packages package = packages.OrderByDescending(p => p.UpdateType).First();
            WebClient client = new WebClient();

            string workFolder = ConfigurationManager.AppSettings["WorkFolder"];

            var packageForCurrentArch = package.PackageSASURLs.Where(x => x.Arch.ToLowerInvariant() == arch.ToLowerInvariant()).FirstOrDefault();

            string packageName = packageForCurrentArch.PackageName;
            string packageUri = packageForCurrentArch.SASUri;
            if (!Directory.Exists(workFolder))
                Directory.CreateDirectory(workFolder);
            client.DownloadFile(packageUri, $"{workFolder}\\{packageName}");
            client.Dispose();

            return $"{workFolder}\\{packageName}";
        }

        public static async Task<MTPPatches> GetPatchInfo(string uri)
        {
            int retryCount = 3;
            do
            {
                try
                {
                    HttpClient client = new HttpClient();
                    string responseBody = await client.GetStringAsync(uri);
                    return JsonConvert.DeserializeObject<MTPPatches>(responseBody);
                }
                catch (Exception ex)
                {
                    retryCount--;
                }
            } while (retryCount > 0);
            return null;
        }

        public static string GenerateURL(string osBuildNumber, string release, string dotnetFrameworkVersions)
        {
            return $"https://abs.corp.microsoft.com/services/api/partner/ServicingUpdatesAndMedia?osbuildnumber={osBuildNumber}&release={release}&ishotpatchable=false&allowPackageWithoutPrereleasedMedia=true&dotnetframeworkversions={dotnetFrameworkVersions}";
        }

        public class MTPPatches
        {
            public Packages[] Packages { get; set; }
        }
    }



    public class Packages
    {
        public string KB { get; set; }
        public string PackageVersion { get; set; }
        public string UpdateType { get; set; }
        public PackageSASURLs[] PackageSASURLs { get; set; }
    }

    public class PackageSASURLs
    {
        public string PackageName { get; set; }
        public string Arch { get; set; }
        public string SASUri { get; set; }
        public string SASValidUptoUTC { get; set; }
    }

}
