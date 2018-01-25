using System;
using DR.Marvin.Model;
using log4net;

namespace DR.Marvin.Logging
{
    public class Log4NetLogging : ILogging
    {
        private readonly ILog _log;
        public Log4NetLogging()
        {
            _log = LogManager.GetLogger("Marvin");
        }

        public void LogException(Exception exception, string message)
        {
            _log.Error(message, exception);
        }

        public void LogException(Exception exception, string message, string urn)
        {
            LogException(exception, message +" urn: " + urn);
        }

        public void LogWarning(string message, string urn)
        {
            LogWarning(message + " urn: " + urn);
        }

        public void LogWarning<T>(string message, T data)
        {
            JsonHelper.SerializeJsonData(_log.Warn, message, data);
        }

        public void LogWarning(string message)
        {
            _log.Warn(message);
        }

        public void LogInfo(string message)
        {
            _log.Info(message);
        }

        public void LogInfo(string message, string urn)
        {
            LogInfo(message + " urn: " + urn);
        }
        
        public void LogInfo<T>(string message, T data)
        {
            JsonHelper.SerializeJsonData(_log.Info, message,data);
        }

        public void LogDebug(string message)
        {
            _log.Debug(message);
        }

        public void LogDebug(string message, string urn)
        {
            LogDebug(message + " urn: " + urn);
        }
    }
}
