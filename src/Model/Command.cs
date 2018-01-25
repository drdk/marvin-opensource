namespace DR.Marvin.Model
{
    /// <summary>
    /// Command model. Targets a specific job.
    /// </summary>
    public class Command
    {
        /// <summary>
        /// Desired operation
        /// </summary>
        public CommandType Type { get; set; }
        /// <summary>
        /// Job Urn
        /// </summary>
        public string Urn { get; set; }
        /// <summary>
        /// Requeried user name.
        /// </summary>
        public string Username { get; set; }
    }

    /// <summary>
    /// List of diferent command types.
    /// </summary>
    public enum CommandType
    {
        #pragma warning disable 1591
        Unknow = 0,
        Cancel = 1,
        Pause = 2,
        Resume = 3
        #pragma warning restore 1591
    }
}