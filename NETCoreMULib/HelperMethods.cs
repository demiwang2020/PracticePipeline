using Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NETCoreMURuntimeLib
{
    public class HelperMethods
    {
        public static bool IsServerUpdate(string title)
        {
            return title.Contains("Server");
        }

        public static Architecture DetectUpdateTargetArch(string title)
        {
            if (title.Contains("x64"))
                return Architecture.AMD64;

            if (title.ToLower().Contains("arm64"))
                return Architecture.ARM64;

            return Architecture.X86;
        }

        public static Architecture ConvertFormat(string osarch)
        {
            if (osarch == "X86")
                return Architecture.X86;

            if (osarch == "AMD64")
                return Architecture.AMD64;

            if (osarch == "ARM64")
                return Architecture.ARM64;

            if (osarch == "X86,AMD64")
                return Architecture.ARM64;

            if (osarch == "X86,ARM64")
                return Architecture.AMD64;

            if (osarch == "AMD64,ARM64")
                return Architecture.X86;
            return Architecture.IA64;
        }

        public static Dictionary<string, string> ParseCaseSpecificData(string specificData)
        {
            string[] splitData = specificData.Split(new char[] { '#', '=' });


            Dictionary<string, string> retResult = new Dictionary<string, string>();
            for (int i = 0; i < splitData.Length - 1; i += 2)
            {
                retResult.Add(splitData[i].Trim(), splitData[i + 1].Trim());
            }

            return retResult;
        }

        public static string ReploaceCaseSpecificData(string content, Dictionary<string, string> caseSpecificData)
        {
            if (!content.Contains('%'))
                return content;

            foreach (var kv in caseSpecificData)
            {
                string token = String.Format("%{0}%", kv.Key);
                if (content.Contains(token))
                    content = content.Replace(token, kv.Value);
            }

            return content;
        }
    }
}
