using System;
#pragma warning disable 1591

namespace DR.Marvin.Model
{
    /// <summary>
    /// Plugin status message, may be used in the furure for dashboards ect.
    /// </summary>
    public class PluginStatus
    {
        /// <summary>
        /// Plugin urn
        /// </summary>
        public string SourceUrn { get; }
        public ExecutionTask CurrentTask { get; }
        public bool Busy { get; }
        public DateTime? EstimatedCompletionTime => CurrentTask?.StartTime + CurrentTask?.Estimation;
        
        /// <summary>
        /// Constructor
        /// </summary>
        public PluginStatus(string sourceUrn, ExecutionTask currentTask, bool busy)
        {
            SourceUrn = sourceUrn;
            CurrentTask = currentTask;
            Busy = busy;
        }
    }
}