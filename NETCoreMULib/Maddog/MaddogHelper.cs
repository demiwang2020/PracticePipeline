using DefinitionInterpreter;
using Helper;
//using MadDogObjects.BuildWebServices;
using Newtonsoft.Json;
using ScorpionDAL;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Linq;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using MDO = MadDogObjects;

namespace NETCoreMURuntimeLib.Maddog
{
    class MaddogHelper
    {
        #region default configrations of run, could be changed before calling CreateMaddogRun
        public string RunOwner = "vsulab";
        public int ContextID = 675434;
        //public int MachineQueryID = 892116;
        public string TestSourcesLocation = ConfigurationManager.AppSettings["TestSourcesLocation_WUAutomation"];
        public string TestReqBinPath = ConfigurationManager.AppSettings["RequiredBinariesLocation_WUAutomation"];
        #endregion

        public int RunID;
        public int RunStatus;

        public int KickoffMaddogRun(List<Maddog.MDPackage> mDPackages, 
                                          string title,
                                          int osId,
                                          TO os,
                                          int imageId,
                                          int caseQueryID,Architecture arch)
        {
            #region
            //ConnectToMadDog();

            //RunID = 0;
            //RunStatus = 6; // error

            //MDO.Run objRun = new MDO.Run();

            //// Title
            //objRun.Title = title;

            //// Case query
            //MDO.QueryObject testcaseQuery = new MDO.QueryObject(caseQueryID);
            //objRun.TestcaseQuery = testcaseQuery;

            ////context query: default context
            //MDO.QueryObject contextQuery = new MDO.QueryObject(ContextID);
            //objRun.ContextQuery = contextQuery;

            //// Machine query (for x86 and x64)
            ////objRun.VMRole = MDO.enuVMRoles.UsePhysical;
            ////MDO.QueryObject machineQuery = new MDO.QueryObject(MachineQueryID);
            ////objRun.MachineQuery = machineQuery;


            ////OS and image
            //objRun.Owner = new MDO.Owner(RunOwner);
            //objRun.AnalysisProfile = new MDO.AnalysisProfile(17);
            //objRun.AutoReserveMaxMachines = 2;
            //objRun.MaxMachines = 2;
            ////objRun.RunTimeOut = ((arch == Architecture.ARM64) ? 72 : 200);
            //objRun.RunTimeOut = 96;
            //objRun.Reimage = true;
            //objRun.OS = new MDO.OS(osId);
            //objRun.OSImage = new MDO.OSImage(imageId);
            //SetRunMachineQuery(objRun, arch, os);
            ////test source location
            //objRun.InstallSelections.Defaults["TestSourcesLocation"].Value = TestSourcesLocation;

            ////required binaries
            //objRun.InstallSelections.Defaults["TestReqBinPath"].Value = TestReqBinPath;

            //// Driver flags for FXBVT Driver
            //objRun.Flags = new MDO.QueryObject(896247);

            ////Config installation sequence
            //foreach (MDPackage pkg in mDPackages)
            //{
            //    MDO.Package objMDOPackage = new MDO.Package(pkg.ID);
            //    Selection objSelection = MDO.UniversalInstaller.PackageSelection.CreateFromPackage(objMDOPackage);
            //    if (pkg.Tokens != null)
            //    {
            //        foreach (var kv in pkg.Tokens)
            //        {
            //            objSelection.SetToken(kv.Key, kv.Value);
            //        }
            //    }

            //    objRun.InstallSelections.InputSequence.Add(objSelection);
            //}

            //objRun.Save();
            //RunID = objRun.ID;

            //DefinitionInterpreter.Log.Enabled = true;
            //objRun.GenerateInstallationSequence();
            //objRun.SetSecurityOnResultsFolder();
            //objRun.Save();

            //// starting run may fail due to permission issue. Catch any exceptions so we can start them manually instead
            //try
            //{
            //    MDO.Run.RunHelpers.StartRun(objRun);

            //    RunStatus = 1; // running
            //}
            //catch
            //{ }

            //return RunID;
            #endregion

            BaseInfo baseInfo = new BaseInfo()
            {
                arch = arch,
                title = title,
                osId = osId,
                caseQueryID = caseQueryID,
                imageId = imageId,
                osVersion = os.OSVersion,
                mdOsId = os.MDOSID
            };

            NetCoreInfo netCoreInfo = new NetCoreInfo()
            { 
                baseInfo = baseInfo,
                mDPackage = mDPackages,
            };

            try
            {
                HttpClient client = new HttpClient();
                client.Timeout = System.TimeSpan.FromSeconds(120);
                string json = JsonConvert.SerializeObject(netCoreInfo);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                string url = ConfigurationManager.AppSettings["ConnectToMadUrl"] + "api/KickoffRunForNetCore";
                HttpResponseMessage response = client.PostAsync(url,content).Result;
                if(response.IsSuccessStatusCode)
                {
                    RunID = Int32.Parse(response.Content.ReadAsStringAsync().Result);
                    return RunID;
                }
                
                

            }
            catch (Exception ex) 
            {


                return -1;
            }
            return 0;

        }


        private void SetRunMachineQuery(MDO.Run objRun, Architecture arch, TO os)
        {
            int machineQueryID = 0;

            switch (arch)
            {
                case Architecture.IA64: // IA64
                    machineQueryID = 245570;
                    break;

                case Architecture.ARM: // ARM
                    machineQueryID = 712487;
                    break;

                case Architecture.ARM64: // ARM64
                    machineQueryID = 909348;
                    break;

                default: // x86 and x64
                    //if (os.OSName.StartsWith("Windows 11"))
                    //    machineQueryID = 909285;
                    //else
                    //    machineQueryID = 892116;
                    if (os.OSVersion.StartsWith("10") && os.MDOSID >= 4055)
                        machineQueryID = 909285;
                    else if (os.OSVersion.StartsWith("10") && (3523 <= os.MDOSID && os.MDOSID < 4055))
                        machineQueryID = 892116;
                    else
                    {
                        objRun.AutoReserveMaxMachines = 1;
                        objRun.MaxMachines = 1;
                        machineQueryID = 915030;
                    }
                    break;
            }

            objRun.VMRole = MDO.enuVMRoles.UsePhysical;
            MDO.QueryObject machineQuery = new MDO.QueryObject(machineQueryID);
            objRun.MachineQuery = machineQuery;
        }

        public class BaseInfo
        {
            public string title { get; set; }
            public int caseQueryID { get; set; }
            public int osId { get; set; }
            public int imageId { get; set; }
            public Architecture arch { get; set; }
            public string osVersion { get; set; }
            public int mdOsId { get; set; }
        }

        public class NetCoreInfo
        {
            public BaseInfo baseInfo { get; set; }
            public List<MDPackage> mDPackage { get; set; }
        }

    }
}
