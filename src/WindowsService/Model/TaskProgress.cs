using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DR.Marvin.WindowsService.Model
{
    /// <summary>
    /// View model for the task status of a given job.
    /// </summary>
    [UsedImplicitly]
    public class TaskProgress
    {
        /// <summary>
        /// How much of the task is done
        /// </summary>
        [Required]
        public int PercentDone { get; [UsedImplicitly] internal set; }
        /// <summary>
        /// Of the total job, how big is this task
        /// </summary>
        [Required]
        public int PercentOfTotal { get; [UsedImplicitly] internal set; }
        /// <summary>
        /// Name of the task
        /// </summary>
        [Required]
        public string Name { get; [UsedImplicitly] internal set; }
    }
}
