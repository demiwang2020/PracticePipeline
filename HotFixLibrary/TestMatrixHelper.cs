using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Xml;
using System.Data;
using System.Collections;

using ScorpionDAL;
using LoggerLibrary;

using Facet.Combinatorics;

namespace HotFixLibrary
{
    public class TestMatrixHelper
    {
        //public static bool CreateCopyTestMatrix(string strNewTestMatrixName, string strExistingTestMatrixName, string strCreatedBy)
        //{
        //    PatchTestDataClassDataContext db = new PatchTestDataClassDataContext();
        //    var vTestMatrix = from t in db.TTestMatrixes
        //                      where t.TestMatrixName == strExistingTestMatrixName
        //                      select t;

        //    List<TTestMatrix> lstTestMatrix = new List<TTestMatrix>();
        //    foreach (TTestMatrix objTestMatrixExisting in vTestMatrix)
        //    {
        //        TTestMatrix objTestMatrixNew = objTestMatrixExisting.Clone();
        //        objTestMatrixNew.TestMatrixName = strNewTestMatrixName;
        //        objTestMatrixNew.CreatedBy = strCreatedBy;
        //        objTestMatrixNew.CreatedDate = DateTime.Now;
        //        objTestMatrixNew.LastModifiedBy = null;
        //        objTestMatrixNew.LastModifiedDate = null;

        //        lstTestMatrix.Add(objTestMatrixNew);
        //    }

        //    db.TTestMatrixes.InsertAllOnSubmit(lstTestMatrix);

        //    return true;
        //}
        public static List<PossibleOS> GetPossibleOSDetails(int MaddogDBID)
        {
            PatchTestDataClassDataContext db = new PatchTestDataClassDataContext();
            
            var varPossibleOS = from os in db.TOs
                                join osi in db.TOSImages on os.MDOSID equals osi.MDOSID
                                where osi.MaddogDBID == MaddogDBID
                                orderby os.OSVersion descending, os.OSName, os.OSCPUID, os.OSLanguageID 
                                //orderby os.OSVersion, os.OSCPUID, os.OSLanguageID, os.MDOSID, osi.OSSPLevel
                                select new { os.MDOSID, osi.OSImageID, MDOSID_OSImageID = os.MDOSID + "-" + osi.OSImageID, PossibleOSDetails = (os.OSDetails + " " + osi.OSSPLevel).ToString() };


            List<PossibleOS> lstPossibleOS = new List<PossibleOS>();
            foreach (var item in varPossibleOS)
            {
                PossibleOS objPossibleOS = new PossibleOS(item.MDOSID, item.OSImageID, item.MDOSID_OSImageID, item.PossibleOSDetails);
                lstPossibleOS.Add(objPossibleOS);
            }
            return lstPossibleOS;
        }

        public static List<DistinctPossibleOS> GetDistinctOSDetails(int MaddogDBID)
        {
            PatchTestDataClassDataContext db = new PatchTestDataClassDataContext();

            var varPossibleOS1 = (from os in db.TOs
                                join osi in db.TOSImages on os.MDOSID equals osi.MDOSID
                                where osi.MaddogDBID == MaddogDBID
                                && osi.Active == true && os.Active == true
                                orderby os.OSVersion descending, os.OSName, os.OSCPUID, os.OSLanguageID
                                //orderby os.OSVersion, os.OSCPUID, os.OSLanguageID, os.MDOSID, osi.OSSPLevel
                                select new { os.MDOSID , os.OSDetails, os.OSVersion, os.OSName, os.OSCPUID, os.OSLanguageID}).Distinct();


            var varPossibleOS = from os in varPossibleOS1
                                orderby os.OSVersion descending, os.OSName, os.OSCPUID, os.OSLanguageID
                                select   new { os.MDOSID , os.OSDetails};

            List<DistinctPossibleOS> lstPossibleOS = new List<DistinctPossibleOS>();
            foreach (var item in varPossibleOS)
            {
                DistinctPossibleOS objPossibleOS = new DistinctPossibleOS(item.MDOSID, item.OSDetails);
                lstPossibleOS.Add(objPossibleOS);
            }
            return lstPossibleOS;
        }

