using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;
using RMIntegration;
using RMIntegration.RMService;
using File = System.IO.File;
using System.Threading;
using Helper;
using NetFxServicing.DropHelperLib;
using Microsoft.TeamFoundation.WorkItemTracking.Client;
namespace DataAggregator
{
    public class DataBuilder
    {
        public const string TFSServerURI = "https://vstfdevdiv.corp.microsoft.com/DevDiv";
        private readonly int _workItemId;
        private Architecture _arch;
        private readonly List<Architecture> _availableArchitectures;
        private readonly Patch _rmPatch;
        private readonly ExtractedPatches _extractedPatches;
        private string _destinationSharePath; // this is set only the first time;

        private string _custom1Data;
        private string _baseBuildNumber;
        private Dictionary<Architecture, string> _lcuPatchLocations;
        private List<string> packagePaths;
        //private string 

        public DataBuilder(int workItem)
        {
            _destinationSharePath = string.Empty;
            _workItemId = workItem;

            RMSvcMethods RMSvcM = new RMSvcMethods(_workItemId);
            _baseBuildNumber = RMSvcM.GetBaseBuildNumber();
            _custom1Data = RMSvcM.GetCustom1Data();
            var lcukb = RMSvcM.GetLCUKBNumber();
            
            //var lcukb = 1260970;
            if (lcukb > 0)
            {
                var baseLine = GetRMPatchObject(lcukb);
                Dictionary<Architecture, string> dt = new Dictionary<Architecture, string>();
                //dt.Add(Architecture.X86, Path.Combine(baseLine.PatchLocationx86, baseLine.PatchNamex86));
                //dt.Add(Architecture.AMD64, Path.Combine(baseLine.PatchLocationx64, baseLine.PatchNamex64));
                dt.Add(Architecture.X86, LCUPathBuild(baseLine.KBNumber.ToString(),baseLine,Architecture.X86));
                dt.Add(Architecture.AMD64, LCUPathBuild(baseLine.KBNumber.ToString(), baseLine, Architecture.AMD64));
                if (!string.IsNullOrEmpty(baseLine.PatchLocationARM))
                    dt.Add(Architecture.ARM, Path.Combine(baseLine.PatchLocationARM, baseLine.PatchNameARM));
                if (!string.IsNullOrEmpty(baseLine.PatchLocationIA64))
                    dt.Add(Architecture.IA64, Path.Combine(baseLine.PatchLocationIA64, baseLine.PatchNameIA64));
                _lcuPatchLocations = dt;
            }

            _arch = Architecture.X86;
            _rmPatch = GetRMPatchObject();
            _availableArchitectures = SetAvailableArchitectures();
            UpdateRMObject();
            //packagePaths = DownloadPackages();
            if (!IsRefreshRedistHFR || IsCBSRefreshRedistHFR)
                _extractedPatches = SetExtractedPaths();
        }

        public string LCUPathBuild(string KbNumber,Patch patch,Architecture arch)
        {
            string path = "\\\\DotNetPatchTest\\F\\SetupTest\\ExtractLocation";
            var directories = Directory.GetDirectories(Path.Combine(path, "KB" + KbNumber, patch.SourceBuildLDR))
            .Select(Path.GetFileName)
            .Where(name => int.TryParse(name, out _))
            .Select(int.Parse)
            .OrderByDescending(num => num)
            .FirstOrDefault();
            if (arch.ToString() == "X86")
                path = Path.Combine(path, "KB" + KbNumber, patch.SourceBuildLDR, directories.ToString(), arch.ToString(),patch.PatchNamex86);
            else
                path = Path.Combine(path, "KB" + KbNumber, patch.SourceBuildLDR, directories.ToString(), arch.ToString(), patch.PatchNamex64);
            return path;
        }

        public string Custom1Data
        {
            get
            {
                return _custom1Data;
            }
        }
        public string BaseBuildNumber
        {
            get
            {
                return _baseBuildNumber;
            }
        }
        public Dictionary<Architecture, string> LCUPatchLocations //TFS WORKITEM ID, FOR patch location
        {
            get
            {
                return _lcuPatchLocations;
            }
        }

        public DataBuilder(int workItem, Architecture arch)
        {
            _destinationSharePath = string.Empty;
            _workItemId = workItem;
            _arch = arch;
            _rmPatch = GetRMPatchObject();
            _availableArchitectures = new List<Architecture> { arch };

            UpdateRMObject();

            if (!IsRefreshRedistHFR || IsCBSRefreshRedistHFR)
                _extractedPatches = SetExtractedPaths(arch);
        }

