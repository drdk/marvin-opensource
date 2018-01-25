using System;

namespace DR.Marvin.Model
{
    /// <summary>
    /// Used to store port configuration and generated id for process
    /// </summary>
    public static class Utilities
    {
        /// <summary>
        /// Configure the current port the progress is runnning on.
        /// </summary>
        public static int Port { get; set; } = 0;
        /// <summary>
        /// Id in the format {MachineName}:{Port}
        /// </summary>
        public static string GetCallerId()
        {
            return $"{Environment.MachineName}:{Port}";
        }
    }
}
