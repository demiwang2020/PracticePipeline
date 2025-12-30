using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NETCoreMURuntimeLib.Maddog
{
    class MDPackage
    {
        public int ID;
        public Dictionary<string, string> Tokens;

        public const int ID_REBOOT = 10420;

        private MDPackage()
        {
        }

        private MDPackage(int id)
        {
            ID = id;
        }

        private void AddToken(string name, string value)
        {
            if (Tokens == null)
                Tokens = new Dictionary<string, string>();

            Tokens[name] = value;
        }

        public static MDPackage CreateCommandPackage(string cmdline, bool spawnWithNativeArch = false)
        {
            MDPackage p = new MDPackage(10494);

            p.AddToken("CommandLine", cmdline);
            if (spawnWithNativeArch)
                p.AddToken("SpawnWithNativeArchitecture", "True");

            return p;
        }

        public static MDPackage CreateEnvironmentVariablePackage(string variableName, string variableValue)
        {
            MDPackage p = new MDPackage(10428);

            p.AddToken("VariableName", variableName);
            p.AddToken("VariableValue", variableValue);

            return p;
        }

        public static MDPackage CreateImportRegFilePackage(string filePath, bool spawnWithNativeArch = false)
        {
            MDPackage p = new MDPackage(10427);

            p.AddToken("File", filePath);
            if (spawnWithNativeArch)
                p.AddToken("SpawnWithNativeArchitecture", "True");

            return p;
        }

        public static MDPackage CreateCommonPackage(int packageID)
        {
            return new MDPackage(packageID);
        }

        public static MDPackage CreateCommonPackage(int packageID, string tokenName, string tokenValue)
        {
            MDPackage p = new MDPackage(packageID);

            p.AddToken(tokenName, tokenValue);

            return p;
        }

        public static MDPackage CreateCommonPackage(int packageID, string tokenName1, string tokenValue1, string tokenName2, string tokenValue2)
        {
            MDPackage p = new MDPackage(packageID);

            p.AddToken(tokenName1, tokenValue1);
            p.AddToken(tokenName2, tokenValue2);

            return p;
        }
    }
}