        public static Hashtable GetTestCaseSpecificDataFormat(HotFixUtility.ApplicationType appType)
        {
            PatchTestDataClassDataContext db = new PatchTestDataClassDataContext();
            Hashtable hstTestCase = new Hashtable();

            var varTestCase = from tc in db.TTestCaseWithPriorities
                              where tc.PatchType == appType.ToString()
                              select new { tc.TestCaseID, tc.TestCaseSpecificDataFormat };


            foreach (var item in varTestCase)
            {
                hstTestCase.Add(item.TestCaseID.ToString(), (item.TestCaseSpecificDataFormat == null? "": item.TestCaseSpecificDataFormat));
            }
            return hstTestCase;
        }

        public static string CreateTestMatrix(string strTestMatrixName, List<TestMatrixParameter> lstOS, int intProductID, string strProductSPLevel,
            int intProductCPUID, List<TestMatrixParameter> lstProductLanguage, List<TestMatrixParameter> lstProductSKUs,
            List<TestMatrixParameter> lstTestCases, int intMaxScenarios, bool blnRandom, string strOwner, string strApplicationType, int MaddogDBID)
        {
            string strResult = "success";
            PatchTestDataClassDataContext db = new PatchTestDataClassDataContext();

            var varTestMatrixCheck = from TestMatrix in db.TTestMatrixes
                                     where TestMatrix.TestMatrixName.ToLower().Equals(strTestMatrixName.ToLower())
                                     select TestMatrix;

            if (varTestMatrixCheck.Count() != 0)
            {
                strResult = "Test Matrix '" + strTestMatrixName + "' already exists. Please select another name.";
                return strResult;
            }

            //char[] inputSet = { 'A', 'B', 'C', 'D' };
            //string str1;
            //Combinations<char> combinations = new Combinations<char>(inputSet, 4);
            //foreach (IList<char> c in combinations)
            //{
            //    str1 = c[0].ToString();
            //}

            //char[] inputSet = { 'A', 'B', 'C' };
            //string str = "";
            //Permutations<char> permutations = new Permutations<char>(inputSet);
            //foreach (IList<char> p in permutations)
            //{
            //    str = p[0].ToString();
            //}


            List<TestMatrixParameter> lstAllTestMatrixParameter = new List<TestMatrixParameter>();
            lstAllTestMatrixParameter.AddRange(lstOS);
            lstAllTestMatrixParameter.AddRange(lstProductLanguage);
            lstAllTestMatrixParameter.AddRange(lstProductSKUs);
            lstAllTestMatrixParameter.AddRange(lstTestCases);

            //string strParameterDetails = "";
            //Permutations<TestMatrixParameter> perm = new Permutations<TestMatrixParameter>(lst);
            Combinations<TestMatrixParameter> combAllPossibleCombination = new Combinations<TestMatrixParameter>(lstAllTestMatrixParameter, 4);
            //Variations<TestMatrixParameter> vari = new Variations<TestMatrixParameter>(lst, 4);
            //foreach (IList<TestMatrixParameter> p in perm)
            //{
            //    strParameterDetails = p[0].ParameterDeails;
            //}

            //foreach (IList<TestMatrixParameter> c in comb)
            //{
            //    strParameterDetails = c[0].ParameterDeails;
            //}
            //foreach (IList<TestMatrixParameter> v in vari)
            //{
            //    strParameterDetails = v[0].ParameterDeails;
            //}


            var varValidCombinations = from tmp in combAllPossibleCombination
                           where tmp[0].ParameterName != tmp[1].ParameterName && tmp[0].ParameterName != tmp[2].ParameterName &&
                                 tmp[0].ParameterName != tmp[3].ParameterName && tmp[1].ParameterName != tmp[2].ParameterName &&
                                 tmp[1].ParameterName != tmp[3].ParameterName && tmp[2].ParameterName != tmp[3].ParameterName
                           select tmp;

            List<List<TestMatrixParameter>> lstValidCombination = new List<List<TestMatrixParameter>>();
            foreach (IList<TestMatrixParameter> c in varValidCombinations)
            {
                lstValidCombination.Add(new List<TestMatrixParameter>(c));
            }
            
            CleanUp(lstValidCombination, lstOS, TestMatrixParameterName.MDOSID);
            CleanUp(lstValidCombination, lstProductLanguage, TestMatrixParameterName.ProductLanguage);
            CleanUp(lstValidCombination, lstProductSKUs, TestMatrixParameterName.ProductSKU);
            CleanUp(lstValidCombination, lstTestCases, TestMatrixParameterName.TestCase);


            List<TTestMatrix> lstTestMatrix = new List<TTestMatrix>();
            for (int i = 0; i < intMaxScenarios; i++)
            {
                if (lstValidCombination.Count == 0 || lstOS.Count == 0 || lstProductLanguage.Count == 0 || lstProductSKUs.Count == 0 || lstTestCases.Count == 0)
                    break;

                TTestMatrix objTestMatrix = GetATestScenario(lstValidCombination, lstOS, lstProductLanguage, lstProductSKUs, lstTestCases, blnRandom);

                objTestMatrix.ProductCPUID = Convert.ToInt16(intProductCPUID);
                objTestMatrix.ProductID = Convert.ToInt16(intProductID);
                objTestMatrix.ProductSPLevel = strProductSPLevel;

                objTestMatrix.TestCaseSpecificData = "";
                objTestMatrix.TestMatrixPriority = 1;

                objTestMatrix.KBNumber = strTestMatrixName;
                objTestMatrix.TestMatrixName = strTestMatrixName;

                objTestMatrix.Active = true;

                objTestMatrix.CreatedBy = strOwner;
                objTestMatrix.CreatedDate = DateTime.Now;

                objTestMatrix.LastModifiedBy = null;
                objTestMatrix.LastModifiedDate = null;

                objTestMatrix.ApplicationType = strApplicationType;
                objTestMatrix.MaddogDBID = MaddogDBID;

                lstTestMatrix.Add(objTestMatrix);
            }

            db.TTestMatrixes.InsertAllOnSubmit(lstTestMatrix);
            db.SubmitChanges();

            return strResult;
        }

