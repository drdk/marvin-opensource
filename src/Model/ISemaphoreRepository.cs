using System;

namespace DR.Marvin.Model
{
    /// <summary>
    /// Semaphore repository interface
    /// </summary>
    public interface ISemaphoreRepository
    {
        /// <summary>
        /// The maximum since a given semaphore has been refreshed via the Get method.
        /// </summary>
        TimeSpan MaxAge { get; }

        /// <summary>
        /// Get and "Heartbeat" method. Used to lock a given semaphore by id. Must be called as a "heartbeat" more 
        /// offen than the defined MaxAge.
        /// </summary>
        /// <param name="semaphoreId">The id of the desired semaphore, use nameof(caller-class) or similar.</param>
        /// <param name="callerId">Unique id of caller, see Utilities.GetCallerId for an example of af caller id generator.</param>
        /// <param name="currentOwnerId">Output parameter for the caller id of who has the lock at the moment.
        /// Only relevant if the Return value is false.</param>
        /// <returns>True if locking of the successful.</returns>
        bool Get(string semaphoreId, string callerId, out string currentOwnerId);

        /// <summary>
        /// Releases a given semaphore id, if the callerId matches the current owner.
        /// Used to gracefully release control of semaphore.
        /// </summary>
        /// <param name="semaphoreId">Target semaphore id.</param>
        /// <param name="callerId">Caller id, must match current owner, otherwise the method has no effect.</param>
        void Release(string semaphoreId, string callerId);

        /// <summary>
        /// Probes a specific semaphore.
        /// </summary>
        /// <param name="semaphoreId">The id of the desired semaphore, use nameof(caller-class) or similar.</param>
        /// <returns>Semaphore</returns>
        Semaphore Probe(string semaphoreId);
    }
}
