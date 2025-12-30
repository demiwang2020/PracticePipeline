using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogAnalyzer
{
    public enum LogType
    {
        NetfxSetup,
        WUTest
    };

    public class TestLogAnalyzer
    {
        private Dictionary<string, object> _dictExtraData;

        public virtual Result Analyze(int runID)
        {
            return null;
        }

        public void PutExtraData(string dataName, object data)
        {
            if(_dictExtraData == null)
                _dictExtraData = new Dictionary<string,object>();

            _dictExtraData[dataName] = data;
        }

        public object GetExtraData(string dataName)
        {
            if (_dictExtraData != null && _dictExtraData.ContainsKey(dataName))
                return _dictExtraData[dataName];
            else
                return null;
        }

        public static TestLogAnalyzer CreateLogAnalyzer(LogType type)
        {
            switch (type)
            {
                case LogType.NetfxSetup:
                    return new NetfxsetupLogAnalyzer();

                case LogType.WUTest:
                    return new WUTestLogAnalyzer();

                default:
                    throw new Exception(String.Format("Analyzer of {0} is not supported", type));
            }
        }
    }
}
