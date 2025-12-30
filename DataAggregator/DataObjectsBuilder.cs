using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using RMIntegration.RMService;
using Helper;
using File = RMIntegration.RMService.File;

namespace DataAggregator
{
    #region Data Model Objects
    public class DFile
    {
        public string FFile { set; get; }
        public string Version { set; get; }
    }

    public class DRegistry
    {
        public string RegKey { set; get; }
        public string RegKeyVal { set; get; }
    }

    public class TargetProduct
    {
        public string ProductName { private set; get; }
        public string SKU { private set; get; }
        public string ProductSPLevel { private set; get; }

        /// <summary>
        /// sets all public properties for target product
        /// </summary>
        /// <param name="productName">Product name like "Microsoft .NET Framework 4.0 RTM"</param>
        public TargetProduct(string productName)
        {
            ProductName = ProductSPLevel = SKU = "";

            if (String.IsNullOrEmpty(productName))
                return; //If this is not set in the ReleaseMan item return without doing any processing.

            productName = productName.Trim();
            string[] productSplit = productName.Split(' ');
            int length = productSplit.Count();

            if (length > 3) ProductSPLevel = productSplit[length - 1];

            if (length > 2)
            {
                SKU = productSplit[length - 2];
                //if patch target multi products and the product name got from RM likes ".NET Framework 4.5/4.5.1/4.5.2 RTM"
                //we need to make SKU point to the end(max) product 4.5.2
                if (SKU.TrimEnd('/').Contains("/"))
                {
                    SKU = SKU.Substring(SKU.LastIndexOf("/") + 1);
                }
            }

            ProductName = SetProductName(productSplit);
        }

        public override string ToString()
        {
            return string.Format("{0} {1} {2}", ProductName, SKU, ProductSPLevel);
        }


        private string SetProductName(string[] product)
        {
            string productName = "";
            for (int i = 0; i < product.Count() - 2; i++)
                productName += string.Format("{0} ", product[i]);

            return productName.TrimEnd(' ');
        }

    }

    public class TargetOS
    {
        public string OSName { private set; get; }
        public string OSSPLevel { private set; get; }

        public TargetOS(string osName)
        {
            OSName = OSSPLevel = "";

            string[] osSplit = osName.Split(' ');
            int length = osSplit.Length;

            if (length > 1) OSSPLevel = osSplit[length - 1];

            OSName = SetOSName(osSplit);

        }

        private string SetOSName(string[] os)
        {
            string name = "";
            for (int i = 0; i < os.Count() - 1; i++)
                name += string.Format("{0} ", os[i]);

            return name.TrimEnd(' ');
        }
    }

    public class ExtractedPatches
    {
        #region X86 location

        private string _extractedPathX86;
        public string ExtractedPathX86
        {
            get { return _extractedPathX86; }
        }

        private string _extractedNameX86;

        public string ExtractedNameX86
        {
            get { return _extractedNameX86; }
        }

        public string ExtractedFullPathX86
        {
            get { return Path.Combine(ExtractedPathX86, ExtractedNameX86); }
            set { SetPathName(value, out _extractedPathX86, out _extractedNameX86); }
        }

        private string _patchNameX86;
        /// <summary>
        /// Points to the actual patch name, copied to our test share.
        /// </summary>
        public string PatchNameX86
        {
            get { return _patchNameX86; }
        }

        public string PatchFullPathX86
        {
            get { return Path.Combine(ExtractedPathX86, PatchNameX86); }
            set { SetPathName(value, out _extractedPathX86, out _patchNameX86); }
        }
        #endregion

        #region AMD64 location

        private string _extractedPathAMD64;
        public string ExtractedPathAMD64
        {
            get { return _extractedPathAMD64; }
        }

        private string _extractedNameAMD64;
        public string ExtractedNameAMD64
        {
            get { return _extractedNameAMD64; }
        }

        public string ExtractedFullPathAMD64
        {
            get { return Path.Combine(ExtractedPathAMD64, ExtractedNameAMD64); }
            set { SetPathName(value, out _extractedPathAMD64, out _extractedNameAMD64); }
        }

        private string _patchNameAMD64;
        /// <summary>
        /// Points to the actual patch name, copied to our test share.
        /// </summary>
        public string PatchNameAMD64
        {
            get { return _patchNameAMD64; }
        }

        public string PatchFullPathAMD64
        {
            get { return Path.Combine(ExtractedPathAMD64, PatchNameAMD64); }
            set { SetPathName(value, out _extractedPathAMD64, out _patchNameAMD64); }
        }

