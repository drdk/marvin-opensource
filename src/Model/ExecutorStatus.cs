using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace DR.Marvin.Model
{
    /// <summary>
    /// Status message from the executor
    /// </summary>
    public class ExecutorStatus
    {
        /// <summary>
        /// Statuses of every plugin
        /// </summary>
        public IList<PluginStatus> PluginStatuses { get; set; }
        /// <summary>
        /// Timestamp from when the query was completed
        /// </summary>
        public DateTime TimeStamp { [UsedImplicitly] get; set; }
        /// <summary>
        /// True if the executor has the lock of the semaphore
        /// </summary>
        public bool IsPrimary { get; set; }
    }
}
