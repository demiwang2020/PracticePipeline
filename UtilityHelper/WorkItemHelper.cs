using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.TeamFoundation.WorkItemTracking.Client;
using System.IO;
using System.Configuration;
using System.Security.Cryptography.X509Certificates;
using HotFixLibrary;
using MadDogObjects.BuildWebServices;
namespace Helper
{
    // This class is a wrapper of WorkItem, similar to WorkItemBO but faster

    public class WorkItemHelper
    {
        private WorkItem _wi;
        private Dictionary<string, string> _dictChangedFields; // a temp dictionary to save changed fields and their values

        public WorkItemHelper(string tfsuri, int tfsid)
        {
            _wi = Connect2TFS.Connect2TFS.GetWorkItem(tfsid, tfsuri);
            _dictChangedFields = null;
        }

        public WorkItemHelper(WorkItem wi)
        {
            _wi = wi;
            _dictChangedFields = null;
        }

        public int ID
        {
            get { return _wi.Id; }
        }

        public string SKU
        {
            get
            {
                return _wi["SKU"].ToString().Split(new char[] { '/' }).Last();
            }
        }

        public string OSInstalled
        {
            get
            {
                return _wi["Environment"].ToString();
            }
        }

        public string OSSPLevel
        {
            get
            {
                return _wi["Target Architecture"].ToString();
            }
        }

        public string ProductSPLevel
        {
            get
            {
                return _wi["Target"].ToString();
            }
        }

        public string OSArchitecture
        {
            get
            {
                return _wi["Processor"].ToString();
            }
        }

        public string BuildNumber
        {
            get
            {
                return _wi["Build Number"].ToString();
            }
        }

        public string BaseBuildNumber
        {
            get
            {
                return _wi["Base Build Number"].ToString();
            }
        }

        public string KBNumber
        {
            get
            {
                return _wi["KB Article"].ToString();
            }
        }

        public string LCUKBArticle
        {
            get
            {
                return _wi["LCU KB Article"].ToString();
            }
        }

        public string Notes
        {
            get
            {
                return _wi["Notes"].ToString();
            }
        }

        public string Title
        {
            get
            {
                return _wi["Title"].ToString();
            }
        }

        public string ComponentVersion
        {
            get
            {
                return _wi["Windows Component Version"].ToString();
            }
        }

        public string ReleaseType
        {
            get
            {
                return _wi["Release Type"].ToString();
            }
        }

        public LinkCollection Links
        {
            get
            {
                return _wi.Links;
            }
        }

        public AttachmentCollection Attachments
        {
            get
            {
                return _wi.Attachments;
            }
        }

        public string Custom01
        {
            get
            {
                return _wi["Custom01"].ToString();
            }
        }

        public string Custom02
        {
            get
            {
                return _wi["Custom02"].ToString();
            }
        }


        public string PatchTechnology
        {
            get
            {
                return _wi["Deliverable"].ToString();
            }
        }

        public string PackagePropLocation
        {
            get
            {
                return _wi["Package Propped Location X64"].ToString();
            }
        }



        public string DropName
        {
            get
            {
                return _wi["Drop Name"].ToString();
            }
        }

        public int CPId
        {
            get
            {
                string cpidstring = _wi["Compliance Review"].ToString();
                int cpid = 0;
                if (!int.TryParse(cpidstring, out cpid))
                {
                    cpid = 0;
                }

                return cpid;
            }
        }

        public int WindowsPackagingId
        {
            get
            {
                string jobidstring = _wi["Windows Packaging ID"].ToString();
                int jobid = 0;
                if (!int.TryParse(jobidstring, out jobid))
                {
                    return 0;
                }

                return jobid;
            }
        }

        public int LCUWindowsPackagingId
        {
            get
            {
                string jobidstring = _wi["LCU Windows Packaging ID"].ToString();
                int lcujobid = 0;
                if (!int.TryParse(jobidstring, out lcujobid))
                {
                    return 0;
                }
                return lcujobid;
            }
        }

        /// <summary>
        /// Get Patch Name
        /// </summary>
        /// <param name="arch"></param>
        /// <returns></returns>
        public string GetPatchName(Architecture arch)
        {
            string fieldName = String.Empty;
            switch (arch)
            {
                case Architecture.X86:
                    fieldName = "Patch Name";
                    break;

                case Architecture.AMD64:
                    fieldName = "Patch Name X64";
                    break;

                case Architecture.IA64:
                    fieldName = "Patch Name IA64";
                    break;

                case Architecture.ARM:
                    fieldName = "Patch Name ARM";
                    break;

                case Architecture.ARM64:
                    fieldName = "Patch Name ARM64";
                    break;

                default:
                    throw new NotSupportedException("Not supported CPU id " + arch);
            }
            return _wi[fieldName] == null ? null : _wi[fieldName].ToString();
        }

