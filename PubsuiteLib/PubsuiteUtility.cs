using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UpdateServices.Publishing.Schemas;
using Microsoft.UpdateServices.Publishing.Client;
using System.IO;
using System.Xml.Serialization;

namespace PubsuiteLib
{
    public static class PubsuiteUtility
    {
        private static PublishingClient _pubClient;

        static PubsuiteUtility()
        {
            _pubClient = new PublishingClient();
        }

        public static Update GetUpdateByID(string id)
        {
            var pubxml = _pubClient.GetPublishingXml(id, Microsoft.UpdateServices.Publishing.PublishingXmlType.Local);
            var xs = new XmlSerializer(typeof(Update));
            using (TextReader tr = new StringReader(pubxml))
            {
                return (Update)xs.Deserialize(tr);
            }
        }

        public static PublishingQueryResult QueryUpdatesByKBArticle(string kb)
        {
            if (kb.StartsWith("KB", StringComparison.InvariantCultureIgnoreCase))
                kb = kb.Substring(2);
            
            var query = new StringBuilder();
            query.Append("<PublishingQuery>");
            query.Append("<KBArticleIDs>");
            query.AppendFormat("<KBArticleID>{0}</KBArticleID>", kb);
            query.Append("</KBArticleIDs>");
            query.Append("</PublishingQuery>");

            return QueryUpdates(query.ToString());
        }

        public static PublishingQueryResult QuerySupersedingUpdates(string id)
        {
            var query = new StringBuilder();
            query.Append("<PublishingQuery>");
            query.AppendFormat("<SupersededUpdateID>{0}</SupersededUpdateID>", id);
            query.Append("</PublishingQuery>");

            return QueryUpdates(query.ToString());
        }

        private static PublishingQueryResult QueryUpdates(string query)
        {
            var xml = _pubClient.Query(query);
            var xs = new XmlSerializer(typeof(PublishingQueryResult));

            using (TextReader tr = new StringReader(xml))
            {
                return (PublishingQueryResult)xs.Deserialize(tr);
            }
        }
    }
}
