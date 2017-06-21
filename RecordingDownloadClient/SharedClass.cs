using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RecordingDownloadClient
{
    public static class SharedClass
    {
        private static log4net.ILog logger = null;
        private static string connectionStringP = null;
        private static string connectionStringS = null; 
        private static bool hasStopSignal = false;
        private static string _savingPathS = null;
        private static string _savingPathP = null;
        private static bool _poolStaging = false;
        public static void InitiaLizeLogger()
        {
            log4net.GlobalContext.Properties["LogName"] = DateTime.Now.ToString("yyyyMMdd");
            log4net.Config.XmlConfigurator.Configure();
            logger = log4net.LogManager.GetLogger("TraceLogger");
        }

        public static string ConnectionString { get { return connectionStringP; } set { connectionStringP = value; } }
        public static string ConnectionStringStaging { get { return connectionStringS; } set { connectionStringS = value; } }

        public static bool HasStopSignal { get { return hasStopSignal; } set { hasStopSignal = value; } }
        public static string SavingPathProduction { get { return _savingPathP == null ? System.Configuration.ConfigurationManager.AppSettings["ClipSavingPathProduction"] : _savingPathP; } }

        public static string SavingPathStaging { get { return _savingPathS == null ? System.Configuration.ConfigurationManager.AppSettings["ClipSavingPathStaging"] : _savingPathS; } }

        public static bool PollStaging { get { return _poolStaging; } set { _poolStaging = value; } }

        public static string GetConnectionString(Environment environment)
        {
            if (environment == Environment.STAGING)
                return ConnectionStringStaging;
            else
                return ConnectionString;
        }


        public static log4net.ILog Logger
        {
            get { return logger; }
        }

    }
}
