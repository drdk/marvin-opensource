using System;
using DR.Marvin.Model;
using JetBrains.Annotations;

namespace DR.Marvin.WindowsService
{
    /// <summary>
    /// Real time provider, uses DateTime.UtcNow.
    /// </summary>
    [UsedImplicitly]
    public class RealTimeProvider : ITimeProvider
    {
        /// <inheritdoc />
        public DateTime GetUtcNow()
        {
            return DateTime.UtcNow;
        }
    }
}
