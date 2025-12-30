using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PubsuiteStaticTestLib.Testcases;
using PubsuiteStaticTestLib.Model;
using PubsuiteStaticTestLib.UpdateHelper;
using System.IO;
using ScorpionDAL;

namespace PubsuiteStaticTestLib
{
    public class PubsuiteStaticTestLib
    {
        /// <summary>
        /// Run WUSAFX test
        /// </summary>
        /// <param name="inputData">Expected update info</param>
        /// <param name="caseToExecute">ID of cases to be executed. If null, all active case will be executed</param>
        /// <returns>Test results</returns>
        public static List<TestResult> RunTests(InputData inputData,int updateID, List<int> caseToExecute = null)
        //public static List<TestResult> RunTests(InputData inputData, int updateID)
        //public static List<TestResult> RunTests(InputData inputData, List<int> caseToExecute = null)
        {
            //Create expect update
            //Update expectedUpdate = UpdateBuilder.BuildExpectedUpdateFromInputData(inputData);

            //Get actual update
            Update actualUpdate = String.IsNullOrEmpty(inputData.PublishingXmlContent) ?
                UpdateBuilder.QueryUpdateFromGUID(inputData.UpdateID) :
                UpdateBuilder.BuildUpdateFromPublishingXml(inputData.PublishingXmlContent);

            //Run each case
            if (caseToExecute == null || caseToExecute.Count == 0)
            {
                caseToExecute = QuerySupportedCases().Select(p => p.ID).ToList();
            }

            List<TestResult> results = new List<TestResult>();
            
            foreach (var id in caseToExecute)
            {
                    //results.Add(TestcaseFactory.ExecuteTestcase(id, inputData, expectedUpdate, actualUpdate));
            }
            foreach (var result in results)
            {
                SaveTestInfoToSelfDB(result.CaseName, inputData, updateID, result.Result);
                if (!result.Result && result.Failures != null && result.Failures.Count > 0)
                {
                    foreach (KeyValuePair<string, TestFailure> kv in result.Failures)
                    {
                        SaveResultDetailToSelfDB(kv.Key, kv.Value.ExpectResult, kv.Value.ActualResult, result.CaseName, inputData);
                    }
                }
            }

            return results;
        }

        public delegate string PublishingXmlHandler(string publishingXmlContent);

        /// <summary>
        /// This is an entry for custom test, by comparing two official updates
        /// </summary>
        /// <param name="updateID1">ID of update 1</param>
        /// <param name="xmlHandler1">The delegate method to change publishing xml for update 1</param>
        /// <param name="updateID2">ID of update 2</param>
        /// <param name="xmlHandler2">The delegate method to change publishing xml for update 2</param>
        /// <param name="caseToExecute">ID of cases to be executed. If null, all active case will be executed</param>
        /// <returns>Test results</returns>
        public static List<TestResult> Compare2Updates(string updateID1, PublishingXmlHandler xmlHandler1, string updateID2, PublishingXmlHandler xmlHandler2, List<int> caseToExecute = null)
        {
            // Update 1
            string xmlPath1 = MUACWorker.GetPubSuiteXMLFile(updateID1);
            string xmlContent1 = String.Empty;
            using (StreamReader sr = new StreamReader(xmlPath1))
            {
                xmlContent1 = sr.ReadToEnd();
            }

            if (xmlHandler1 != null)
            {
                xmlContent1 = xmlHandler1(xmlContent1);
            }

            Update update1 = UpdateBuilder.BuildUpdateFromPublishingXml(xmlContent1);

            // Update 2
            string xmlPath2 = MUACWorker.GetPubSuiteXMLFile(updateID2);
            string xmlContent2 = String.Empty;
            using (StreamReader sr = new StreamReader(xmlPath2))
            {
                xmlContent2 = sr.ReadToEnd();
            }

            if (xmlHandler2 != null)
            {
                xmlContent2 = xmlHandler2(xmlContent2);
            }

            Update update2 = UpdateBuilder.BuildUpdateFromPublishingXml(xmlContent2);

            // cases
            if (caseToExecute == null || caseToExecute.Count == 0)
            {
                caseToExecute = QuerySupportedCases().Select(p => p.ID).ToList();
            }

            List<TestResult> results = new List<TestResult>();
            foreach (var id in caseToExecute)
            {
                results.Add(TestcaseFactory.ExecuteTestcase(id, null, update1, update2));
            }

            return results;
        }

