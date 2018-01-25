using System;

namespace DR.Marvin.Model
{
    /// <summary>
    /// Semaphore model, used for probing the semaphore.
    /// </summary>
    public class Semaphore
    {
        /// <summary>
        /// Semaphore identifier
        /// </summary>
        public string SemaphoreId { get; set; }
        /// <summary>
        /// Current owner ifendtifer 
        /// </summary>
        public string CurrentOwnerId { get; set; }
        /// <summary>
        /// Time stamp of last update
        /// </summary>
        public DateTime HeartBeat { get; set; }
    }
}
