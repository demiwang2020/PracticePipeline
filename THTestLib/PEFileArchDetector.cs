using Helper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace THTestLib
{
    public class PEArchDetector
    {
        private static List<string> _archIndependentDotNetFiles;

        /// <summary>
        /// Get machine code field from PE file
        /// </summary>
        public static Architecture GetPEArch(string peFilePath)
        {
            ushort machineCode = GetPEMachineField(peFilePath);
            switch (machineCode)
            {
                case 0x8664:
                    return Architecture.AMD64;

                case 0x14c:
                    return Architecture.X86;

                case 0x200:
                    return Architecture.IA64;

                case 0x1c0:
                case 0x1c4:
                    return Architecture.ARM;

                case 0xAA64:
                    return Architecture.ARM64;

                default:
                    return (Architecture)machineCode;
            }
        }

        /// <summary>
        /// Detect if a .NET binary is indpendent to CPU type
        /// </summary>
        public static bool IsPEIndependentFromArch(string peFileName)
        {
            if (_archIndependentDotNetFiles == null || _archIndependentDotNetFiles.Count == 0)
            {
                string filePath = System.Configuration.ConfigurationManager.AppSettings["ArchIndependentFiles"];
                string line = null;
                using (StreamReader sr = new StreamReader(filePath))
                {
                    _archIndependentDotNetFiles = new List<string>();
                    while((line = sr.ReadLine()) != null)
                    {
                        _archIndependentDotNetFiles.Add(line.ToLowerInvariant());
                    }
                }
            }

            return _archIndependentDotNetFiles.Contains(peFileName.ToLowerInvariant());
        }

        private static ushort GetPEMachineField(string peFilePath)
        {
            using (FileStream fs = new FileStream(peFilePath, FileMode.Open, FileAccess.Read))
            {
                using (BinaryReader br = new BinaryReader(fs))
                {
                    br.ReadBytes(60);
                    int peOffset = br.ReadInt32();

                    br.ReadBytes(peOffset - 64);
                    br.ReadInt32();

                    return (ushort)br.ReadInt16();
                }
            }
        }
    }
}
