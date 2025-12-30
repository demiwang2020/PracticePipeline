using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LoggerLibrary
{
    public interface ILogger
    {
        void ProcessLogMessage(string strLogMessage, LogHelper.LogLevel enumLogLevel, ScorpionDAL.TTestProdInfo objTPatch, string strErrorMessage);
    }
}
