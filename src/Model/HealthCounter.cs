using System;

namespace DR.Marvin.Model
{
    /// <summary>
    /// Result type used by the IHealthCounterRepository
    /// </summary>
    public class HealthCounter
    {
        /// <summary>
        /// Pulse Function identifer
        /// </summary>
        public string Id { get; set; }
        /// <summary>
        /// The latest message stored.
        /// </summary>
        public string Message { get; set; }
        /// <summary>
        /// Total count since last time TimeStamp exceeded IHealthCounterRepository.MaxAge
        /// </summary>
        public int Count { get; set; }
        /// <summary>
        /// Last time this entry has been increemented. 
        /// </summary>
        public DateTime TimeStamp { get; set; }
    }
}
