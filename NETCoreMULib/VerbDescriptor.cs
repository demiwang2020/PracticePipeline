using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NETCoreMURuntimeLib
{
    enum VerbCategory
    {
        NETCoreRelease,
        CustomCommand,
        ImportRegistry,
        Reboot,
        MDPackage,
    }

    class VerbDescriptor
    {
        public VerbCategory Category { get; private set; }

        public Dictionary<string, string> Tokens { get; private set; }

        private VerbDescriptor()
        {
        }

        private VerbDescriptor(VerbCategory cat)
        {
            Category = cat;

            Tokens = new Dictionary<string, string>();
        }

        public static VerbDescriptor ParseVerbFromString(string vebDes)
        {
            string[] splitData = vebDes.Split(new char[] { ',', '=' });

            VerbCategory category = ParseCategory(splitData[0]);

            VerbDescriptor vebObj = new VerbDescriptor(category);

            for (int i = 0; i < splitData.Length - 1; i += 2)
            {
                vebObj.Tokens.Add(splitData[i].Trim(), splitData[i + 1].Trim());
            }

            return vebObj;
        }

        private static VerbCategory ParseCategory(string firstName)
        {
            VerbCategory category = VerbCategory.NETCoreRelease;

            switch (firstName)
            {
                case "Release":
                    category = VerbCategory.NETCoreRelease;
                    break;

                case "Command":
                    category = VerbCategory.CustomCommand;
                    break;

                case "ImportRegistry":
                    category = VerbCategory.ImportRegistry;
                    break;

                case "Reboot":
                    category = VerbCategory.Reboot;
                    break;

                case "MDPackage":
                    category = VerbCategory.MDPackage;
                    break;

                default:
                    throw new NotSupportedException(firstName + "is not supported");
            }

            return category;
        }
    }
}
