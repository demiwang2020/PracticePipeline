using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PubsuiteProductRefreshStaticTestLib.Testcases
{
    public class TestResult
    {
        public string CaseName { get; set; }
        public bool Result { get; set; }
        public string Log
        {
            get { return _logBuf == null ? String.Empty : _logBuf.ToString(0, _logBuf.Length - 1); }
        }
        public Dictionary<string, TestFailure> Failures { get; private set; }

        private StringBuilder _logBuf;

        public void LogMessage(string message)
        {
            if(_logBuf == null)
                _logBuf = new StringBuilder();

            //_logBuf.AppendFormat("[{0}] {1}\r\n", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), message);
            _logBuf.AppendLine(message);
        }

        public void LogError(string message)
        {
            LogMessage(String.Format("!!Error: {0}", message));
        }

        public void LogWarning(string message)
        {
            LogMessage(String.Format("Warning: {0}", message));
        }

        public void AddFailure(string name, TestFailure failure)
        {
            if (Failures == null)
                Failures = new Dictionary<string, TestFailure>();

            Failures.Add(name, failure);
        }
    }

    public class TestFailure
    {
        public string ExpectResult;
        public string ActualResult;
    }
}