        private static TTestMatrix GetATestScenario(List<List<TestMatrixParameter>> lstValidCombination, List<TestMatrixParameter> lstOS, List<TestMatrixParameter> lstProductLanguage,
            List<TestMatrixParameter> lstProductSKUs, List<TestMatrixParameter> lstTestCases, bool blnRandom)
        {
            Random rdRandom = new Random(100);
            if (blnRandom)
                rdRandom = new Random(DateTime.Now.Millisecond);

            int intRandomNumber = rdRandom.Next() % lstValidCombination.Count;
            List<TestMatrixParameter> lstTestMatrixParameter = lstValidCombination[intRandomNumber];

            TestMatrixParameter objOSTestMatrixParameter = lstTestMatrixParameter.Single(tmp => tmp.ParameterName == TestMatrixParameterName.MDOSID);
            TestMatrixParameter objProductLanguageTestMatrixParameter = lstTestMatrixParameter.Single(tmp => tmp.ParameterName == TestMatrixParameterName.ProductLanguage);
            TestMatrixParameter objProductSKUTestMatrixParameter = lstTestMatrixParameter.Single(tmp => tmp.ParameterName == TestMatrixParameterName.ProductSKU);
            TestMatrixParameter objTestCaseTestMatrixParameter = lstTestMatrixParameter.Single(tmp => tmp.ParameterName == TestMatrixParameterName.TestCase);

            int intMDOSID = Convert.ToInt32(objOSTestMatrixParameter.ParameterID.Split("-".ToCharArray())[0]);
            int intOSImageID = Convert.ToInt32(objOSTestMatrixParameter.ParameterID.Split("-".ToCharArray())[1]);
            short intProductLanguageID = Convert.ToInt16(objProductLanguageTestMatrixParameter.ParameterID);
            short intProductSKUID = Convert.ToInt16(objProductSKUTestMatrixParameter.ParameterID);
            int intTestCaseID = Convert.ToInt32(objTestCaseTestMatrixParameter.ParameterID);

            bool blnIsTestMatrixParameterEmpty = false;
            blnIsTestMatrixParameterEmpty = UpdateListTestMatrixParameter(lstOS, objOSTestMatrixParameter.ParameterID);
            if (blnIsTestMatrixParameterEmpty == true)
            {
                CleanUp(lstValidCombination, lstOS, TestMatrixParameterName.MDOSID);
            }

            blnIsTestMatrixParameterEmpty = UpdateListTestMatrixParameter(lstProductLanguage, objProductLanguageTestMatrixParameter.ParameterID);
            if (blnIsTestMatrixParameterEmpty == true)
            {
                CleanUp(lstValidCombination, lstProductLanguage, TestMatrixParameterName.ProductLanguage);
            }

            blnIsTestMatrixParameterEmpty = UpdateListTestMatrixParameter(lstProductSKUs, objProductSKUTestMatrixParameter.ParameterID);
            if (blnIsTestMatrixParameterEmpty == true)
            {
                CleanUp(lstValidCombination, lstProductSKUs, TestMatrixParameterName.ProductSKU);
            }

            blnIsTestMatrixParameterEmpty = UpdateListTestMatrixParameter(lstTestCases, objTestCaseTestMatrixParameter.ParameterID);
            if (blnIsTestMatrixParameterEmpty == true)
            {
                CleanUp(lstValidCombination, lstTestCases, TestMatrixParameterName.TestCase);
            }
                                                         
            TTestMatrix objTestMatrix = new TTestMatrix();
            objTestMatrix.MDOSID = intMDOSID;
            objTestMatrix.OSImageID = intOSImageID;
            objTestMatrix.ProductLanguageID = intProductLanguageID;
            objTestMatrix.ProductSKUID = intProductSKUID;
            objTestMatrix.TestCaseID = intTestCaseID;

            lstValidCombination.Remove(lstTestMatrixParameter);

            return objTestMatrix;
        }

