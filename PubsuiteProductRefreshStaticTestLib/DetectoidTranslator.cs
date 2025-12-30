using PubsuiteProductRefreshStaticTestLib.DbClassContext;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PubsuiteProductRefreshStaticTestLib
{
    class DetectoidTranslator
    {
        private static char[] _guidLeadingChars = new char[] { '{', '}' };
        private static string _guidSearchString = @"[A-Fa-f0-9]{8}-[A-Fa-f0-9]{4}-[A-Fa-f0-9]{4}-[A-Fa-f0-9]{4}-[A-Fa-f0-9]{12}";

        public static string GUID2Name(string guid)
        {
            using (var db = new WUSAFXDbContext())
            {
                guid = guid.Trim(_guidLeadingChars);

                var result = db.TDetectoids.Where(p => p.GUID.Equals(guid)).SingleOrDefault();

                return result == null ? null : result.Name;
            }
        }

        public static string TranslateDetectoidsInString(string content)
        {
            Dictionary<string, string> dict = new Dictionary<string, string>();

            foreach (Match match in Regex.Matches(content, _guidSearchString))
            {
                if (!dict.ContainsKey(match.Value))
                {
                    string name = GUID2Name(match.Value);
                    if(!String.IsNullOrEmpty(name))
                        dict.Add(match.Value, name);
                }
            }

            foreach (var s in dict)
            {
                content = content.Replace(s.Key, s.Value);
            }

            return content;
        }
    }
}