        #endregion


        #region IA64 location

        private string _extractedPathIA64;
        public string ExtractedPathIA64
        {
            get { return _extractedPathIA64; }
        }

        private string _extractedNameIA64;
        public string ExtractedNameIA64
        {
            get { return _extractedNameIA64; }
        }

        public string ExtractedFullPathIA64
        {
            get { return Path.Combine(ExtractedPathIA64, ExtractedNameIA64); }
            set { SetPathName(value, out _extractedPathIA64, out _extractedNameIA64); }
        }

        private string _patchNameIA64;
        /// <summary>
        /// Points to the actual patch name, copied to our test share.
        /// </summary>
        public string PatchNameIA64
        {
            get { return _patchNameIA64; }
        }

        public string PatchFullPathIA64
        {
            get { return Path.Combine(ExtractedPathIA64, PatchNameIA64); }
            set { SetPathName(value, out _extractedPathIA64, out _patchNameIA64); }
        }

        #endregion


        #region ARM location

        private string _extractedPathARM;
        public string ExtractedPathARM
        {
            get { return _extractedPathARM; }
        }

        private string _extractedNameARM;
        public string ExtractedNameARM
        {
            get { return _extractedNameARM; }
        }

        public string ExtractedFullPathARM
        {
            get { return Path.Combine(ExtractedPathARM, ExtractedNameARM); }
            set { SetPathName(value, out _extractedPathARM, out _extractedNameARM); }
        }

        private string _patchNameARM;
        /// <summary>
        /// Points to the actual patch name, copied to our test share.
        /// </summary>
        public string PatchNameARM
        {
            get { return _patchNameARM; }
        }

        public string PatchFullPathARM
        {
            get { return Path.Combine(ExtractedPathARM, PatchNameARM); }
            set { SetPathName(value, out _extractedPathARM, out _patchNameARM); }
        }

        #endregion

        private void SetPathName(string value, out string path, out string name)
        {
            if (!string.IsNullOrEmpty(value))
            {
                FileInfo fi = new FileInfo(value);
                path = fi.Directory.FullName;
                name = fi.Name;
            }
            else
            {
                path = name = string.Empty;
            }
        }
    }

    #endregion

    /// <summary>
    /// Base class for build object.
    /// Contains definitions for common data members for build being tested.
    /// </summary>
    public abstract class BuildBase
    {
        #region Data Members
        /// <summary>
        /// For patches this would represent the build number for GDR's (or standard build number for LDR's)
        /// For product setup it just means the build number of the build being tested
        /// </summary>
        public string BuildNumber { set; get; }

        /// <summary>
        /// Refers to the fully qualified path of the build being tested
        /// </summary>
        public string BPath { set; get; }

        /// <summary>
        /// file name of the build being tested
        /// </summary>
        public string FileName { set; get; }

        /// <summary>
        /// Returns fully qualified path, along with file name
        /// </summary>
        public string FullPath
        {
            get { return Path.Combine(BPath, FileName); }
        }
        #endregion
    }

    public abstract class ServicingBuilds : BuildBase
    {
        private File[] _fileList;
        public File[] FileList
        {
            get { return _fileList; }
        }

        public string KbNumber { set; get; }
        public string MspFileName { set; get; }
        public string MSPPath { set; get; }
        public string LdrBuildNumber { set; get; }
        public bool IsDualBranch { set; get; }
        public bool IsLdr { set; get; }
        public string UpdateType { set; get; } //TODO: Get value from Enum
        public int WorkItemId { set; get; }
        public TargetProduct TargetProd { set; get; }
        public string TargetArch { set; get; }
        public string TargetLanguage { set; get; }
        public string PatchTechnology { set; get; }
        public string ReleaseType { set; get; }