        private static void CleanUp(List<List<TestMatrixParameter>> lstValidCombination, List<TestMatrixParameter> lstTestMatrixParameter, TestMatrixParameterName pmParameterName)
        {
            var varInActiveParameter = from param in lstTestMatrixParameter
                                       where param.ParameterIsCompleted == true
                                       select param;

            for(int i = 0; i < varInActiveParameter.Count(); i++)
            {
                TestMatrixParameter objInActiveTestMatrixParameter = varInActiveParameter.ToArray()[i];

                var varInActiveCombinations = from comb in lstValidCombination
                                              where ((comb[0].ParameterName == pmParameterName && comb[0].ParameterID.Equals(objInActiveTestMatrixParameter.ParameterID))
                                              || (comb[1].ParameterName == pmParameterName && comb[1].ParameterID.Equals(objInActiveTestMatrixParameter.ParameterID))
                                              || (comb[2].ParameterName == pmParameterName && comb[2].ParameterID.Equals(objInActiveTestMatrixParameter.ParameterID))
                                              || (comb[3].ParameterName == pmParameterName && comb[3].ParameterID.Equals(objInActiveTestMatrixParameter.ParameterID)))
                                              select comb;

                for(int j = 0; j < varInActiveCombinations.Count(); j++)
                {
                    List<TestMatrixParameter> lstInActiveTestMatrixParameter = varInActiveCombinations.ToArray()[j];
                    lstValidCombination.Remove(lstInActiveTestMatrixParameter);
                    j--;
                }
                //foreach (List<TestMatrixParameter> lstInActiveTestMatrixParameter in varInActiveCombinations)
                    
                lstTestMatrixParameter.Remove (objInActiveTestMatrixParameter);
                i--;
            }
        }

