using System;
using System.Diagnostics;
using System.IO;
using DR.Marvin.Model;
using Newtonsoft.Json;

namespace DR.Marvin.Logging
{
    public class DummyLogging : ILogging
    {
        public void LogException(Exception exception, string message)
        {
            var oldColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Debug.WriteLine("Exception : " + message + " " + exception.ToString());
            Console.ForegroundColor = oldColor;
        }

        public void LogException(Exception exception, string message, string urn)
        {
            LogException(exception, message + " " + urn);
        }

        public void LogWarning(string message, string urn)
        {
            LogWarning(message + " " + urn);
        }

        public void LogWarning<T>(string message, T data)
        {
            JsonHelper.SerializeJsonData(LogWarning, message, data);
        }

        public void LogWarning(string message)
        {
            var oldColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Yellow;
            Debug.WriteLine("Warning:" + message);
            Console.ForegroundColor = oldColor;
        }

        public void LogInfo(string message)
        {
            Debug.WriteLine("Info:" + message);
        }

        public void LogInfo(string message, string urn)
        {
            LogInfo(message + " " + urn);
        }

        public void LogInfo<T>(string message, T data)
        {
            JsonHelper.SerializeJsonData(LogInfo, message,data);
        }

        public void LogDebug(string message)
        {
            Debug.WriteLine("Debug: " + message);
        }

        public void LogDebug(string message, string urn)
        {
            LogDebug(message + " " + urn);
        }
    }
}
