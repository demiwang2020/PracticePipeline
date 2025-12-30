using NETCoreMUStaticLib.DbClassContext;
using NETCoreMUStaticLib.Model;
using NETCoreMUStaticLib.Testcases;
using NETCoreMUStaticLib.UpdateHelper;
using ScorpionDAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NETCoreMUStaticLib
{
    public class NETCoreMUStaticLib
    {
        public static List<TestResult> RunTests(string publishingXml, string filename, List<int> caseToExecute = null, Dictionary<string, string> parameters = null)
        {
            //Build actual update from input publishing xml
            Update actualUpdate = UpdateBuilder.BuildUpdateFromPublishingXml(publishingXml);

            //Translate Input data to inner data structure, and save it to DB for SS case
            InnerData innerData = CommonHelper.ParseUpdateInfo(actualUpdate.ID, actualUpdate.Title, parameters);

            int updateID = SaveUpdateInfoToSelfDB(innerData, filename);
            SaveUpdateInfoToDB(innerData);

            //Build expect update
            Update expectedUpdate = UpdateBuilder.BuildUpdateFromInputData(innerData);

            //Run each case
            if (caseToExecute == null || caseToExecute.Count == 0)
            {
                caseToExecute = NETCoreMUStaticLib.QuerySupportedCases().Select(p => p.ID).ToList();
            }

            List<TestResult> results = new List<TestResult>();
            foreach (var id in caseToExecute)
            {
                results.Add(TestcaseFactory.ExecuteTestcase(id, innerData, expectedUpdate, actualUpdate));
            }
            foreach (var result in results)
            {
                SaveTestInfoToSelfDB(result.CaseName, updateID, result.Result);
                if (!result.Result && result.Failures != null && result.Failures.Count > 0)
                {
                    foreach (KeyValuePair<string, TestFailure> kv in result.Failures)
                    {
                        SaveResultDetailToSelfDB(kv.Key, kv.Value.ExpectResult, kv.Value.ActualResult, result.CaseName, updateID);
                    }
                }
            }

            return results;
        }

        /// <summary>
        /// Get all cases that are active
        /// </summary>
        /// <returns>A list of case info</returns>
        public static List<TTestCaseInfo> QuerySupportedCases()
        {
            return TestcaseFactory.GetAllCases();
        }

        private static void SaveUpdateInfoToDB(InnerData data)
        {
            using (var dbContext = new NetCoreWUSAFXDbContext())
            {
                var records = dbContext.TTestedUpdates.Where(p => p.UpdateID == data.UpdateID);
                if (records.Count() == 0)
                {
                    TTestedUpdate updateInfo = new TTestedUpdate()
                    {
                        UpdateID = data.UpdateID,
                        Title = data.Title,
                        Arch = (int)data.Arch,
                        MajorRelease = data.MajorRelease,
                        ReleaseNumber = data.ReleaseNumber,
                        ReleaseDate = data.ReleaseDate,
                        IsSecurityRelease = data.IsSecurityRelease,
                        IsServerBundle = data.IsServerBundle,
                        IsAUBundle = data.IsAUBundle
                    };

                    dbContext.TTestedUpdates.Add(updateInfo);
                    dbContext.SaveChanges();
                }
            }
        }

        private static int SaveUpdateInfoToSelfDB(InnerData data, string filename)
        {
            TNETCoreUpdate netCoreUpdate = new TNETCoreUpdate();
            using (var dbContext = new PatchTestDataClassDataContext())
            {
                netCoreUpdate.UpdateID = data.UpdateID;
                netCoreUpdate.Title = filename;
                netCoreUpdate.ReleaseDate = data.ReleaseDate;
                dbContext.TNETCoreUpdate.InsertOnSubmit(netCoreUpdate);
                dbContext.SubmitChanges();
            }
            return netCoreUpdate.ID;
        }

        private static void SaveTestInfoToSelfDB(string caseName, int updateID, bool result)
        {
            TNetCoreStaticTest netCoreStaticTest = new TNetCoreStaticTest();
            using (var dbContext = new PatchTestDataClassDataContext())
            {
                netCoreStaticTest.CaseID = dbContext.TNetCoreCaseInfo.Where(item => item.Name.Contains(caseName)).Select(item => item.ID).Single();

                netCoreStaticTest.UpdateID = updateID;
                if (result == true)
                {
                    netCoreStaticTest.Result = "Pass";
                }
                else
                    netCoreStaticTest.Result = "Fail";

                dbContext.TNetCoreStaticTest.InsertOnSubmit(netCoreStaticTest);
                dbContext.SubmitChanges();
            }
        }

        private static void SaveResultDetailToSelfDB(string resultTitle, string expectResult, string actualResult, string caseName, int updateID)
        {
            TNetCoreStaticResultDetail netCoreStaticResultDetail = new TNetCoreStaticResultDetail();
            using (var dbContext = new PatchTestDataClassDataContext())
            {
                netCoreStaticResultDetail.ResultTitle = resultTitle;
                netCoreStaticResultDetail.ExpectResult = expectResult;
                netCoreStaticResultDetail.ActualResult = actualResult;
                netCoreStaticResultDetail.CaseName = caseName;

                var CaseID = dbContext.TNetCoreCaseInfo.Where(item => item.Name.Contains(caseName)).Select(item => item.ID).Single();
                var TestID = dbContext.TNetCoreStaticTest.Where(item => item.CaseID.Equals(CaseID) && item.UpdateID.Equals(updateID)).Select(item => item.ID).Single();
                netCoreStaticResultDetail.TestID = TestID;
                dbContext.TNetCoreStaticResultDetail.InsertOnSubmit(netCoreStaticResultDetail);
                dbContext.SubmitChanges();
            }
        }
    }
}
