using NETCoreMUStaticLib.DbClassContext;
using NETCoreMUStaticLib.Model;
using NETCoreMUStaticLib.UpdateHelper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace NETCoreMUStaticLib.Testcases
{
    class TestcaseFactory
    {
        public static TestResult ExecuteTestcase(int caseID, InnerData data, Update expectUpdate, Update actualUpdate)
        { 
            using (var db = new NetCoreWUSAFXDbContext())
            {
                var caseInfo = db.TTestcaseInfos.Where(p => p.ID == caseID).Single();

                //Create case
                Assembly assembly = Assembly.GetCallingAssembly();
                Type type = assembly.GetType(caseInfo.Type);
                
                Type[] constructParas = new Type [] { typeof(InnerData), typeof(Update), typeof(Update) };
                ConstructorInfo info = type.GetConstructor(constructParas);

                object[] paramObjs = new object[] { data, expectUpdate, actualUpdate };
                object caseObj = info.Invoke(paramObjs);

                //Run test
                MethodInfo runTest = type.GetMethod("Run");
                return (TestResult)runTest.Invoke(caseObj, null);
            }
        }

        public static List<TTestCaseInfo> GetAllCases()
        {
            using (var db = new NetCoreWUSAFXDbContext())
            {
                return db.TTestcaseInfos.Where(p => p.Active).ToList();
            }
        }
    }
}