        private void UpdateRMObject()
        {
            if (_rmPatch.MetaData.TargetProduct.Equals(".NET Framework 4.5.X/4.6.X", StringComparison.InvariantCultureIgnoreCase))
            {
                _rmPatch.MetaData.TargetProduct += " RTM";
            }

            // As Redmond said, all SKU branches are moving to single branch only
            _rmPatch.IsDualBranch = false;

            // Some file names from RM are not correct, correct them with hardcode
            foreach (RMIntegration.RMService.File file in _rmPatch.Files)
            {
                if (file.FileName.Equals("regtlib.exe", StringComparison.InvariantCultureIgnoreCase))
                    file.FileName = "regtlibv12.exe";
                else if (file.FileName.Equals("VsVersion.dll", StringComparison.InvariantCultureIgnoreCase))
                    file.FileName = "wpftxt_v0400.dll";
                else if (file.FileName.Equals("sbscmp10.dll", StringComparison.InvariantCultureIgnoreCase))
                    file.FileName = "sbsnclperf.dll";
                else if (file.FileName.Equals("cvtres_clr.exe", StringComparison.InvariantCultureIgnoreCase))
                    file.FileName = "cvtres.exe";
                else if (file.FileName.Equals("cvtresui_clr.dll", StringComparison.InvariantCultureIgnoreCase))
                    file.FileName = "cvtresui.dll";
                else if (file.FileName.Equals("Placeholder.dll", StringComparison.InvariantCultureIgnoreCase))
                    file.FileName = "penimc_v0400.dll";
            }
        }

        /// <summary>
        /// Returns a list of available architectures for the current work-item.
        /// </summary>
        public List<Architecture> AvailableArchitectures
        {
            get { return _availableArchitectures; }
        }

        public bool IsRefreshRedistHFR
        {
           get
            {
                return false;
            }
        }

        public bool IsCBSRefreshRedistHFR
        {
            get
            {
                return false;
            }
        }


        /// <summary>
        /// Create SAFX run object and meta data
        /// </summary>
        /// <param name="archtecture">Specifies what architecture to filter by</param>
        /// <returns>PatchSAFX object</returns>
        public PatchSAFX GetPatchSAFXObject(Architecture archtecture)
        {
            _arch = archtecture;
            return GetPatchSAFXObject();
        }

        private PatchSAFX GetPatchSAFXObject()
        {
            PatchSAFX patchSAFX = new PatchSAFX();
            patchSAFX.Build(_rmPatch, _arch, _extractedPatches);

            //Use base build number for SAFX if it is not null
            if (!string.IsNullOrEmpty(BaseBuildNumber))
            {
                patchSAFX.BuildNumber = BaseBuildNumber;
                patchSAFX.LdrBuildNumber = BaseBuildNumber;
            }

            //Set LCU Patch Location
            if (LCUPatchLocations != null && LCUPatchLocations.ContainsKey(_arch))
                patchSAFX.LCUPatchLocation = LCUPatchLocations[_arch];

            if (Custom1Data.StartsWith("3.0;SP2"))
            {
                patchSAFX.Custom1Data = Custom1Data;
            }
            else
            {
                patchSAFX.Custom1Data = "";
            }
            return patchSAFX;
        }

        /// <summary>
        /// Create Smoke run object and meta data
        /// </summary>
        /// <param name="architecture">Specifies what architecture to filter by</param>
        /// <returns>Returns PatchSmoke object</returns>
        public PatchSmoke GetPatchSmokeObject(Architecture architecture)
        {
            _arch = architecture;
            return GetPatchSmokeObject();
        }

        private PatchSmoke GetPatchSmokeObject()
        {

            PatchSmoke patchSmoke = new PatchSmoke();
            patchSmoke.Build(_rmPatch, _arch, _extractedPatches);
            patchSmoke.TestGroupName = SetTestGroupName(patchSmoke.KbNumber);

            //Set LCU Patch Location
            if (LCUPatchLocations != null && LCUPatchLocations.ContainsKey(_arch))
                patchSmoke.LCUPatchLocation = LCUPatchLocations[_arch];

            return patchSmoke;
        }


        public PatchTargetInfo GetPatchTargetInfo(Architecture architectrue)
        {
            PatchTargetInfo targetInfo = new PatchTargetInfo();
            targetInfo.Build(_rmPatch, _arch, _extractedPatches);
            return targetInfo;
        }

