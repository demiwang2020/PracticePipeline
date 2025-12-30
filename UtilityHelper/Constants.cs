using System;


namespace Helper
{
    /// <summary>
    /// Define  global constants
    /// </summary>
    public class GlobalConstants
    {
        static GlobalConstants()
        {
            APPNAME = System.Configuration.ConfigurationManager.AppSettings["APPNAME"].ToString();
            MTPRUNTYPE = System.Configuration.ConfigurationManager.AppSettings["MTPRUNTYPE"].ToString();
            PREMTPRUNTYPE = System.Configuration.ConfigurationManager.AppSettings["PREMTPRUNTYPE"].ToString();
            MDUSER = System.Configuration.ConfigurationManager.AppSettings["MDUSER"].ToString();
        }
        public static string APPNAME;
        public static string MTPRUNTYPE;
        public static string PREMTPRUNTYPE;
        public static string MDUSER;
    }
    /// <summary>
    /// Define Event log constant
    /// </summary>
    public class LogConstants
    {
        static LogConstants()
        {
            LOGDIR = System.Configuration.ConfigurationManager.AppSettings["LOGDIR"].ToString();
            LOGFILEPREFIX = System.Configuration.ConfigurationManager.AppSettings["LOGFILEPREFIX"].ToString();
            LOGQUEUESIZE = Convert.ToInt32(System.Configuration.ConfigurationManager.AppSettings["LOGQUEUESIZE"].ToString());
            LOGQUEUEAGE = Convert.ToInt32(System.Configuration.ConfigurationManager.AppSettings["LOGQUEUEAGE"].ToString());
        }
        public static string LOGDIR;
        public static string LOGFILEPREFIX;
        public static int LOGQUEUESIZE;
        public static int LOGQUEUEAGE;
        public enum MESSAGETYPE { INFO = 0, WARNING, ERROR, EXCEPTION };
    }
    /// <summary>
    /// define Maddog constanst
    /// </summary>
    public class MDConstants
    {
        static MDConstants()
        {

            MDSERVERNAMEWHIDBEY = System.Configuration.ConfigurationManager.AppSettings["MDSERVERNAMEWHIDBEY"].ToString();
            MDSERVERNAMEORCAS = System.Configuration.ConfigurationManager.AppSettings["MDSERVERNAMEORCAS"].ToString();
            PUCLRBRANCH = (System.Configuration.ConfigurationManager.AppSettings["PUCLRBRANCH"].ToString());
            MAINBRANCH = (System.Configuration.ConfigurationManager.AppSettings["MAINBRANCH"].ToString());
            RTMRELBRANCH = (System.Configuration.ConfigurationManager.AppSettings["RTMRELBRANCH"].ToString());

        }

        public static string MDSERVERNAMEWHIDBEY;
        public static string MDSERVERNAMEORCAS;

        public enum MDDBNAME { Whidbey, OrcasTS };

        public static string PUCLRBRANCH;
        public static string  MAINBRANCH;
        public static string RTMRELBRANCH;

    }
    /// <summary>
    /// Define operation status constant
    /// </summary>
    public class OperationStatus
    {
        public const bool SUCCESS = true;
        public const bool FAILURE = false;
    }


    public class MTPConstants
    {
        static MTPConstants()
        {
            MTPCONFIGFILE = System.Configuration.ConfigurationManager.AppSettings["MTPCONFIGFILE"].ToString();
            PREMTPCONFIGFILE = System.Configuration.ConfigurationManager.AppSettings["PREMTPCONFIGFILE"].ToString();
            TESTTYPECLICKONCE = System.Configuration.ConfigurationManager.AppSettings["TESTTYPECLICKONCE"].ToString();
            TESTTYPEJSCRIPT = System.Configuration.ConfigurationManager.AppSettings["TESTTYPEJSCRIPT"].ToString();
            TESTTYPEINSTALLUTIL = System.Configuration.ConfigurationManager.AppSettings["TESTTYPEINSTALLUTIL"].ToString();
            MDFORMATQUERYID = System.Configuration.ConfigurationManager.AppSettings["MDFORMATQUERYID"].ToString();

        }
    public static string MTPCONFIGFILE ;
    public static string PREMTPCONFIGFILE ;
    public static string TESTTYPECLICKONCE ;
    public static string TESTTYPEJSCRIPT ;
    public static string TESTTYPEINSTALLUTIL;
    public static string MDFORMATQUERYID;

    
        
    }
}