        public string GetPatchNameForUpgradePackage(Architecture arch, WorkItem workItem)
        {
            string fieldName = String.Empty;
            switch (arch)
            {
                case Architecture.X86:
                    fieldName = "Patch Name";
                    break;

                case Architecture.AMD64:
                    fieldName = "Patch Name X64";
                    break;

                case Architecture.IA64:
                    fieldName = "Patch Name IA64";
                    break;

                case Architecture.ARM:
                    fieldName = "Patch Name ARM";
                    break;

                case Architecture.ARM64:
                    fieldName = "Patch Name ARM64";
                    break;

                default:
                    throw new NotSupportedException("Not supported CPU id " + arch);
            }
            return workItem[fieldName] == null ? null : workItem[fieldName].ToString();
        }
        /// <summary>
        /// add by JC
        /// </summary>
        /// <param name="arch"></param>
        /// <returns></returns>
        /// <exception cref="NotSupportedException"></exception>
        public string GetPackagePropLoc(Architecture arch)
        {
            string fieldName = String.Empty;
            switch (arch)
            {
                case Architecture.X86:
                    fieldName = "Package Propped Location";
                    break;

                case Architecture.AMD64:
                    fieldName = "Package Propped Location X64";
                    break;

                case Architecture.IA64:
                    fieldName = "Package Propped Location IA64";
                    break;

                case Architecture.ARM:
                    fieldName = "Package Propped Location ARM";
                    break;

                case Architecture.ARM64:
                    fieldName = "Package Propped Location ARM64";
                    break;

                default:
                    throw new NotSupportedException("Not supported CPU id " + arch);
            }
            return _wi[fieldName] == null ? null : _wi[fieldName].ToString();
        }
        public string GetPackagePropLocForUpgradePackage(Architecture arch, WorkItem workItem)
        {
            string fieldName = String.Empty;
            switch (arch)
            {
                case Architecture.X86:
                    fieldName = "Package Propped Location";
                    break;

                case Architecture.AMD64:
                    fieldName = "Package Propped Location X64";
                    break;

                case Architecture.IA64:
                    fieldName = "Package Propped Location IA64";
                    break;

                case Architecture.ARM:
                    fieldName = "Package Propped Location ARM";
                    break;

                case Architecture.ARM64:
                    fieldName = "Package Propped Location ARM64";
                    break;

                default:
                    throw new NotSupportedException("Not supported CPU id " + arch);
            }
            return workItem[fieldName] == null ? null : workItem[fieldName].ToString();
        }
        /// <summary>
        /// add by JC
        /// </summary>
        /// <param name="arch"></param>
        /// <returns></returns>
        /// <exception cref="NotSupportedException"></exception>
        public string GetDropLocation(Architecture arch)
        {
            string fieldName = String.Empty;
            switch (arch)
            {
                case Architecture.X86:
                    fieldName = "Drop Patch Location";
                    break;

                case Architecture.AMD64:
                    fieldName = "Drop Patch Location X64";
                    break;

                case Architecture.IA64:
                    fieldName = "Drop Patch Location IA64";
                    break;

                case Architecture.ARM:
                    fieldName = "Drop Patch Location ARM";
                    break;

                case Architecture.ARM64:
                    fieldName = "Drop Patch Location ARM64";
                    break;

                default:
                    throw new NotSupportedException("Not supported CPU id " + arch);
            }
            if (String.IsNullOrEmpty(_wi[fieldName].ToString()) && !String.IsNullOrEmpty(GetPackagePropLoc(arch)))
            {
                var patchInfo = GetPackagePropLoc(arch).Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                return patchInfo[1];
            }
            string drop = BuildNumber + "\\" + arch.ToString().ToLower() + "\\enu";

            return _wi[fieldName] == null ? drop : _wi[fieldName].ToString();
        }
        public string GetDropLocationForUpgradePackage(Architecture arch, WorkItem workItem)
        {
            string fieldName = String.Empty;
            switch (arch)
            {
                case Architecture.X86:
                    fieldName = "Drop Patch Location";
                    break;

                case Architecture.AMD64:
                    fieldName = "Drop Patch Location X64";
                    break;

                case Architecture.IA64:
                    fieldName = "Drop Patch Location IA64";
                    break;

                case Architecture.ARM:
                    fieldName = "Drop Patch Location ARM";
                    break;

                case Architecture.ARM64:
                    fieldName = "Drop Patch Location ARM64";
                    break;

                default:
                    throw new NotSupportedException("Not supported CPU id " + arch);
            }
            if (String.IsNullOrEmpty(workItem[fieldName].ToString()) && !String.IsNullOrEmpty(GetPackagePropLocForUpgradePackage(arch, workItem)))
            {
                var patchInfo = GetPackagePropLocForUpgradePackage(arch, workItem).Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                return patchInfo[1];
            }
            return workItem[fieldName] == null ? null : workItem[fieldName].ToString();
        }