        public RefreshRedistHFRInfo GetRefreshRedistHFRInfo()
        {
            if (IsRefreshRedistHFR)
            {
                RefreshRedistHFRInfo hfrInfo = new RefreshRedistHFRInfo();
                hfrInfo.Build(_rmPatch, _arch, _extractedPatches);
                return hfrInfo;
            }

            return null;
        }

        #region Meta Data Private Methods

        /// <summary>
        /// Creates a list of all available architectures for a given patch
        /// </summary>
        /// <returns>returns List of Enum Architectures based</returns>
        private List<Architecture> SetAvailableArchitectures()
        {
            List<Architecture> architectures = new List<Architecture>();

            if (!String.IsNullOrEmpty(_rmPatch.PatchNamex64))
                architectures.Add(Architecture.AMD64);

            if (!String.IsNullOrEmpty(_rmPatch.PatchNamex86))
                architectures.Add(Architecture.X86);

            if (!String.IsNullOrEmpty(_rmPatch.PatchNameIA64))
                architectures.Add(Architecture.IA64);

            if (!String.IsNullOrEmpty(_rmPatch.PatchNameARM))
                architectures.Add(Architecture.ARM);

            return architectures;
        }

        /// <summary>
        /// Interface to RMIntegration Class. Handles all communication with external data source.
        /// </summary>
        /// <returns>Returns Release Man's Patch object</returns>
        private Patch GetRMPatchObject()
        {
            RMSvcMethods rmFetch = new RMSvcMethods(_workItemId);

            //try 10 times at most if needed. try it per 30 seconds.
            int i = 0;
            while (i < 10)
            {
                try
                {
                    //rmFetch = new RMSvcMethods(_workItemId);
                    rmFetch.Populate();
                    break;
                }
                catch
                {
                    i++;
                    Thread.Sleep(30000);
                    rmFetch = new RMSvcMethods(_workItemId);
                }
            }

            if (rmFetch.PPatch == null)
                throw new ArgumentNullException(
                    String.Format("Patch object is null for Work Item: {0}. Release Man Service Error: {1}", _workItemId,
                                  rmFetch.RMServiceErrorMessage));
            return rmFetch.PPatch;
        }

        public static Patch GetRMPatchObject(int tfsID)
        {
            RMSvcMethods rmFetch = new RMSvcMethods(tfsID);

            //try 10 times at most if needed. try it per 30 seconds.
            int i = 0;
            while (i < 10)
            {
                try
                {
                    //rmFetch = new RMSvcMethods(_workItemId);
                    rmFetch.Populate();
                    break;
                }
                catch
                {
                    i++;
                    Thread.Sleep(30000);
                    rmFetch = new RMSvcMethods(tfsID);
                }
            }

            if (rmFetch.PPatch == null)
                throw new ArgumentNullException(
                    String.Format("Patch object is null for Work Item: {0}. Release Man Service Error: {1}", tfsID,
                                  rmFetch.RMServiceErrorMessage));
            return rmFetch.PPatch;
        }
        /// <summary>
        /// Set MSP path using the following logic:
        ///     - For the specified share path, detect if share path + build number exists + architecture exists
        ///     - If it does, delete directory
        ///     - then create directory and copy msp to share path
        /// </summary>
        /// <returns></returns>
        private ExtractedPatches SetExtractedPaths()
        {
            string extractedFullPath;
            string patchFullPath;
            ExtractedPatches msPs = new ExtractedPatches();
            packagePaths = DownloadPackages();
            //Extract and set target arch patch contents
            //ExtractedFullPath[arch] target MspFileName
            //PatchFullPath[arch] target MSPPath

            DoCopyToShare(Architecture.X86, out extractedFullPath, out patchFullPath);
            msPs.ExtractedFullPathX86 = extractedFullPath;
            msPs.PatchFullPathX86 = patchFullPath;


            DoCopyToShare(Architecture.AMD64, out extractedFullPath, out patchFullPath);
            msPs.ExtractedFullPathAMD64 = extractedFullPath;
            msPs.PatchFullPathAMD64 = patchFullPath;

            DoCopyToShare(Architecture.ARM, out extractedFullPath, out patchFullPath);
            msPs.ExtractedFullPathARM = extractedFullPath;
            msPs.PatchFullPathARM = patchFullPath;

            DoCopyToShare(Architecture.IA64, out extractedFullPath, out patchFullPath);
            msPs.ExtractedFullPathIA64 = extractedFullPath;
            msPs.PatchFullPathIA64 = patchFullPath;

            return msPs;
        }

