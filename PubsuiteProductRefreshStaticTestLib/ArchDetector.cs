using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PubsuiteProductRefreshStaticTestLib
{
    public class ArchDetector
    {
        public static int ParseArchFromUpdateTitle(string title)
        {
            if (title.Contains("8.1 RT"))
                return 4;

            if (title.Contains("for x64"))
                return 2;

            if (title.Contains("for Itanium-based Systems"))
                return 3;

            if (title.ToLower().Contains("arm64"))
                return 5;

            return 1;
        }

        /// <summary>
        /// Parse Architecture from localized title. The rule is not so strict as enu title
        /// </summary>
        /// <param name="title"></param>
        /// <returns></returns>
        public static int ParseArchFromLocalizedTitle(string title)
        {
            if (title.Contains("8.1 RT"))
                return 4;

            if (title.Contains("x64") /*|| title.Contains("64-разрядных")*/)
                return 2;

            if (title.Contains("Itanium"))
                return 3;

            if (title.ToLower().Contains("arm64"))
                return 5;

            return 1;
        }
    }
}
