using PubsuiteProductRefreshStaticTestLib.DbClassContext;
using PubsuiteProductRefreshStaticTestLib.Model;
using PubsuiteProductRefreshStaticTestLib.UpdateHelper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace PubsuiteProductRefreshStaticTestLib.Testcases
{
    class TestcaseFactory
    {
        public static TestResult ExecuteTestcase(int caseID, InputData inputData, Update expectUpdate, Update actualUpdate)
        { 
            using (var db = new WUSAFXDbContext())
            {
                var caseInfo = db.TTestcaseInfos.Where(p => p.ID == caseID).Single();

                //Create case
                Assembly assembly = Assembly.GetCallingAssembly();
                Type type = assembly.GetType(caseInfo.ProductRefreshType);
                
                Type[] constructParas = new Type [] { typeof(InputData), typeof(Update), typeof(Update) };
                ConstructorInfo info = type.GetConstructor(constructParas);

                object[] paramObjs = new object[] { inputData, expectUpdate, actualUpdate };
                object caseObj = info.Invoke(paramObjs);

                //Run test
                MethodInfo runTest = type.GetMethod("Run");
                return (TestResult)runTest.Invoke(caseObj, null);
            }
        }

        public static List<TTestCaseInfo> GetAllCases()
        {
            using (var db = new WUSAFXDbContext())
            {
                return db.TTestcaseInfos.Where(p => p.Active).ToList();
                //return db.TTestcaseInfos.ToList();
            }
        }
    }
}
