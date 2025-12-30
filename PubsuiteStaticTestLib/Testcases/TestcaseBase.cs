using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PubsuiteStaticTestLib.UpdateHelper;

namespace PubsuiteStaticTestLib.Testcases
{   
    class TestcaseBase
    {
        protected Update _expectedUpdate;
        protected Update _actualUpdate;
        protected InputData _inputData;
        protected TestResult _result;
        private string _caseName;

        public TestcaseBase(InputData inputData, Update expectUpdate, Update actualUpdate, string name)
        {
            _inputData = inputData;
            _expectedUpdate = expectUpdate;
            _actualUpdate = actualUpdate;
            _caseName = name;

            _result = new TestResult();
            _result.Result = true;
        }

        /// <summary>
        /// Entry for each case
        /// </summary>
        /// <returns></returns>
        public TestResult Run()
        {
            _result.CaseName = _caseName;

            try
            {
                RunTest();
            }
            catch (Exception ex)
            {
                _result.LogError("Exception caught: " + ex.Message);
                _result.LogMessage(ex.StackTrace);

                _result.Result = false;
            }

            return _result;
        }

        protected virtual void RunTest()
        {
            throw new NotSupportedException("This methoed is not supported in base case class");
        }

        protected void GenerateFailResult(string errorMsg, string failureName, string expectResult, string actualResult)
        {
            _result.LogError(errorMsg);
            _result.Result = false;
            _result.AddFailure(failureName,
                new TestFailure()
                {
                    ExpectResult = expectResult,
                    ActualResult = actualResult
                });
        }
    }
}