        private ExtractedPatches SetExtractedPaths(Architecture arch)
        {
            string extractedFullPath;
            string patchFullPath;
            ExtractedPatches msPs = new ExtractedPatches();
            
            switch (arch)
            {
                case Architecture.X86:
                    // extract and set x86 patch contents
                    DoCopyToShare(Architecture.X86, out extractedFullPath, out patchFullPath);
                    msPs.ExtractedFullPathX86 = extractedFullPath;
                    msPs.PatchFullPathX86 = patchFullPath;
                    break;
                case Architecture.AMD64:
                    // extract and set AMD64 patch contents
                    DoCopyToShare(Architecture.AMD64, out extractedFullPath, out patchFullPath);
                    msPs.ExtractedFullPathAMD64 = extractedFullPath;
                    msPs.PatchFullPathAMD64 = patchFullPath;
                    break;
                case Architecture.IA64:
                    // extract and set IA64 patch contents
                    DoCopyToShare(Architecture.IA64, out extractedFullPath, out patchFullPath);
                    msPs.ExtractedFullPathIA64 = extractedFullPath;
                    msPs.PatchFullPathIA64 = patchFullPath;
                    break;
                case Architecture.ARM:
                    // extract and set ARM patch contents
                    DoCopyToShare(Architecture.ARM, out extractedFullPath, out patchFullPath);
                    msPs.ExtractedFullPathARM = extractedFullPath;
                    msPs.PatchFullPathARM = patchFullPath;
                    break;
                default:
                    break;
            }

            return msPs;
        }


        private List<string> DownloadPackages()
        {
            WorkItem item = Connect2TFS.Connect2TFS.GetWorkItem(_workItemId, TFSServerURI);
            List<string> packages = new List<string>();
            WorkItemHelper helper = new WorkItemHelper(item);
            int exitCode = 0;
            string localDownloadLocation = Path.Combine(ConfigurationManager.AppSettings["DownloadPath"], $"KB{helper.KBNumber}");
            string downloadRoots = string.Empty;
            if (!String.IsNullOrEmpty(item["Drop Patch Location"].ToString()))
            {
                packages.Add(Path.Combine(localDownloadLocation, $"{item["Drop Patch Location"].ToString()}\\{item["Patch Name"].ToString()}"));
            }
            if (!String.IsNullOrEmpty(item["Drop Patch Location X64"].ToString()))
            {
                packages.Add(Path.Combine(localDownloadLocation, $"{item["Drop Patch Location X64"].ToString()}\\{item["Patch Name X64"].ToString()}"));
            }
            if (!String.IsNullOrEmpty(item["Drop Patch Location ARM64"].ToString()))
            {
                packages.Add(Path.Combine(localDownloadLocation, $"{item["Drop Patch Location ARM64"].ToString()}\\{item["Patch Name ARM64"].ToString()}"));
            }
            DropHelper dropHelper = new DropHelper();
            //MsuDropObject msuDropObject = new MsuDropObject(item["Drop Name X64"].ToString());
            MsuDropObject msuDropObject = new MsuDropObject($"NetFxServicing/KB/{helper.KBNumber}");
            var jsonStr = dropHelper.GetDropInfoJsonStr(msuDropObject);
            List<DropInfo> drops = dropHelper.DeserializeJsonStr(jsonStr);
            drops.Sort((a, b) => b.CreatedDateUtc.CompareTo(a.CreatedDateUtc));

            msuDropObject = new MsuDropObject(drops[0].Name);

            if (dropHelper.DownloadPackage(msuDropObject, localDownloadLocation, $" -r {downloadRoots}", out exitCode))
            {
                foreach (string root in downloadRoots.Split(';'))
                {
                    packages.Add(Path.Combine(localDownloadLocation, root));
                }
            }
            else
            {
                throw new Exception($"Failed to download Patches from DevDiv cloud. Exit code: {exitCode}");
            }
            return packages;
        }

