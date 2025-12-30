using System;
using System.Linq;
using System.IO;
using Microsoft.Test.DevDiv.SAFX.CommonLibraries.Utilities;

namespace Helper
{
    public class PatchTechnologyDetector
    {
        /// <summary>
        /// Detect technology the patch used: MSI, CBS, OCM
        /// </summary>
        /// <param name="patchName">Name file name</param>
        /// <returns>return technology the patch uesed</returns>
        public static PatchTechnology Detect(string patchFileName)
        {
            if (string.IsNullOrEmpty(patchFileName))
                throw new ArgumentNullException("Patch file Name");

            patchFileName = patchFileName.ToLowerInvariant();

            string extension = Path.GetExtension(patchFileName);
            if (string.IsNullOrEmpty(extension))
            {
                throw new Exception("File name should contain extension.");
            }

            if (extension.Equals(".msu"))
                return PatchTechnology.CBS;
            else if (extension.Equals(".exe"))
            {
                string[] tempArray = patchFileName.Split('-');

                if (tempArray.Contains("ocm"))
                    return PatchTechnology.OCM;
                else if (tempArray.Contains("windowsserver2003"))
                    return PatchTechnology.OCM;
                else
                    return PatchTechnology.MSP;
            }
            else
                throw new NotSupportedException(string.Format("This kind of file {0} is not supported now.", extension));
        }
    }
}