        private static bool UpdateListTestMatrixParameter(List<TestMatrixParameter> lstTestMatrixParameter, string strParameterID)
        {
            TestMatrixParameter objTestMatrixParameter = lstTestMatrixParameter.Single(param => param.ParameterID.Equals(strParameterID));
            objTestMatrixParameter.ParameterRemainCount = objTestMatrixParameter.ParameterRemainCount - 1;
            if (objTestMatrixParameter.ParameterRemainCount <= 0)
            {
                objTestMatrixParameter.ParameterIsCompleted = true;
                return true;
            }
            return false;
        }

        private static string GetRandomParameterID(List<TestMatrixParameter> lstTestMatrixParameter, bool blnRandom, TestMatrixParameterName pmParameterName)
        {
            Random rdRandom = new Random(100);
            if (blnRandom)
                rdRandom = new Random( DateTime.Now.Millisecond);

            var varActiveParameter = from param in lstTestMatrixParameter
                                     where param.ParameterIsCompleted == false
                                     select param;

            if (varActiveParameter.Count() == 0)
            {
                throw new Exception("Error occured: No active " + pmParameterName.ToString() + " found. Please contact dev support");
            }

            int intRandomNumber = rdRandom.Next() % varActiveParameter.Count();

            TestMatrixParameter objTestMatrixParameter = varActiveParameter.ToArray()[intRandomNumber];
            objTestMatrixParameter.ParameterRemainCount = objTestMatrixParameter.ParameterRemainCount - 1;
            if (objTestMatrixParameter.ParameterRemainCount <= 0)
                objTestMatrixParameter.ParameterIsCompleted = true;

            return objTestMatrixParameter.ParameterID;
        }
    }

    public class PossibleOS
    {
        public int MDOSID { get; set; }
        public int OSImageID { get; set; }

        public string MDOSID_OSImageID { get; set; }
        public string PossibleOSDetails { get; set; }
        public PossibleOS(int intMDOSID, int intOSImage, string strMDOSID_OSImageID, string strPossibleOSDetails)
        {
            MDOSID = intMDOSID;
            OSImageID = intOSImage;
            MDOSID_OSImageID = strMDOSID_OSImageID;
            PossibleOSDetails = strPossibleOSDetails;
        }

    
    }

    public class DistinctPossibleOS
    {
        public int MDOSID { get; set; }
        public string OSDetails { get; set; }

        public DistinctPossibleOS(int intMDOSID, string strOSDetails)
        {
            MDOSID = intMDOSID;
            OSDetails = strOSDetails;


        }


    }

    public class TestMatrixParameter : IComparable<TestMatrixParameter>
    {
        public string ParameterID { get; set; }
        public string ParameterDeails { get; set; }
        public object ParameterObject { get; set; }
        public double ParameterPercent { get; set; }
        public int ParameterTotalCount { get; set; }
        public int ParameterRemainCount { get; set; }
        public bool ParameterIsCompleted { get; set; }
        public double ParameterDecimalPart { get; set; }
        public TestMatrixParameterName ParameterName { get; set; }

        //#region IComparable Members

        //int IComparable.CompareTo(object obj)
        //{
        //    return 0;
        //    //throw new NotImplementedException();
        //}

        //#endregion



        #region IComparable<TestMatrixParameter> Members

        int IComparable<TestMatrixParameter>.CompareTo(TestMatrixParameter other)
        {
            if (ParameterName == other.ParameterName)
                return 0;
             else if (ParameterName > other.ParameterName)
                return 1;
            else
                return -1;
        }

        #endregion
    }

    public enum TestMatrixParameterName { MDOSID = 1, ProductLanguage = 2, ProductSKU = 3, TestCase = 4 };

    #region Create Rule Engine
    /* ToDo: Create a Rule Engine
     * Driven by DB or config or XML file
     * Some Rule:
     *  1. Non ENU OS should have either ENU Product or OS_Lang Product
     *  
     */
    #endregion Create Rule Engine

}