        private void DoCopyToShare(Architecture architectures, out string extractedPatchPath, out string patchSharePath)
        {
            extractedPatchPath = string.Empty;
            patchSharePath = string.Empty;
            string localPath = string.Empty;           
            
            if (_availableArchitectures.Contains(architectures))
            {
                //string path = GetPatchPath(architectures);
                string path = string.Empty;
                foreach (string str in packagePaths)
                {
                    if (str.Contains(architectures.ToString().ToLower()))
                    {
                        path = str;
                        break;
                    }
                        
                }
                if (!string.IsNullOrEmpty(path))
                {
                    try
                    {
                        //Copy all patches from (1) to local temp folder
                        string kbNumber = String.Format("KB{0}", _rmPatch.KBNumber);
                        localPath = Extraction.ExtractPatchToLocalPath(path, kbNumber, _rmPatch.MetaData.PatchTechnology);

                        // if this is a cbs patch, then only for *.cab files, otherwise look for *.msp
                        string filter = (_rmPatch.MetaData.PatchTechnology.Equals("CBS")) ? "*.cab" : "*.msp";
                        //locate patch using kbnumber
                        string patchPath = FindPatch(kbNumber, localPath, filter);

                        //copy msp to \\vsufile share (share path set in Resource)
                        string share = ConfigurationManager.AppSettings["SharePath"];// DataAggregator.Properties.Resources.SharePath;
                        // Get build number and construct apporpriate build path
                        string buildNumber = GetBuildNumber();

                        // now copy the actual patch itself.
                        //patchSharePath = path;
                        patchSharePath =
                            CopyToShareLocation(
                                Path.Combine(new string[] { share, kbNumber, buildNumber }), path, architectures.ToString());

                        if (_rmPatch.MetaData.PatchTechnology == "OCM" && string.IsNullOrEmpty(extractedPatchPath))
                        {
                            extractedPatchPath = patchSharePath;
                            return;
                        }

                        if (!string.IsNullOrEmpty(patchPath))
                        {
                            // first copy the extract patch content (cab or msp file)
                            //extractedPatchPath = CopyToShareLocation(Path.GetDirectoryName(path) ,patchPath, architectures.ToString());
                            extractedPatchPath =
                                CopyToShareLocation(
                                    Path.Combine(new string[] { share, kbNumber, buildNumber }), patchPath, architectures.ToString());
                        }
                        else
                        {
                            throw new Exception(string.Format("Can not find msp file, Local path is {0}, KB number is {1}", localPath, kbNumber));
                        }
                    }
                    catch (Exception ex)
                    {
                        throw ex;
                    }
                    finally
                    {
                        if (!string.IsNullOrEmpty(localPath))
                        {
                            DirectoryInfo parent = new DirectoryInfo(localPath).Parent; // delete local (temp) extract directory
                            if (parent != null) DeleteDirectory(parent.FullName);
                            else DeleteDirectory(localPath);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// First determine path to copy by calculating appropriate share path \\share-path\kbnumber\buildnumber\[0, 1, 2, ....]\architecture
        /// Second create directory and then copy local msp to the share, returning copied files name
        /// </summary>
        /// <param name="share">share path specified up to \\share-path\kbnumber\buildnumber\</param>
        /// <param name="patchPath">path of the local extracted msp</param>
        /// <param name="architecture">one of X86, AMD64, ARM, IA64</param>
        /// <returns></returns>
        private string CopyToShareLocation(string share, string patchPath, string architecture)
        {
            if (String.IsNullOrEmpty(_destinationSharePath))
                _destinationSharePath = CalculateSharePath(share);

            string newSharePath = Path.Combine(_destinationSharePath, architecture); // set new share to \\share-path\kbnumber\buildnumber\[0, 1, 2, ....]\architecture
            string destination = Path.Combine(newSharePath, new FileInfo(patchPath).Name);

            if (!Directory.Exists(newSharePath)) // if this share already exists then don't create it again.
                Directory.CreateDirectory(newSharePath);

            File.Copy(patchPath, destination, true);

            return destination;
        }

        /// <summary>
        /// Calculate share path based on \\share\[folder name 0, 1, 2, ... n]
        /// </summary>
        /// <param name="share">share path</param>
        /// <returns>share path with the folder name of the highest non-existent directory</returns>
        private string CalculateSharePath(string share)
        {
            string sharePath;
            int counter = 0;
            for (; ; counter++) // this goes on until we find a directory that does not exist
            {
                sharePath = Path.Combine(share, counter.ToString(CultureInfo.InvariantCulture));
                if (!Directory.Exists(sharePath))
                    break;
            }
            return sharePath;
        }

        private string GetBuildNumber()
        {
            string buildNumber;

            // first try and set build number for CBS or Redbits MSI.            
            buildNumber = (string.IsNullOrEmpty(_rmPatch.SourceBuildGDR)) ? _rmPatch.SourceBuildLDR : _rmPatch.SourceBuildGDR; //if LDR then return the ldr build number_rmPatch.SourceBuildGDR;

            // if buildNumber is null then this is a normal MSI patch
            if (string.IsNullOrEmpty(buildNumber))
                buildNumber = (string.IsNullOrEmpty(_rmPatch.PatchBuildGDR)) ? _rmPatch.PatchBuildLDR : _rmPatch.PatchBuildGDR; //if LDR then return the ldr build number_rmPatch.PatchBuildGDR;

            // if the build number is still null throw an exception
            if (string.IsNullOrEmpty(buildNumber))
                throw new ArgumentNullException(
                    String.Format("buildNumber variable is null. We should have picked one value from PatchBuildLDR:{0}, PatchBuildGDR:{1}, SourceBuildLDR:{2}, SourceBuildGDR:{3}",
                    _rmPatch.PatchBuildLDR, _rmPatch.PatchBuildGDR, _rmPatch.SourceBuildLDR, _rmPatch.SourceBuildGDR)
                    );

            // if the build number is a string of build numbers separated by a comma split it and return the first build number.
            //if (buildNumber.Contains(','))
            //    buildNumber = buildNumber.Split(',')[0];
            // TODO: this is commented out for now, we should never have a build number that is comma separated string with multiple build numbers

            return buildNumber.Trim();
        }

        private void DeleteDirectory(string localPath)
        {
            try
            {
                if (Directory.Exists(localPath))
                    Directory.Delete(localPath, true);
            }
            catch (Exception exception)
            {
                //TODO: Log this message
                Console.WriteLine(exception.Message);
            }
        }

        /// <summary>
        /// Finds the extracted cab or msp file under a given directory
        /// </summary>
        /// <param name="kbNumber">KBNumber to look for in the file name</param>
        /// <param name="directory">Directory to search under</param>
        /// <param name="filter">filter for the file, *.cab for CBS patches (msu); *.msp for MSI patches</param>
        /// <returns></returns>
        private string FindPatch(string kbNumber, string directory, string filter = "*.msp")
        {
            DirectoryInfo di = new DirectoryInfo(directory);

            return (from fileInfo in di.GetFiles(filter, SearchOption.AllDirectories) where fileInfo.Name.ToLower().Contains(kbNumber.ToLower()) select fileInfo.FullName).FirstOrDefault();
        }

        private string GetPatchPath(Architecture architecture)
        {
            string path = string.Empty;
            if (architecture == Architecture.ARM && _rmPatch.PatchLocationARM != null && _rmPatch.PatchNameARM != null)
                path = (Path.Combine(_rmPatch.PatchLocationARM, _rmPatch.PatchNameARM));

            if (architecture == Architecture.X86 && _rmPatch.PatchLocationx86 != null && _rmPatch.PatchNamex86 != null)
                path = (Path.Combine(_rmPatch.PatchLocationx86, _rmPatch.PatchNamex86));

            if (architecture == Architecture.IA64 && _rmPatch.PatchLocationIA64 != null && _rmPatch.PatchNameIA64 != null)
                path = (Path.Combine(_rmPatch.PatchLocationIA64, _rmPatch.PatchNameIA64));

            if (architecture == Architecture.AMD64 && _rmPatch.PatchLocationx64 != null && _rmPatch.PatchNamex64 != null)
                path = (Path.Combine(_rmPatch.PatchLocationx64, _rmPatch.PatchNamex64));

            return path;
        }

        /// <summary>
        /// Constructs test group name as such kbnumber-[Hotfix|GDR]-architecture-[MSI|CBS|OCM]
        /// </summary>
        /// <param name="kbNumber"></param>
        /// <returns></returns>
        private string SetTestGroupName(string kbNumber)
        {
            string releaseType = _rmPatch.ReleaseType.ToLower().Contains("hotfix") ? "Hotfix" : "GDR"; // Set this to either hotfix or GDR
            string testGroupName = String.Format("{0}-{1}-{2}-{3}", kbNumber, releaseType, _arch.ToString(), _rmPatch.MetaData.PatchTechnology);

            // Now restrict the length of this variable to 40 characters
            if (testGroupName.Length > 40)
                testGroupName = testGroupName.Remove(39); //remove everything from the 40th character onwards

            return testGroupName;
        }

        #endregion
    }
}
