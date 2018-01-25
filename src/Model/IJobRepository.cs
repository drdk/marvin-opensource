using System;
using System.Collections.Generic;
#pragma warning disable 1591

namespace DR.Marvin.Model
{
    /// <summary>
    /// Job Repository interface.
    /// </summary>
    public interface IJobRepository
    {
        void Add(Job job);
        void Update(Job job);
        IEnumerable<Job> ActiveJobs();
        IEnumerable<Job> WaitingJobs();
        IEnumerable<Job> DoneJobs(DateTime? after = null);
        Job Get(string urn);
        Job GetNewest();
        IEnumerable<Job> FailedJobs(DateTime? after = null);
        IEnumerable<Job> CanceledJobs(DateTime? after = null);
        /// <summary>
        /// Get current DB environment
        /// </summary>
        /// <returns>Current DB environment</returns>
        string GetEnvironment();

    }
}