        public virtual void Build(Patch patch, Architecture architectures, ExtractedPatches patches)
        {
            _fileList = patch.Files;
            KbNumber = "KB" + Convert.ToString(patch.KBNumber);
            LdrBuildNumber = (string.IsNullOrEmpty(patch.SourceBuildLDR)) ? patch.PatchBuildLDR : patch.SourceBuildLDR; // If Source LDR build number is null, set the PatchLDRBuild number (MSI)
            IsLdr = patch.MetaData.PatchTechnology.ToUpperInvariant() != "OCM"; //All SKUs moved to LDR branch

            IsDualBranch = patch.IsDualBranch;
            UpdateType = patch.ReleaseType;
            WorkItemId = (int)patch.Id;
            TargetProd = new TargetProduct(patch.MetaData.TargetProduct);
            TargetArch = architectures.ToString();
            TargetLanguage = ""; //TODO: is this even relevant -- patch.MetaData.TargetLanguages;
            base.BuildNumber =
                (string.IsNullOrEmpty(patch.SourceBuildGDR)) ? ((string.IsNullOrEmpty(patch.PatchBuildGDR)) ? this.LdrBuildNumber : patch.PatchBuildGDR) : patch.SourceBuildGDR;
            if (patches != null)
                SetPath(patches, architectures);
            PatchTechnology = patch.MetaData.PatchTechnology;
            ReleaseType = patch.ReleaseType;
        }

        protected virtual void SetPath(ExtractedPatches patch, Architecture architectures)
        {
            switch (architectures)
            {
                case Architecture.X86:
                    BPath = patch.ExtractedPathX86;
                    FileName = patch.PatchNameX86;
                    MspFileName = patch.ExtractedNameX86;
                    MSPPath = patch.ExtractedFullPathX86;
                    break;
                case Architecture.AMD64:
                    BPath = patch.ExtractedPathAMD64;
                    FileName = patch.PatchNameAMD64;
                    MspFileName = patch.ExtractedNameAMD64;
                    MSPPath = patch.ExtractedFullPathAMD64;
                    break;
                case Architecture.IA64:
                    BPath = patch.ExtractedPathIA64;
                    FileName = patch.PatchNameIA64;
                    MspFileName = patch.ExtractedNameIA64;
                    MSPPath = patch.ExtractedFullPathIA64;
                    break;
                case Architecture.ARM:
                    BPath = patch.ExtractedPathARM;
                    FileName = patch.PatchNameARM;
                    MspFileName = patch.ExtractedNameARM;
                    MSPPath = patch.ExtractedFullPathARM;
                    break;
                default:
                    BPath = MSPPath = FileName = MspFileName = null;
                    break;
            }
        }
    }


    public class PatchSAFX : ServicingBuilds
    {
        // this the expected file list we get from release man
        private string _expectedFileList;

        public string LCUPatchLocation;

        public string Custom1Data;

        public new string FileList
        {
            get { return _expectedFileList; }
        }

        public string FileListGdrVersion { get { return ConvertFileList(false); } }

        public string FileListLdrVersion { get { return ConvertFileList(true); } }

        public string TargetFrameworkVersion { private set; get; }

        public override void Build(Patch patch, Architecture architectures, ExtractedPatches patches)
        {
            base.Build(patch, architectures, patches);
            _expectedFileList = ConvertFileList(patch.ExpectedFiles);
            // Set the framework family\version            
            TargetFrameworkVersion = string.Format("NDP{0}{1}", TargetProd.SKU.Replace(".", ""), ((TargetProd.ProductSPLevel) == "RTM") ? "" : TargetProd.ProductSPLevel);
        }

        #region Internal Data Cleaning methods

        // cleans up expected file list property and sends back a comma separated string of files
        private string ConvertFileList(string expectedFiles)
        {
            string files = expectedFiles.Split(new char[] { ' ', ';' }, StringSplitOptions.RemoveEmptyEntries).Aggregate(string.Empty, (current, fileName) => current + (fileName + ","));

            return files.TrimEnd(',');
        }

        private string ConvertFileList()
        {
            string files = base.FileList.Where(file => file.PatchArchitecture.ToLower().Equals(GetMappedArchitecture().ToLower())).Aggregate("", (current, file) => current + (file.FileName + ","));
            return files.TrimEnd(',');
        }

        private string ConvertFileList(bool notGdrPayload)
        {
            string filesWithVersions = "";
            if (!IsLdr) // means this is a GDR
            {
                if (!notGdrPayload && IsDualBranch) // this the GDR payload of the GDR
                    filesWithVersions =
                        base.FileList.Where(
                            file =>
                            !file.IsLDRPayload && file.PatchArchitecture.ToLower().Equals(GetMappedArchitecture().ToLower()))
                            .Aggregate(filesWithVersions,
                                       (current, file) =>
                                       current + string.Format("({0}-{1}),", file.FileName, file.FileVersion));
                else
                    if ((notGdrPayload && IsDualBranch) || // this is the LDR case for the GDR
                        (!notGdrPayload && !IsDualBranch)) // this is the GDR which is single branch ARM & NDP 20 SP1 MSI
                        filesWithVersions =
                            base.FileList.Where(
                                file =>
                                file.IsLDRPayload &&
                                file.PatchArchitecture.ToLower().Equals(GetMappedArchitecture().ToLower())).Aggregate(
                                    filesWithVersions,
                                    (current, file) =>
                                    current + string.Format("({0}-{1}),", file.FileName, file.FileVersion));
            }
            else // this is the hotfix case
                filesWithVersions = base.FileList.Where(file => file.PatchArchitecture.ToLower().Equals(GetMappedArchitecture().ToLower())).Aggregate(filesWithVersions, (current, file) => current + string.Format("({0}-{1}),", file.FileName, file.FileVersion));

            return filesWithVersions.TrimEnd(',');
        }