        /// <summary>
        /// get patch location with patch name modify by JC
        /// </summary>
        /// <param name="arch"></param>
        /// <returns></returns>
        public string GetPatchDownloadLocation(Architecture arch)
        {
            string path = ConfigurationManager.AppSettings["DownloadPath"];

            if (arch == Architecture.X86 && String.IsNullOrEmpty(GetDropLocation(arch)) && String.IsNullOrEmpty(GetPackagePropLoc(arch)) && !string.IsNullOrEmpty(GetPatchName(arch)) && WindowsPackagingId.ToString() != "0")
            {
                path = Path.Combine(path, $"kb{KBNumber}", WindowsPackagingId.ToString(), "X86", GetPatchName(arch));
            }
            else if (arch == Architecture.AMD64 && String.IsNullOrEmpty(GetDropLocation(arch)) && String.IsNullOrEmpty(GetPackagePropLoc(arch)) && !string.IsNullOrEmpty(GetPatchName(arch)) && WindowsPackagingId.ToString() != "0")
            {
                path = Path.Combine(path, $"kb{KBNumber}", WindowsPackagingId.ToString(), "X64", GetPatchName(arch));
            }
            else if (!string.IsNullOrEmpty(GetPatchName(arch)))
            {
                path = Path.Combine(path, $"kb{KBNumber}", GetDropLocation(arch), GetPatchName(arch));
            }
            else if (!_wi["Patch Location X64"].ToString().StartsWith("http") && WindowsPackagingId.ToString() == "0")
            {
                path = Path.Combine(path, $"kb{KBNumber}", GetDropLocation(arch), GetPatchName(arch));
            }
            return path;
        }
        /// <returns></returns>
        public string GetPatchDownloadLocationForUpgradePackage(Architecture arch, WorkItem workItem)
        {
            string path = ConfigurationManager.AppSettings["DownloadPath"];

            if (arch == Architecture.X86 && String.IsNullOrEmpty(GetDropLocationForUpgradePackage(arch, workItem)) && String.IsNullOrEmpty(GetPackagePropLocForUpgradePackage(arch, workItem)) && !string.IsNullOrEmpty(GetPatchNameForUpgradePackage(arch, workItem)))
            {
                path = Path.Combine(path, $"kb{workItem["KB Article"]}", workItem["Windows Packaging ID"].ToString(), "X86", GetPatchNameForUpgradePackage(arch, workItem));
            }
            else if (arch == Architecture.AMD64 && String.IsNullOrEmpty(GetDropLocationForUpgradePackage(arch, workItem)) && String.IsNullOrEmpty(GetPackagePropLocForUpgradePackage(arch, workItem)) && !string.IsNullOrEmpty(GetPatchNameForUpgradePackage(arch, workItem)))
            {
                path = Path.Combine(path, $"kb{workItem["KB Article"]}", workItem["Windows Packaging ID"].ToString(), "X64", GetPatchNameForUpgradePackage(arch, workItem));
            }
            else if (!string.IsNullOrEmpty(GetPatchNameForUpgradePackage(arch, workItem)))
            {
                path = Path.Combine(path, $"kb{workItem["KB Article"]}", GetDropLocationForUpgradePackage(arch, workItem), GetPatchNameForUpgradePackage(arch, workItem));
            }
            return path;
        }

