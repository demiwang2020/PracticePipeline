using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PubsuiteStaticTestLib
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

        public static DateTime TTGLString2DateTime(string ttgl)
        {
            // TimeToGoLive="2020-05-12T10:00:00.0000000-07:00"

            int dateIndex = ttgl.IndexOf('T');
            string date = ttgl.Substring(0, dateIndex);

            string[] dateSpit = date.Split(new char[] { '-' });

            int timezoneIndex = ttgl.IndexOf('+', dateIndex + 1);
            if(timezoneIndex < 0)
                timezoneIndex = ttgl.IndexOf('-', dateIndex + 1);

            int timezone = 0;
            if (timezoneIndex > 0)
            {
                timezone = Convert.ToInt32(ttgl.Substring(timezoneIndex).Split(new char[] { ':' })[0]);
            }

            string time = ttgl.Substring(dateIndex + 1, timezoneIndex - dateIndex - 1);
            string[] timeSplit = time.Split(new char[] { ':' });

            return new DateTime(Convert.ToInt32(dateSpit[0]),
                Convert.ToInt32(dateSpit[1]),
                Convert.ToInt32(dateSpit[2]),
                Convert.ToInt32(timeSplit[0]) - timezone,
                Convert.ToInt32(timeSplit[1]),
                Convert.ToInt32(timeSplit[2].Replace(".", String.Empty)),
                DateTimeKind.Utc);
        }
    }
}
