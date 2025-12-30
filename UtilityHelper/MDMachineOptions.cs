using System;
using System.Collections.Generic;
using System.Xml;

namespace Helper
{
    public static class MDMachineOptions
    {
        public static readonly string VISTA_WIN7_BLUE_XML = "<root><nebula><InitImage>MaddogNebulaInitDisk.39_NoPatchingDuringReImage</InitImage></nebula></root>";
        public static readonly string RS1_XML = @"<root><AutoPath>\\mdfile3\OrcasTS\files\LabSetup\Release\Current</AutoPath></root>";
        public static readonly string PreWin10_XML = @"<root><AutoPath>\\mdfile3\orcasts\Files\LabSetup</AutoPath></root>";

        private static Dictionary<string, XmlDocument> _dictMachineOptions = new Dictionary<string, XmlDocument>();

        static MDMachineOptions()
        {
            XmlDocument xmlDoc = new XmlDocument();
            //xmlDoc.LoadXml(VISTA_WIN7_BLUE_XML);

            //_dictMachineOptions.Add("Vista", xmlDoc);
            //_dictMachineOptions.Add("Windows 7", xmlDoc);
            //_dictMachineOptions.Add("Windows Blue", xmlDoc);
            //_dictMachineOptions.Add("Windows 8.1", xmlDoc);

            //xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(RS1_XML);
            _dictMachineOptions.Add("RS1", xmlDoc);
            _dictMachineOptions.Add("1607", xmlDoc);
            _dictMachineOptions.Add("RS2", xmlDoc);
            _dictMachineOptions.Add("1703", xmlDoc);
            _dictMachineOptions.Add("RS3", xmlDoc);
            _dictMachineOptions.Add("1709", xmlDoc);
            _dictMachineOptions.Add("RS4", xmlDoc);
            _dictMachineOptions.Add("1803", xmlDoc);
        }

        /// <summary>
        /// Get machine options xml for some specific OS
        /// </summary>
        /// <param name="osNameOrWin10SPLevel">OS name for downlevel os (the OSName field in TOS table), sp level name for Windows10 (sp level could be 'RTM', 'TH1', '1511', 'TH2', '1607', 'RS1')</param>
        /// <returns></returns>
        public static XmlDocument GetMDMachineOptions(string osNameOrWin10SPLevel)
        {
            if (!String.IsNullOrEmpty(osNameOrWin10SPLevel))
            {
                foreach (KeyValuePair<string, XmlDocument> kv in _dictMachineOptions)
                {
                    if (osNameOrWin10SPLevel.Contains(kv.Key))
                    {
                        return kv.Value;
                    }
                }
            }

            return null;
        }
    }
}
