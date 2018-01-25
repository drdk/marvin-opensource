using System;
using System.Collections.Generic;

namespace DR.Marvin.Model
{
    /// <summary>
    /// A sub task of a given plan.
    /// </summary>
    public class ExecutionTask
    {
        /// <summary>
        /// Identifier, last part of urn
        /// </summary>
        public Guid Id;
        /// <summary>
        /// Constructor
        /// </summary>
        public ExecutionTask()
        {
            Id = Guid.NewGuid();
        }
        private const string UrnPrefix = "urn:dr:marvin:executiontask:";
        /// <summary>
        /// Uniform Resource Name
        /// </summary>
        public string Urn => UrnPrefix + Id;
        /// <summary>
        /// Optional foreign key. May be used to identify a given task in an underlying integration.
        /// </summary>
        public string ForeignKey { get; set; }
        /// <summary>
        /// Source essence
        /// </summary>
        public Essence To { get; set; }
        /// <summary>
        /// Destination essence
        /// </summary>
        public Essence From { get; set; }
        /// <summary>
        /// The time work started on the task
        /// </summary>
        public DateTime? StartTime { get; set; }
        /// <summary>
        /// The time work ended on the task
        /// </summary>
        public DateTime? EndTime { get; set; }
        /// <summary>
        /// Estimate duration of the task. Used to report progress
        /// </summary>
        public TimeSpan Estimation { get; set; }
        /// <summary>
        /// Current state of the task
        /// </summary>
        public ExecutionState State { get; set; }
        /// <summary>
        /// Plugin identifier, specfiy which plugin to use to fulfill this task
        /// </summary>
        public string PluginUrn { get; set; }
        /// <summary>
        /// Number of times a given task has been tried. Used to bail on repeatly failing tasks.
        /// </summary>
        public int NumberOfRetries { get; set; }

        private IDictionary<string, string> _arguments;
        /// <summary>
        /// Optional extra paramter to the plugin.
        /// </summary>
        public IDictionary<string, string> Arguments
        {
            get { return _arguments ?? (_arguments = new Dictionary<string, string>()); }
            set { _arguments = value; }
        }

    }
}
