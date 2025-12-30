using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ScorpionDAL;
namespace LoggerLibrary
{
    class DBLogger: ILogger
    {
        #region ILogger Members

        public void ProcessLogMessage(string logMessage, LogHelper.LogLevel enumLogLevel, TTestProdInfo objTPatch, string strErrorMessage)
        {
            if (objTPatch != null)
            {
                PatchTestDataClassDataContext db = new PatchTestDataClassDataContext();
                TLog objTLog = new TLog();

                objTLog.LogMessage = (logMessage.Length < 500 ? logMessage : logMessage.Substring(0, 500));
                objTLog.LogLevel = enumLogLevel.ToString();
                objTLog.ErrorMessage = (strErrorMessage.Length < 5000 ? strErrorMessage : strErrorMessage.Substring(0, 5000));

                string strDateTime = logMessage.Substring(0, logMessage.IndexOf('-'));
                objTLog.LogDateTime = Convert.ToDateTime(strDateTime);

                objTLog.PatchID = objTPatch.TTestProdInfoID;
                objTLog.PatchKBNumber = objTPatch.TestIdentifier;
                objTLog.PatchBuildNumber = objTPatch.BuildNumber;
                objTLog.PatchCreationDate = (objTPatch.CreatedDate == DateTime.MinValue ? new DateTime?() : objTPatch.CreatedDate);
                objTLog.PatchCreatedBy = objTPatch.CreatedBy;

                db.TLogs.InsertOnSubmit(objTLog);
                db.SubmitChanges();
            }
            else
            {
                PatchTestDataClassDataContext db = new PatchTestDataClassDataContext();
                TLog objTLog = new TLog();

                objTLog.LogMessage = (logMessage.Length < 500 ? logMessage : logMessage.Substring(0, 500));
                objTLog.LogLevel = enumLogLevel.ToString();
                objTLog.ErrorMessage = (strErrorMessage.Length < 5000 ? strErrorMessage : strErrorMessage.Substring(0, 5000));

                string strDateTime = logMessage.Substring(0, logMessage.IndexOf('-'));
                objTLog.LogDateTime = Convert.ToDateTime(strDateTime);

                objTLog.PatchID = -1;
                objTLog.PatchKBNumber = "KB";
                objTLog.PatchBuildNumber = "Build";
                objTLog.PatchCreationDate = DateTime.Now;
                objTLog.PatchCreatedBy = "user";

                db.TLogs.InsertOnSubmit(objTLog);
                db.SubmitChanges();
            }
        }

        #endregion
    }
}
