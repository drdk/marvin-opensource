using System;

namespace DR.Marvin.WindowsService.Model
{
    /// <inheritdoc />
    [Serializable]
    public class OrderException : Exception
    {
        /// <inheritdoc />
        public OrderException(string message) : base(message) { }
    }
}