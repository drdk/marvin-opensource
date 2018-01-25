using System;
#pragma warning disable 1591

namespace DR.Marvin.Model
{
    /// <summary>
    /// Logging repo interface
    /// </summary>
    public interface ILogging
    {
        void LogException(Exception exception, string message);
        void LogException(Exception exception, string message,string urn);
        void LogWarning(string message, string urn);
        void LogWarning<T>(string message, T data);
        void LogWarning(string message);
        void LogInfo(string message);
        void LogInfo(string message, string urn);
        void LogInfo<T>(string message, T data);
        void LogDebug(string message);
        void LogDebug(string message, string urn);
    }
}
