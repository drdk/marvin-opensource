using System;
using System.ComponentModel.DataAnnotations;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace DR.Marvin.WindowsService.Model
{
    /// <summary>
    /// View model for the status of a given job.
    /// </summary>
    [UsedImplicitly]
    public class JobStatus
    {
        /// <summary>
        /// How much of the job is done
        /// </summary>
        [Required]
        public int PercentDone { get; [UsedImplicitly] internal set; }
        /// <summary>
        /// Guid of the job in question
        /// </summary>
        [Required]
        public string JobUrn { get; [UsedImplicitly] internal set; }
        /// <summary>
        /// State of the job
        /// </summary>
        [Required]
        public string State { get; [UsedImplicitly] internal set; }
        /// <summary>
        /// Estimated done 
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Include)]
        public DateTime? EstimatedDone { get; [UsedImplicitly] internal set; }
        /// <summary>
        /// Time issued
        /// </summary>
        public DateTime Issued { get; [UsedImplicitly] internal set; }
        /// <summary>
        /// Time work has started on the job.
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Include)]
        public DateTime? Started { get; [UsedImplicitly] internal set; }
        /// <summary>
        /// Time work was completed
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Include)]
        public DateTime? EndTime { get; [UsedImplicitly] internal set; }
    }
}
