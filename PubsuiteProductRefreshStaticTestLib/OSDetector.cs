using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PubsuiteProductRefreshStaticTestLib
{
    public class OSDetector
    {
        private static readonly string os2008 = "Server 2008";
        private static readonly string os2008R2 = "Server 2008 R2";

        private static readonly string os2012 = "Server 2012";
        private static readonly string os2012R2 = "Server 2012 R2";

        private static readonly string osWES09 = "WES09";
        //private static readonly string osPosReady2009 = "POSReady 2009";
        private static readonly string os2003 = "Server 2003";
        private static readonly string osWin8Emb = "Windows Embedded 8";
        private static readonly string osWin7 = "Windows 7";
        private static readonly string osWin7Emb = "Windows Embedded Standard 7";
        private static readonly string osBlue = "Windows 8.1";
        private static readonly string osWin81Emb = "Windows Embedded 8.1";
        private static readonly string osWin81Emb1 = "Windows 8.1 Embedded";
        private static readonly string osRS5 = "Windows 10 Version 1809";
        private static readonly string os2019 = "Server 2019";
        private static readonly string osRS1 = "Windows 10 Version 1607";
        private static readonly string os2016 = "Server 2016";
        private static readonly string osRS2 = "Windows 10 Version 1703";
        private static readonly string osRS3 = "Windows 10 Version 1709";
        private static readonly string osRS4 = "Windows 10 Version 1803";
        private static readonly string osRS6 = "Windows 10 Version 1903";

        private static readonly string osRS4Server = "Windows Server 2016 (1803)";
        private static readonly string osRS6Server = "Windows Server 2019 (1903)";
        private static readonly string osRS6Server2 = "Windows Server, version 1903";

        private static readonly string os19H2 = "Windows 10 Version 1909";
        private static readonly string os19H2Server = "Windows Server, version 1909";

        private static readonly string os20H1 = "Windows 10 Version 2004";
        private static readonly string os20H1Server = "Windows Server, version 2004";

        private static readonly string os20H2 = "Windows 10 Version 20H2";
        private static readonly string os20H2Server = "Windows Server, version 20H2";

        private static readonly string osAzureHCI1809 = "Azure Stack HCI, version 20H2";

        private static readonly string os21H1 = "Windows 10 Version 21H1";
        private static readonly string os21H2 = "Windows 10 Version 21H2";
        private static readonly string os22H2 = "Windows 10 Version 22H2";

        private static readonly string osServer2022 = "Microsoft server operating system version 21H2";
        private static readonly string osServerASZOS22H2 = "Microsoft server operating system, version 22H2";

        private static readonly string osServerASZOS23H2 = "Microsoft server operating system, version 23H2";

        private static readonly string osWinSV21H2 = "Windows 11";
        private static readonly string osWin1122H2 = "Windows 11, version 22H2";

        private static readonly string osWin10VersionNext = "Windows Version Next";
        private static readonly string osWin10SvrVersionNext = "Windows Server Version Next";

        public static string ParseTargetOSFromUpdateTitle(string title)
        {
            if (title.Contains(os2008) && !title.Contains(os2008R2))
                return os2008;

            if (title.Contains(os2012) && !title.Contains(os2012R2))
                return os2012;

            if (title.Contains(osWES09))
                return osWES09;

            if (title.Contains(os2003))
                return os2003;

            if (title.Contains(osWin7Emb))
                return osWin7Emb;

            if (title.Contains(osWin7))
            {
                //if (title.Contains(osWin7Emb))
                //    return osWin7Emb;
                //else
                //    return osWin7;
                return osWin7;
            }

            if (title.Contains(os2008R2))
                return os2008R2;

            if (title.Contains(osWin81Emb))
                return osWin81Emb;

            if (title.Contains(osWin81Emb1))
                return osWin81Emb1;

            if (title.Contains(osWin8Emb))
                return osWin8Emb;

            if (title.Contains(osBlue))
                return osBlue;

            if (title.Contains(os2012R2))
                return os2012R2;

            if (title.Contains(osRS5))
                return osRS5;

            if (title.Contains(osRS6Server))
                return osRS6Server;

            if (title.Contains(osRS6Server2))
                return osRS6Server2;

            if (title.Contains(os2019))
                return os2019;

            if (title.Contains(osRS1))
                return osRS1;

            if (title.Contains(osRS4Server))
                return osRS4Server;

            if (title.Contains(os2016))
                return os2016;

            if (title.Contains(osRS2))
                return osRS2;

            if (title.Contains(osRS3))
                return osRS3;

            if (title.Contains(osRS4))
                return osRS4;

            if (title.Contains(osRS6))
                return osRS6;

            if (title.Contains(os19H2))
                return os19H2;

            if (title.Contains(os19H2Server))
                return os19H2Server;

            if (title.Contains(os20H1))
                return os20H1;
            if (title.Contains(os20H1Server))
                return os20H1Server;

            if (title.Contains(os20H2))
                return os20H2;
            if (title.Contains(os20H2Server))
                return os20H2Server;

            if (title.Contains(osAzureHCI1809))
                return osAzureHCI1809;

            if (title.Contains(os21H1))
                return os21H1;

            if (title.Contains(os21H2))
                return os21H2;

            if (title.Contains(os22H2))
                return os22H2;

            if (title.Contains(osServer2022))
                return osServer2022;

            if (title.Contains(osServerASZOS22H2))
                return osServerASZOS22H2;

            if (title.Contains(osWin1122H2))
                return osWin1122H2;

            if (title.Contains(osWinSV21H2))
                return osWinSV21H2;

            if (title.Contains(osWin10VersionNext))
                return osWin10VersionNext;
            if (title.Contains(osWin10SvrVersionNext))
                return osWin10SvrVersionNext;

            if (title.Contains(osServerASZOS23H2))
                return osServerASZOS23H2;

            return null;
        }

        public static List<string> ParseAllTargetOSFromUpdateTitle(string title)
        {
            List<string> osList = new List<string>();

            if (title.Contains(os2008R2))
            {
                osList.Add(os2008R2);
            }
            else if (title.Contains(os2008))
            {
                osList.Add(os2008);
            }

            if (title.Contains(os2012R2))
            {
                osList.Add(os2012R2);
            }
            else if (title.Contains(os2012))
            {
                osList.Add(os2012);
            }

            if (title.Contains(osWES09))
                osList.Add(osWES09);

            if (title.Contains(os2003))
                osList.Add(os2003);

            if (title.Contains(osWin7Emb))
                osList.Add(osWin7Emb);

            if (title.Contains(osWin7))
                osList.Add(osWin7);

            if (title.Contains(osWin8Emb))
                osList.Add(osWin8Emb);

            if (title.Contains(osBlue))
                osList.Add(osBlue);

            if (title.Contains(osWin81Emb))
                osList.Add(osWin81Emb);

            if (title.Contains(osRS5))
                osList.Add(osRS5);

            if (title.Contains(osRS6Server))
                osList.Add(osRS6Server);

            if (title.Contains(osRS6Server2))
                osList.Add(osRS6Server2);

            if (title.Contains(os2019))
                osList.Add(os2019);

            if (title.Contains(osRS1))
                osList.Add(osRS1);

            if (title.Contains(osRS4Server))
                osList.Add(osRS4Server);

            if (title.Contains(os2016))
                osList.Add(os2016);

            if (title.Contains(osRS2))
                osList.Add(osRS2);

            if (title.Contains(osRS3))
                osList.Add(osRS3);

            if (title.Contains(osRS4))
                osList.Add(osRS4);

            if (title.Contains(osRS6))
                osList.Add(osRS6);

            if (title.Contains(os19H2))
                osList.Add(os19H2);

            if (title.Contains(os19H2Server))
                osList.Add(os19H2Server);

            if (title.Contains(os20H1))
                osList.Add(os20H1);

            if (title.Contains(os20H1Server))
                osList.Add(os20H1Server);

            if (title.Contains(os20H2))
                osList.Add(os20H2);
            if (title.Contains(os20H2Server))
                osList.Add(os20H2Server);

            if (title.Contains(os21H1))
                osList.Add(os21H1);

            if (title.Contains(os21H2))
                osList.Add(os21H2);

            if (title.Contains(os22H2))
                osList.Add(os22H2);

            if (title.Contains(osServer2022))
                osList.Add(osServer2022);

            if (title.Contains(osServerASZOS22H2))
                osList.Add(osServerASZOS22H2);

            if (title.Contains(osWin1122H2))
                osList.Add(osWin1122H2);
            else if (title.Contains(osWinSV21H2))
                osList.Add(osWinSV21H2);

            if (title.Contains(osAzureHCI1809))
                osList.Add(osAzureHCI1809);

            if (title.Contains(osWin10VersionNext))
                osList.Add(osWin10VersionNext);
            if (title.Contains(osWin10SvrVersionNext))
                osList.Add(osWin10SvrVersionNext);

            if (title.Contains(osServerASZOS23H2))
                osList.Add(osServerASZOS23H2);

            return osList;
        }

        /// <summary>
        /// Get a list of uniformed names of some server OS, which only have 18 languages
        /// </summary>
        public static List<string> GetServerNamesWithSpecialLocalization()
        {
            return new List<string>()
            {
                os2008
            };
        }

        public static bool IsWin10OS(int osID)
        {
            return (osID > 5 && osID != 1025 && osID != 1026 && osID != 1031 && osID != 1040);
        }
    }
}
