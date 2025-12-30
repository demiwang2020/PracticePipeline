using Helper;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NETCoreMUStaticLib
{
    class CommonHelper
    {
        public static bool TooManyAdditionalSpaces(string text)
        {
            bool tooMany = false;

            int extraSpaces = System.Text.RegularExpressions.Regex.Matches(text, @"   ").Count;
            if (extraSpaces > 0)
                tooMany = true;

            extraSpaces = System.Text.RegularExpressions.Regex.Matches(text, @"  ").Count;
            if (extraSpaces > 1)
                tooMany = true;

            if (System.Text.RegularExpressions.Regex.IsMatch(text, @"[\t\r\n\v\f]"))
                tooMany = true;

            return tooMany;
        }

        public static string GetManagedId()
        {
            if (Regex.Match(Environment.GetEnvironmentVariable("COMPUTERNAME"), "DotNetPatchTest", RegexOptions.IgnoreCase).Success)
            {
                return ConfigurationManager.AppSettings["gofxservinfra01ManagedId"];
            }
            return string.Empty;
        }

        public static void Createtxt(string content)
        {

            string filePath = "\\\\dotnetpatchtest\\F\\OutPutFile\\Keyvault.txt"; // 文件路径

            // 检查文件是否存在并且不为空
            if (File.Exists(filePath) && new FileInfo(filePath).Length > 0)
            {
                // 文件存在且不为空，追加内容
                File.AppendAllText(filePath, content);
               
            }
            else
            {
                // 文件不存在或为空，直接写入（创建或覆盖）
                File.WriteAllText(filePath, content);
            }
        }

        public static DateTime TTGLString2DateTime(string ttgl, out bool timezoneSet)
        {
            // TimeToGoLive="2020-05-12T10:00:00.0000000-07:00"
            // TimeToGoLive="2020-05-12T10:00:00

            timezoneSet = false;

            int dateIndex = ttgl.IndexOf('T');
            string date = ttgl.Substring(0, dateIndex);

            string[] dateSpit = date.Split(new char[] { '-' });

            int timezoneIndex = ttgl.IndexOf('+', dateIndex + 1);
            if(timezoneIndex < 0)
                timezoneIndex = ttgl.IndexOf('-', dateIndex + 1);

            int timezone = 0;
            if (timezoneIndex > 0)
            {
                timezoneSet = true;
                timezone = Convert.ToInt32(ttgl.Substring(timezoneIndex).Split(new char[] { ':' })[0]);
            }

            string time = timezoneIndex > 0 ? ttgl.Substring(dateIndex + 1, timezoneIndex - dateIndex - 1) : ttgl.Substring(dateIndex + 1);
            string[] timeSplit = time.Split(new char[] { ':' });

            return new DateTime(Convert.ToInt32(dateSpit[0]),
                Convert.ToInt32(dateSpit[1]),
                Convert.ToInt32(dateSpit[2]),
                Convert.ToInt32(timeSplit[0]) - timezone,
                Convert.ToInt32(timeSplit[1]),
                Convert.ToInt32(timeSplit[2].Replace(".", String.Empty)),
                DateTimeKind.Utc);
        }


        public static bool IsServerUpdate(string title)
        {
            return title.Contains("Server");
        }

        public static bool IsSecurityRelease(string title)
        {
            return title.Contains("Security Update");
        }

        public static Architecture DetectUpdateTargetArch(string title)
        {
            if (title.Contains("x64"))
                return Architecture.AMD64;

            if (title.ToLower().Contains("arm64"))
                return Architecture.ARM64;

            return Architecture.X86;
        }

        public static string DetectReleaseVersionFromTitle(string title)
        {
            // 2020-12 .NET 5.0.1 Update for x64 Client
            // 2020-12 .NET 5.0.1 Update for arm64 Client
            //2020-12 .NET 5.0.1 Update for x64 Server

            string[] temp = title.Split(new char[] { ' ' });

            string version = temp.Length > 3 ? temp[2] : null;
            if (version == "Core")
                return temp.Length > 4 ? temp[3] : null;
            else
                return version;
        }

        public static string DetectReleaseDateFromTitle(string title)
        {
            int index = title.IndexOf(' ');

            return title.Substring(0, index);
        }

        public static InnerData ParseUpdateInfo(string updateID, string title, Dictionary<string, string> paramenters)
        {
            InnerData innerData = new InnerData();
            innerData.Title = title;
            innerData.UpdateID = updateID.ToLower();
            innerData.IsSecurityRelease = CommonHelper.IsSecurityRelease(title);
            innerData.IsServerBundle = CommonHelper.IsServerUpdate(title);
            innerData.Arch = CommonHelper.DetectUpdateTargetArch(title);
            innerData.ReleaseNumber = CommonHelper.DetectReleaseVersionFromTitle(title);
            innerData.ReleaseDate = CommonHelper.DetectReleaseDateFromTitle(title);
            innerData.MajorRelease = innerData.ReleaseNumber.Substring(0, 3);
            innerData.IsAUBundle = false;

            if (paramenters != null)
            {
                foreach (var kv in paramenters)
                    innerData.Parameters.Add(kv.Key, kv.Value);

                // detect if AU bundle
                if (innerData.IsServerBundle &&
                    innerData.Parameters.ContainsKey("AU") &&
                    Convert.ToBoolean(innerData.Parameters["AU"])) 
                {
                    innerData.IsAUBundle = true;
                }
            }

            return innerData;
        }

        public static string Arch2String(Architecture arch)
        {
            if (arch == Architecture.AMD64)
                return "X64";
            else
                return arch.ToString();
        }

        public static int CompareCoreReleaseNumber(string number1, string number2)
        {
            // 2.1.9...
            if (number1.Length == number2.Length)
                return String.CompareOrdinal(number1, number2);
            else
            {
                var privateBuildNumber1 = number1.Split(new char[] { '.' }).Last();
                var privateBuildNumber2 = number2.Split(new char[] { '.' }).Last();

                var n1 = Convert.ToInt32(privateBuildNumber1);
                var n2 = Convert.ToInt32(privateBuildNumber2);

                return n1 - n2;
            }
        }
    }
}
