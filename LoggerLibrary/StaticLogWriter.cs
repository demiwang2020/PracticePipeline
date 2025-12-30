using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace LoggerLibrary
{
    public class StaticLogWriter
    {
        private static StaticLogWriter logInfoInstance;
        private FileStream fs;
        private StreamWriter sw;

        public static string LineSeparator1 { get; private set; }
        public static string LineSeparator2 { get; private set; }
        public bool LogOff { get; set; }
        public bool TimestampOff { get; set; }

        private StaticLogWriter(string logPath)
        {
            if (!File.Exists(logPath))
            {
                FileStream tempFS = File.Create(logPath);
                tempFS.Close();
            }

            fs = new FileStream(logPath, FileMode.Append);
            sw = new StreamWriter(fs);
            LogOff = false;
            TimestampOff = false;

            LineSeparator1 = "-----------------------------------------";
            LineSeparator2 = "=========================================";
        }

        public static StaticLogWriter Instance
        {
            get
            {
                return logInfoInstance;
            }
        }

        public static void createInstance(string logPath)
        {
            if (logInfoInstance == null)
            {
                logInfoInstance = new StaticLogWriter(logPath);
            }
        }

        public void logMessage(string message)
        {
            if (LogOff)
                return;

            string output = TimestampOff ? message : String.Format("[{0}] {1}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), message);
            Console.WriteLine(output);

            if (sw == null)
                return;

            sw.WriteLine(output);
            sw.Flush();
        }

        public void logScenario(string scenarioName)
        {
            logMessage(LineSeparator2);
            logMessage(scenarioName);
            logMessage(LineSeparator1);
        }

        public void logError(string message)
        {
            logMessage(String.Format("!!Error: {0}", message));
        }

        public void logWarning(string message)
        {
            logMessage(String.Format("Warning: {0}", message));
        }

        public void close()
        {
            if (logInfoInstance != null)
            {
                logInfoInstance = null;
            }

            if (sw!=null)
            {
                sw.Close();
                sw = null;
            }

            if (fs!=null)
            {
                fs.Close();
                fs = null;
            }
        }
    }
}
