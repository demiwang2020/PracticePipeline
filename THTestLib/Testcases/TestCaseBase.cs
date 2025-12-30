using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace THTestLib.Testcases
{
    class TestCaseBase
    {
        protected THTestObject TestObject;
      

        public TestCaseBase(THTestObject testobj)
        {
            TestObject = testobj;
         
        }

        public virtual bool RunTestCase()
        {
            return false;
        }

        public static List<TestCaseBase> GetAllTestCases(THTestObject testObj)
        {
            List<TestCaseBase> lstAllCases = new List<TestCaseBase>();

            lstAllCases.Add(new TestCaseVerifyBinariesVersions(testObj));
            lstAllCases.Add(new TestCaseVerifyUnexpectedBinaries(testObj));
            lstAllCases.Add(new TestCaseVerifyCumulativePayloads(testObj));
            lstAllCases.Add(new TestcaseVerifyNewBinariesInCumulativePatch(testObj));
            lstAllCases.Add(new TestcaseVerifyLCUPayloadsInOtherSKU(testObj));
            lstAllCases.Add(new TestCaseVerifyNoUnexpectPayloadInSimplePatch(testObj));
            lstAllCases.Add(new TestCaseVerifyContentArch(testObj));
            lstAllCases.Add(new TestCase3264BinariesSame(testObj));
            lstAllCases.Add(new TestCaseVerifyBinariesDestination(testObj));
            lstAllCases.Add(new TestCaseRealSigned(testObj));
            lstAllCases.Add(new TestCaseVerifyCertCount(testObj));
            lstAllCases.Add(new TestCaseVerifyConflict4XFilesNotInclude(testObj));
            lstAllCases.Add(new TestCaseVerifyAssemblyIdentity(testObj));
            lstAllCases.Add(new TestCaseVerifyARP(testObj));
            lstAllCases.Add(new TestCaseVerifyTargetProducts(testObj));
            lstAllCases.Add(new TestCaseVerifyNet35Parents(testObj));
            lstAllCases.Add(new TestCaseVerifySpecificManifests(testObj));
            //lstAllCases.Add(new TestCaseVerifyRemoteExpandFolders(testObj));
            lstAllCases.Add(new TestCaseVerifyESUManifestExists(testObj));
            lstAllCases.Add(new TestCaseVerifySuffixForAbove48Packages(testObj));
            lstAllCases.Add(new TestCaseVerifyComponentVer(testObj));

            return lstAllCases;
        }
    }
}
