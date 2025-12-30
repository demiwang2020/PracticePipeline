using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;
using System.Text.RegularExpressions;
using System.Configuration;
using Helper;

namespace WUTestManagerLib
{
    public static class SUSDB
    {
        /// <summary>
        /// Gets the SS info for the given KB from the pubv5winse database
        /// </summary>
        /// <param name="kb">Target KB number</param>
        /// <param name="arch">ARCH of target KB number</param>
        /// <param name="kbstoreturn">String containing the specific SS KBs you want info back on</param>
        /// <returns></returns>
        public static List<SSKB> GetSSInfoForKB(string kb, Architecture arch, string kbstoreturn)
        {
            try
            {
                bool returnonlykbsinstring = false;
                if (!String.IsNullOrEmpty(kbstoreturn))
                    returnonlykbsinstring = true;
                Dictionary<Guid, SSKB> results = new Dictionary<Guid, SSKB>();
                List<int> revisionids = new List<int>();
                List<SSKB> returnedresults = new List<SSKB>();
                bool foundrecord = false;
                using (var context = new SUSDBEntities())
                {
                    var returnedrevisions = context.tbKBArticleForRevisions.Where<tbKBArticleForRevision>(k => k.KBArticleID == kb);
                    foreach (var revision in returnedrevisions)
                    {
                        revisionids.Add(revision.RevisionID);
                    }

                    foreach (int revisionid in revisionids)
                    {
                        List<Guid> ssguids = new List<Guid>();
                        var ssupdateids = context.tbRevisionSupersedesUpdates.Where<tbRevisionSupersedesUpdate>(u => u.RevisionID == revisionid);
                        foreach(var ssupdateid in ssupdateids)
                        {
                            ssguids.Add(ssupdateid.SupersededUpdateID);
                            foundrecord = true;
                        }

                        foreach (Guid ssguid in ssguids)
                        {
                            if (!results.ContainsKey(ssguid))
                            {
                                var sstitles = context.tbUpdateMetaDatas.Where<tbUpdateMetaData>(t => t.PackageId == ssguid).OrderByDescending(t => t.RevisionNumber);
                                string title = string.Empty;
                                foreach(var sstitle in sstitles)
                                {
                                    title = sstitle.PackageTitle;
                                    break;
                                }
                                Regex findkb = new Regex("KB\\d+");
                                Match kbmatch = findkb.Match(title);
                                string sskbnumber = kbmatch.Value.Replace("KB", "");
                                Architecture targetarch;

                                if (title.ToLower().Contains("x64"))
                                    targetarch = Architecture.AMD64;
                                else if (title.ToLower().Contains("ia64") || title.ToLower().Contains("itanium"))
                                    targetarch = Architecture.IA64;
                                else if (title.ToLower().Contains("windows rt"))
                                    targetarch = Architecture.ARM;
                                else
                                    targetarch = Architecture.X86;

                                if (arch == targetarch)
                                {
                                    if (returnonlykbsinstring)
                                    {
                                        if (kbstoreturn.Contains(sskbnumber))
                                        {
                                            results.Add(ssguid, new SSKB(sskbnumber, ssguid));
                                        }
                                    }
                                    else
                                    {
                                        results.Add(ssguid, new SSKB(sskbnumber, ssguid));
                                    }
                                }
                            }
                        }
                    }
                }
                returnedresults.AddRange(results.Values);
                if (!foundrecord)
                {
                    Console.WriteLine("No SS KBs found for KB");
                    return null;
                }
                return returnedresults;
            }
            catch (Exception e)
            {
                throw new Exception("Could not connect to SUSDB database: " + e.Message + "\r\n" + e.StackTrace);
            }
        }
        public static List<SSKB> GetSSInfoForKB(string kb, Architecture arch)
        {
            return GetSSInfoForKB(kb, arch, null);
        }

        /// <summary>
        /// Gets all the KBs that are superseding the given guid
        /// </summary>
        /// <param name="kbguid">target kb guid</param>
        /// <param name="arch">target arch</param>
        /// <returns></returns>
        public static List<SSKB> GetSSingKBsForKB(Guid kbguid)
        {
            try
            {
                List<int> revisionids = new List<int>();
                List<SSKB> ssingkbsresults = new List<SSKB>();
                bool foundrecord = false;
                using (var context = new SUSDBEntities())
                {
                    var ssrevisionids = context.tbRevisionSupersedesUpdates.Where<tbRevisionSupersedesUpdate>(r => r.SupersededUpdateID == kbguid);
                    foreach(var ssrevisionid in ssrevisionids)
                    {
                        revisionids.Add(ssrevisionid.RevisionID);
                    }
                    foreach (int revisionid in revisionids)
                    {
                        var ssupdatedids = context.tbKBArticleForRevisions.Where<tbKBArticleForRevision>(k => k.RevisionID == revisionid);
                        string kbarticle = string.Empty;
                        foreach(var ssupdateid in ssupdatedids)
                        {
                            kbarticle = ssupdateid.KBArticleID;
                            foundrecord = true;
                        }

                        var updateguids = context.ivwApiUpdateRevisions.Where<ivwApiUpdateRevision>(g => g.RevisionID == revisionid);
                        Guid ssguid = Guid.Empty;
                        foreach(var updateguid in updateguids)
                        {
                            bool islatestrevision = Convert.ToBoolean(updateguid.IsLatestRevision);
                            if(islatestrevision)
                            {
                                ssguid = updateguid.UpdateID;
                            }
                        }
                        if (foundrecord && ssguid != Guid.Empty)
                        {
                            ssingkbsresults.Add(new SSKB(kbarticle, ssguid));
                        }
                    }
                }
                if (!foundrecord)
                {
                    return null;
                }
                return ssingkbsresults;
            }
            catch (Exception e)
            {
                throw new Exception("Could not connect to SUSDB database: " + e.Message + "\r\n" + e.StackTrace);
            }
        }

        public static bool TitleCompare(Guid packageguid, string expectedtitle, out string actualtitle)
        {
            using(var context = new SUSDBEntities())
            {
                var metadatarecord = context.tbUpdateMetaDatas.Where<tbUpdateMetaData>(t => t.PackageId == packageguid).OrderByDescending(t => t.RevisionNumber).FirstOrDefault();
                actualtitle = metadatarecord==null?string.Empty:metadatarecord.PackageTitle;
            }
            return expectedtitle == actualtitle;
        }
    }
}
