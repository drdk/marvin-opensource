using System;
using System.Collections.Generic;
using DR.Marvin.Model;
using JetBrains.Annotations;
#pragma warning disable 1591

namespace DR.Marvin.WindowsService.Model
{
    /// <summary>
    /// DashboardInfo is a job status view model for waiting, running an recently ended jobs.
    /// </summary>
    public class DashboardInfo
    {
        /// <summary>
        /// Job enqueued, but not started or planed.
        /// </summary>
        public IEnumerable<DashboardJob> WaitingJobs { [UsedImplicitly] get; set; }
        /// <summary>
        /// Running jobs
        /// </summary>
        public IEnumerable<DashboardJob> ActiveJobs { [UsedImplicitly] get; set; }
        /// <summary>
        /// recently done
        /// </summary>
        public IEnumerable<DashboardJob> RecentlyDoneJobs { [UsedImplicitly] get; set; }
        /// <summary>
        /// recently failed
        /// </summary>
        public IEnumerable<DashboardJob> RecentlyFailedJobs { [UsedImplicitly] get; set; }
        /// <summary>
        /// recently cancled
        /// </summary>
        public IEnumerable<DashboardJob> RecentlyCanceledJobs { [UsedImplicitly] get; set; }
        /// <summary>
        /// list of configured plugins
        /// </summary>
        public IEnumerable<DashbaordPlugin> Plugins { [UsedImplicitly] get; set; }
    }

    /// <summary>
    /// View model for the job status of a single job
    /// TODO: Replace with JobStatus? 
    /// </summary>
    [UsedImplicitly]
    public class DashboardJob 
    {
        public string Urn { get; set; }
        public string SourceUrn { get; set; }
        public string Name { get; set; }
        public string CurrentPluginUrn { get; [UsedImplicitly] set; }
        public DateTime? EstimatedDone { get; [UsedImplicitly] set; }
        public double PercentDone { get; [UsedImplicitly] set; }
        public List<TaskProgress> TaskPercentDone { get; [UsedImplicitly] set; }
        public string State { get; [UsedImplicitly] set; }
        public DateTime? Started { get; [UsedImplicitly] set; }
        public DateTime Issued { get; set; }
        public DateTime? EndTime { get; set; }
        public Priority Priority { get; set; }
        public DateTime DueDate { get; set; }
        public int Duration { get; [UsedImplicitly] set; }
        public string SourceFormat { get; [UsedImplicitly] set; }
        public string DestionationFormat { get; [UsedImplicitly] set; }
    }

    [UsedImplicitly]
    public class DashbaordPlugin
    {
        public string Urn { get; [UsedImplicitly] set; }
        public string PluginType { get; [UsedImplicitly] set; }
    }
}