        /// <summary>
        /// Get update destionation
        /// </summary>
        /// <param name="updateID">update ID</param>
        /// <returns></returns>
        public static string GetUpdateDestination(string updateID)
        {
            Update update = UpdateBuilder.QueryUpdateFromGUID(updateID);

            string dest = String.Empty;

            if (update.Properties.Site)
                dest += "Site";
            if (update.Properties.AU)
                dest += "AU";
            if (update.Properties.SUS)
                dest += "SUS";
            if (update.Properties.Catalog)
                dest += "Catalog";
            if (update.Properties.Csa)
                dest += "Csa";

            return dest;
        }

        /// <summary>
        /// Get all cases that are active
        /// </summary>
        /// <returns>A list of case info</returns>
        public static List<TTestCaseInfo> QuerySupportedCases()
        {
            return TestcaseFactory.GetAllCases();
        }


        /// <summary>
        /// Get update object by ID
        /// </summary>
        /// <param name="updateId">ID of update</param>
        /// <returns>Update object</returns>
        public static Update GetUpdateByID(string updateId)
        {
            return UpdateBuilder.QueryUpdateFromGUID(updateId);
        }

        /// <summary>
        /// Save Data in DB
        /// </summary>
        /// <param name="caseName"></param>
        /// <param name="updateID"></param>
        /// <param name="result"></param>
        private static void SaveTestInfoToSelfDB(string caseName, InputData inputdata, int updateID, bool result)
        {
            TPubsuiteStaticTest pubsuiteStaticTest = new TPubsuiteStaticTest();
            using (var dbContext = new PatchTestDataClassDataContext())
            {
                pubsuiteStaticTest.Title = inputdata.Title;
                pubsuiteStaticTest.CaseID = dbContext.TPubsuiteTestCaseInfo.Where(item => item.Name.Contains(caseName)).Select(item => item.ID).Single();
                pubsuiteStaticTest.UpdateGUID = inputdata.UpdateID;
                if (result == true)
                {
                    pubsuiteStaticTest.Result = "Pass";
                }
                else
                    pubsuiteStaticTest.Result = "Fail";
                pubsuiteStaticTest.UpdateID = updateID;
                dbContext.TPubsuiteStaticTest.InsertOnSubmit(pubsuiteStaticTest);
                dbContext.SubmitChanges();
            }
        }

        private static void SaveResultDetailToSelfDB(string resultTitle, string expectResult, string actualResult, string caseName, InputData inputdata)
        {
            TPubsuiteStaticResultDetail pubsuiteStaticResultDetail = new TPubsuiteStaticResultDetail();
            using (var dbContext = new PatchTestDataClassDataContext())
            {
                pubsuiteStaticResultDetail.ResultTitle = resultTitle;
                pubsuiteStaticResultDetail.ExpectResult = expectResult;
                pubsuiteStaticResultDetail.ActualResult = actualResult;
                pubsuiteStaticResultDetail.CaseName = caseName;

                var CaseID = dbContext.TPubsuiteTestCaseInfo.Where(item => item.Name.Contains(caseName)).Select(item => item.ID).Single();
                //var TestID = dbContext.TPubsuiteStaticTest.Where(item => item.CaseID.Equals(CaseID) && item.UpdateGUID.Equals(inputdata.UpdateID)).Select(item => item.ID).FirstOrDefault();
                var TestID = dbContext.TPubsuiteStaticTest.Where(item => item.UpdateGUID.Equals(inputdata.UpdateID) && item.CaseID.Equals(CaseID)).OrderByDescending(item => item.ID).FirstOrDefault().ID;
                pubsuiteStaticResultDetail.TestID = TestID;
                dbContext.TPubsuiteStaticResultDetail.InsertOnSubmit(pubsuiteStaticResultDetail);
                dbContext.SubmitChanges();
            }
        }
    }
}
