using System;
using System.Collections.Generic;

namespace DR.Marvin.Model
{
    /// <summary>
    /// Data layer to store and retrive recently failed Pulse()-function errors. Use be the PulseHealthCheck.
    /// </summary>
    public interface IHealthCounterRepository
    {
        /// <summary>
        /// How old an Health counter can be before it is pruned. 
        /// </summary>
        TimeSpan MaxAge { get; }

        /// <summary>
        /// Store the last massage and increment the counter by one. (Resets the counter to one, if the previous timestamp exceedes MaxAge).
        /// </summary>
        /// <param name="id">Pulse function identifier</param>
        /// <param name="message">Message to store, only the last message is pesisted.</param>
        void Increment(string id, string message);

        /// <summary>
        /// Prunes counters older than MaxAge and returns the rest if any.
        /// Retrive all counters that
        /// </summary>
        /// <returns>Active counters.</returns>
        IEnumerable<HealthCounter> ProbeAndPrune();

    }
}
