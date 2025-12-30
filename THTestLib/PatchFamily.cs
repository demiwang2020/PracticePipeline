using System.Collections.Generic;
using Helper;
using System.Linq;

namespace THTestLib
{
    class PatchFamily
    {
        private static string _Tool_Path = System.Configuration.ConfigurationManager.AppSettings["PatchFamilyToolLocation"];
        private static string _Tool_Arg = "/filelist:{0} /arch:{1} /sku:{2} /patchname:{3} /collect:true";

        public static List<string> GetAllFilesInSamePF(string fileName, string sku, string patchName = null)
        {
            try
            {
                string outputX86 = CallPatchFamily(fileName, sku, Architecture.X86, patchName);
                string outputX64 = CallPatchFamily(fileName, sku, Architecture.AMD64, patchName);

                List<string> files = new List<string>();
                files.AddRange(ReadPFOutput(outputX86));
                files.AddRange(ReadPFOutput(outputX64));

                return files.Distinct().ToList();
            }
            catch
            {
            }

            return null;
        }

        private static string CallPatchFamily(string fileName, string sku, Architecture arch, string patchName)
        {
            string skuForPF = "NDP" + sku.Replace(".", string.Empty);
            if (skuForPF == "NDP20" || skuForPF == "NDP30")
                skuForPF = skuForPF + "SP2";
            else if(skuForPF == "NDP35")
                skuForPF = skuForPF + "SP1";

            string args = string.Format(_Tool_Arg, fileName, arch, skuForPF, patchName);

            System.Diagnostics.ProcessStartInfo procStartInfo = new System.Diagnostics.ProcessStartInfo(_Tool_Path, args);

            // The following commands are needed to redirect the standard output.
            // This means that it will be redirected to the Process.StandardOutput StreamReader.
            procStartInfo.RedirectStandardOutput = true;
            procStartInfo.UseShellExecute = false;
            // Do not create the black window.
            procStartInfo.CreateNoWindow = true;

            // Now we create a process, assign its ProcessStartInfo and start it
            System.Diagnostics.Process proc = new System.Diagnostics.Process();
            proc.StartInfo = procStartInfo;
            proc.Start();

            // Get the output into a string
            return proc.StandardOutput.ReadToEnd();
        }

        private static List<string> ReadPFOutput(string output)
        {
            int index = output.IndexOf("Files in same PF: ");
            if (index > 0)
            {
                string filesString = output.Substring(index + 18).Trim();

                if(!string.IsNullOrEmpty(filesString))
                    return filesString.Split(new char[] { ',' }).ToList();
            }

            return new List<string>();
        }
    }
}
