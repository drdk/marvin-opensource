using System;
using JetBrains.Annotations;
#pragma warning disable 1591


namespace DR.Marvin.Model
{
    /// <summary>
    /// This the internal representation of a job.
    /// </summary>
    // ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
    public class Job
    {
        private const string UrnPrefix = "urn:dr:marvin:job:";
        /// <summary>
        /// Identifier, last part of urn
        /// </summary>
        public Guid Id;

        /// <summary>
        /// Construtor, generates a new random Guid as id
        /// </summary>
        public Job()
        {
            Id = Guid.NewGuid();
        }

        /// <summary>
        /// Uniform Resource Name
        /// </summary>
        public string Urn => UrnPrefix + Id;

        /// <summary>
        /// Foreign key of caller, must validate as an URN.
        /// </summary>
        public string SourceUrn { get; [UsedImplicitly] set; }

        /// <summary>
        /// Human readable title of the current job, used in dashboards ect.
        /// </summary>
        public string Name { get; [UsedImplicitly]set; }

        /// <summary>
        /// Date the job was received by Marvin
        /// </summary>
        public DateTime Issued { get; set; }
        /// <summary>
        /// Desired date the job should be completed before.
        /// </summary>
        public DateTime DueDate { get; set; }
        public Priority Priority { get; set; }
        public Essence Source { get; set; }
        public Essence Destination { get; set; }
        public virtual ExecutionPlan Plan { get; set; }
        public DateTime? EndTime { get; set; }
        public string CallbackUrl { get; set; }
        public DateTime LastModified { get; set; }
    }

    public enum Priority
    {
        #pragma warning disable 1591
        // ReSharper disable InconsistentNaming
        low = 0,
        medium = 5,
        high = 10
        // ReSharper restore InconsistentNaming
        #pragma warning restore 1591
    }
}