        private string GetMappedArchitecture()
        {
            return TargetArch.Equals("AMD64") ? "x64" : TargetArch;
        }
        #endregion
    }

    public class PatchSmoke : ServicingBuilds
    {
        public string VerificationOption { set; get; }
        public bool PauseOnFailure { set; get; }
        public string TestGroupName { set; get; }
        public List<TargetOS> TargetOperatingSystems { set; get; }

        public string LCUPatchLocation { get; set; }

        public override void Build(Patch patch, Architecture architectures, ExtractedPatches patches)
        {
            TargetOperatingSystems = new List<TargetOS>();
            base.Build(patch, architectures, patches);

            // now set target operating systems
            foreach (OS os in patch.MetaData.TargetOS)
            {
                TargetOperatingSystems.Add(new TargetOS(os.OSShortName));
            }
        }

    }

    public class PatchTargetInfo : ServicingBuilds
    {
        public string TargetProduct { get; set; }
        public List<TargetOS> TargetOperatingSystems { set; get; }

        public override void Build(Patch patch, Architecture architectures, ExtractedPatches patches)
        {
            TargetOperatingSystems = new List<TargetOS>();
            base.Build(patch, architectures, patches);

            TargetProduct =new TargetProduct(patch.MetaData.TargetProduct).ToString();

            // now set target operating systems
            foreach (OS os in patch.MetaData.TargetOS)
            {
                TargetOperatingSystems.Add(new TargetOS(os.OSShortName));
            }
        }
    }

    public class RefreshRedistHFRInfo : PatchTargetInfo
    {
        public RefreshRedistHFRType PackageType { get; private set; }
        public string PackagePath { get; private set; }
        public string ConfigSharePath { get; private set; }

        public override void Build(Patch patch, Architecture architectures, ExtractedPatches patches)
        {
            base.Build(patch, architectures, patches);

            GetData(patch);
        }

        private void GetData(Patch patch)
        {
            ConfigSharePath = DataAggregator.Properties.Resources.SharePath;
            if(!String.IsNullOrEmpty(patch.PatchLocationx64))
                PackagePath = Path.Combine(patch.PatchLocationx64, patch.PatchNamex64);
            else if (!String.IsNullOrEmpty(patch.PatchLocationx86))
                PackagePath = Path.Combine(patch.PatchLocationx86, patch.PatchNamex86);
                        
            if (String.IsNullOrEmpty(PatchTechnology))
            {
                throw new Exception("Failed retrive PatchTechnology from ReleaseMan, please sync ReleaseMan with TFS");
            }
            if (String.IsNullOrEmpty(BuildNumber))
            {
                throw new Exception("Failed retrive BuildNumber from ReleaseMan, please sync ReleaseMan with TFS");
            }

            switch (PatchTechnology.ToUpperInvariant())
            {
                case "CBS":
                    PackageType = RefreshRedistHFRType.RefreshRedistMSU;
                    break;

                case "MSI":
                    PackageType = RefreshRedistHFRType.FullRedist;
                    break;

                case "CHAINER":
                    if (Path.GetFileName(PackagePath).ToUpperInvariant().EndsWith("-WEB.EXE"))
                    {
                        PackageType = RefreshRedistHFRType.Webbootstrapper;
                    }
                    else
                    {
                        PackageType = RefreshRedistHFRType.FullRedistISV;
                    }
                    break;

                default:
                    throw new Exception("Unsupportted patch technology");
            }

            if (String.IsNullOrEmpty(PackagePath) && PackageType == RefreshRedistHFRType.RefreshRedistMSU)
            {
                throw new Exception("Failed retrive PackagePath from ReleaseMan, please sync ReleaseMan with TFS");
            }
        }
    }
}
