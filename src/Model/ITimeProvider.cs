using System;

namespace DR.Marvin.Model
{
    /// <summary>
    /// Time provider interface. 
    /// </summary>
    public interface ITimeProvider
    {
        /// <summary>
        /// Return the the now time, with kind as UTC.
        /// </summary>
        DateTime GetUtcNow();
    }
}