        public string GetExpandPackage(Architecture arch)
        {
            string path = GetPatchDownloadLocation(arch);
            int len = path.LastIndexOf("\\") + 1;
            string name = path.Substring(len, path.Length - 4 - len);
            string expandPackage = string.Empty;
            //expandPackage = Path.Combine(path.Substring(0,len), "EXPANDED_PACKAGE",name);
            expandPackage = Path.Combine(path.Substring(0, len), "EXPANDED_PACKAGE");
            string[] dirs = Directory.GetDirectories(expandPackage);
            expandPackage = dirs[0];
            return expandPackage;
        }
        public string GetExpandPackageForUpgradePackage(Architecture arch, WorkItem workItem)
        {
            string path = GetPatchDownloadLocationForUpgradePackage(arch, workItem);
            int len = path.LastIndexOf("\\") + 1;
            string name = path.Substring(len, path.Length - 4 - len);
            string expandPackage = string.Empty;
            //expandPackage = Path.Combine(path.Substring(0,len), "EXPANDED_PACKAGE",name);
            expandPackage = Path.Combine(path.Substring(0, len), "EXPANDED_PACKAGE");
            string[] dirs = Directory.GetDirectories(expandPackage);
            expandPackage = dirs[0];
            return expandPackage;
        }

        public void SetPatchName(Architecture arch, string patchName)
        {
            string fieldName = String.Empty;
            switch (arch)
            {
                case Architecture.X86:
                    fieldName = "Patch Name";
                    break;

                case Architecture.AMD64:
                    fieldName = "Patch Name X64";
                    break;

                case Architecture.IA64:
                    fieldName = "Patch Name IA64";
                    break;

                case Architecture.ARM:
                    fieldName = "Patch Name ARM";
                    break;

                case Architecture.ARM64:
                    fieldName = "Patch Name ARM64";
                    break;

                default:
                    throw new NotSupportedException("Not supported CPU id " + arch);
            }

            if (!_wi[fieldName].ToString().Equals(patchName))
            {
                SaveFieldTemply(fieldName, patchName);
            }
        }

        /// <summary>
        /// Get patch location, without patch name
        /// </summary>
        /// <param name="arch"></param>
        /// <returns></returns>
        //public string GetPatchLocation(Architecture arch)
        //{
        //    string fieldName = String.Empty;
        //    string kbNumber = String.Empty;
        //    switch (arch)
        //    {
        //        case Architecture.X86:
        //            fieldName = "KB Published Location";
        //            break;

        //        case Architecture.AMD64:
        //            fieldName = "Patch Location X64";
        //            break;

        //        case Architecture.IA64:
        //            fieldName = "Patch Location IA64";
        //            break;

        //        case Architecture.ARM:
        //            fieldName = "Patch Location ARM";
        //            break;

        //        case Architecture.ARM64:
        //            fieldName = "Patch Location ARM64";
        //            break;

        //        default:
        //            throw new NotSupportedException("Not supported CPU id " + arch);
        //    }
        //    //kbNumber = "KB Article";
        //    var a = _wi[fieldName];
        //    return _wi[fieldName] == null ? null : _wi[fieldName].ToString();
        //    //string PackagePathBase = ConfigurationManager.AppSettings["PackagePath"];

        //    //string SpeficPackage = PackagePathBase + @"\KB" + _wi[kbNumber] + @"\"+arch;
        //    //if (arch.ToString() == "AMD64"&& !File.Exists(SpeficPackage)) {
        //    //    SpeficPackage=SpeficPackage.Replace(arch.ToString(), "X64");
        //    //}

        //    //if (string.IsNullOrEmpty(_wi[fieldName].ToString()) || _wi[fieldName].ToString().StartsWith(@"\\winsehotfix")) {
        //    //        return SpeficPackage;
        //    //}
        //    //return _wi[fieldName].ToString();
        //}
        public string GetPatchLocation(Architecture arch)
        {
            string fieldName = String.Empty;
            string kbNumber = String.Empty;
            string jobid = String.Empty;
            string SpeficPackage = String.Empty;

            switch (arch)
            {
                case Architecture.X86:
                    fieldName = "KB Published Location";
                    break;

                case Architecture.AMD64:
                    fieldName = "Patch Location X64";
                    break;

                case Architecture.IA64:
                    fieldName = "Patch Location IA64";
                    break;

                case Architecture.ARM:
                    fieldName = "Patch Location ARM";
                    break;

                case Architecture.ARM64:
                    fieldName = "Patch Location ARM64";
                    break;

                default:
                    throw new NotSupportedException("Not supported CPU id " + arch);
            }
            string kbNumberfileName = "KB Article";
            kbNumber = "KB" + _wi[kbNumberfileName];
            string jobidfieldName = "Windows Packaging ID";
            jobid = _wi[jobidfieldName].ToString();
            string PackagePathBase = ConfigurationManager.AppSettings["DownloadPath"];
            if (!string.IsNullOrEmpty(jobid))
            {

                SpeficPackage = Path.Combine(PackagePathBase, kbNumber, jobid, arch.ToString());
            }

            if (arch.ToString() == "AMD64" && !string.IsNullOrEmpty(SpeficPackage) && !File.Exists(SpeficPackage))
            {
                SpeficPackage = SpeficPackage.Replace(arch.ToString(), "X64");
            }

            if (!string.IsNullOrEmpty(SpeficPackage) && string.IsNullOrEmpty(_wi[fieldName].ToString())
                || _wi[fieldName].ToString().StartsWith(@"\\winsehotfix")
                || _wi[fieldName].ToString().StartsWith(@"https://aka.ms/pcpartifacts?")
                )
            {
                return SpeficPackage;
            }

            if (PatchTechnology == "CBS")
            {
                string path = GetPatchDownloadLocation(arch);
                SpeficPackage = path.Substring(0, path.LastIndexOf("\\"));
                return SpeficPackage;
            }

            return _wi[fieldName].ToString();
        }

