using System;

namespace DR.Marvin.Model
{
    /// <inheritdoc />
    [Serializable]
    public class PluginException : Exception
    {
        /// <inheritdoc />
        public PluginException(string message) : base(message) { }
        /// <inheritdoc />
        public PluginException(string message, Exception innerException) : base(message, innerException) { }
    }
}
