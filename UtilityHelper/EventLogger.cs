using System;
using System.Collections.Generic;
using System.IO;

namespace Helper
{
    public class EventLogger
    {
        private static  EventLogger Loginstance=null;
        private static object syncRoot = new Object();
        private static Queue<Log> logQueue;
        private static string logDir = GetTempDir();
        private static string logFile = LogConstants.LOGFILEPREFIX;
        private static FileStream LogFileStream;
        private static DateTime LastFlushed = DateTime.Now;

        /// <Description>
        /// Private constructor to prevent instance creation
        /// </Description>
        private EventLogger()
        {
            if (!LogConstants.LOGDIR.ToString().Equals("%temp%"))
                logDir = LogConstants.LOGDIR.ToString();
            logFile = string.Format("{0}{1}{2}{3}_{4}{5}{6}.log", logFile.ToString(), DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second);
            string logPath = Path.Combine(logDir,logFile);
            LogFileStream = File.Open(logPath, FileMode.Append, FileAccess.Write);
        }

        public void Reopen()
        {
            if (LogFileStream != null)
            {
                LogFileStream.Close();
            }
            string logPath = Path.Combine(logDir, logFile);
            LogFileStream = File.Open(logPath, FileMode.Append, FileAccess.Write);
        }

        private static string GetTempDir()
        {
            return (Path.GetTempPath());
        }
        /// <Description>
        /// An EventLogger instance that exposes a single instance
        /// </Description>
        public static EventLogger Instance(string LogDir ="ScorpionLog.log")
        {
              // If the instance is null then create one and init the Queue
                if (Loginstance == null)
                {
                    lock (syncRoot)
                    {
                        if (Loginstance == null)
                        {
                            Loginstance = new EventLogger();
                            logQueue = new Queue<Log>();
                        }
                    }
                }
                return Loginstance;
          
        }
        public void WriteErrorToLog(string message)
        {
            WriteToLog(message, LogConstants.MESSAGETYPE.ERROR);
        }
        public void WriteInfoToLog(string message)
        {
            WriteToLog(message, LogConstants.MESSAGETYPE.INFO);
        }
        public void WriteExceptionToLog(string message)
        {
            WriteToLog(message, LogConstants.MESSAGETYPE.EXCEPTION);
        }
        public void WriteWarningToLog(string message)
        {
            WriteToLog(message, LogConstants.MESSAGETYPE.WARNING);
        }

        /// <Description>
        /// The single instance method that writes to the log file
        /// </Description>
        /// <param name="message">The message to write to the log,<name="type">The message type to write to the log</param>
        public void WriteToLog(string message, LogConstants.MESSAGETYPE type = LogConstants.MESSAGETYPE.INFO)
        {
            // Lock queue while writing to prevent contention for the log file
            lock (logQueue)
            {
                // Create an entry and push to Queue
                Log logEntry = new Log(message, type);
                logQueue.Enqueue(logEntry);

                // If we have reached the Queue Size then flush
                if (logQueue.Count >= LogConstants.LOGQUEUESIZE || DoPeriodicFlush())
                {
                    FlushLog();
                    LastFlushed = DateTime.Now;
                }
            }
        }

        private bool DoPeriodicFlush()
        {
            TimeSpan logAge = DateTime.Now - LastFlushed;
            if (logAge.TotalSeconds >= LogConstants.LOGQUEUEAGE)
            {
                LastFlushed = DateTime.Now;
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <Description>
        /// Flushes Queue to the physical log file
        /// </Description>
        private void FlushLog()
        {
            Reopen();
            using (StreamWriter log = new StreamWriter(LogFileStream))
            {
                while (logQueue.Count > 0)
                {
                    if (LogFileStream != null)
                    {
                        Log entry = logQueue.Dequeue();
                        log.WriteLine(string.Format("{0} : {1}\t{2}", entry.LogDate, entry.LogTime, entry.Message));
                    }
                }
                log.Flush();
                log.Close();
            }
        }
        public void CleanLog()
        {            
            Reopen();
            using (StreamWriter log = new StreamWriter(LogFileStream))
            {
                while (logQueue.Count > 0)
                {
                    Log entry = logQueue.Dequeue();
                    log.WriteLine(string.Format("{0} : {1} : {2} \t{3}", entry.LogDate, entry.LogTime, entry.MessageTypeString, entry.Message));
                }
                log.Flush();
                log.Close();
            }
            LogFileStream.Dispose();
        }
    }

    /// <Description>
    /// A Log class to store messages,types of message and Date and Time the log message was created
    /// </Description>
    public class Log
    {
        public string Message { get; set; }
        public string LogTime { get; set; }
        public string LogDate { get; set; }
        public string MessageTypeString { get; set; }

        public Log(string message, LogConstants.MESSAGETYPE MessageType)
        {
            Message = message;
            MessageTypeString = GetTypeString(MessageType);
            LogDate = DateTime.Now.ToString("yyyy-MM-dd");
            LogTime = DateTime.Now.ToString("hh:mm:ss.fff tt");
        }
        private string GetTypeString(LogConstants.MESSAGETYPE MessageType)
        {
            string TypeString;
            switch (MessageType)
            {
                case LogConstants.MESSAGETYPE.INFO: TypeString = "INFO"; break;
                case LogConstants.MESSAGETYPE.ERROR: TypeString = "ERROR"; break;
                case LogConstants.MESSAGETYPE.EXCEPTION: TypeString = "EXCEPTION"; break;
                case LogConstants.MESSAGETYPE.WARNING: TypeString = "WARNING"; break;
                default: TypeString = "UNKNOWN"; break;

            }
            return TypeString;

        }
    }
}