        public void SetPatchLocation(Architecture arch, string patchLocation)
        {
            string fieldName = String.Empty;
            switch (arch)
            {
                case Architecture.X86:
                    fieldName = "KB Published Location";
                    break;

                case Architecture.AMD64:
                    fieldName = "Patch Location X64";
                    break;

                case Architecture.IA64:
                    fieldName = "Patch Location IA64";
                    break;

                case Architecture.ARM:
                    fieldName = "Patch Location ARM";
                    break;

                case Architecture.ARM64:
                    fieldName = "Patch Location ARM64";
                    break;

                default:
                    throw new NotSupportedException("Not supported CPU id " + arch);
            }

            if (!_wi[fieldName].ToString().Equals(patchLocation))
            {
                SaveFieldTemply(fieldName, patchLocation);
            }
        }

        /// <summary>
        /// Get patch location, including patch name
        /// </summary>
        /// <param name="arch"></param>
        /// <returns></returns>
        public string GetPatchFullPath(Architecture arch)
        {
            string name = GetPatchName(arch);
            string path = GetPatchLocation(arch);

            try
            {
                string fullPath = System.IO.Path.Combine(path, name);

                return fullPath;
            }
            catch
            {
                return null;
            }
        }

        public string GetProppedLocation(Architecture arch)
        {
            string fieldName = String.Empty;
            switch (arch)
            {
                case Architecture.X86:
                    fieldName = "Package Propped Location";
                    break;

                case Architecture.AMD64:
                    fieldName = "Package Propped Location X64";
                    break;

                case Architecture.IA64:
                    fieldName = "Package Propped Location IA64";
                    break;

                case Architecture.ARM:
                    fieldName = "Package Propped Location ARM";
                    break;

                case Architecture.ARM64:
                    fieldName = "Package Propped Location ARM64";
                    break;

                default:
                    throw new NotSupportedException("Not supported CPU id " + arch);
            }

            return _wi[fieldName] == null ? null : _wi[fieldName].ToString();
        }

        public DateTime GetLastStateChangeTime(string stateName)
        {
            for (int i = _wi.Revisions.Count - 1; i >= 0; --i)
            {
                Revision rvs = _wi.Revisions[i];
                if ((rvs.Fields["State"].OriginalValue == null || !rvs.Fields["State"].OriginalValue.ToString().Equals(stateName)) &&
                    rvs.Fields["State"].Value.ToString() == stateName)
                {
                    return Convert.ToDateTime(rvs.Fields["Changed Date"].Value);
                }
            }

            // return a fake date if not found
            return new DateTime(2000, 1, 1);
        }

        public void SaveWorkItemHelper(string tfsuri)
        {
            if (_dictChangedFields != null && _dictChangedFields.Count > 0)
            {
                // there are some fields to be saved
                Connect2TFS.Connect2TFS.SaveWorkItem(_wi.Id, _dictChangedFields, tfsuri);

                // clear the saving buffer
                _dictChangedFields.Clear();

                // re-fetch work item (so it is latest)
                _wi = Connect2TFS.Connect2TFS.GetWorkItem(_wi.Id, tfsuri);
            }
        }

        private void SaveFieldTemply(string fieldName, string value)
        {
            if (_dictChangedFields == null)
                _dictChangedFields = new Dictionary<string, string>();

            _dictChangedFields[fieldName] = value;
        }
    }
}
