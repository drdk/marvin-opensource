using System;

namespace DR.Marvin.WindowsService.Model
{
    /// <summary>
    /// View model for version infomation. Simulator the __version.js-format.
    /// </summary>
    public class VersionInfo
    {
        /// <summary>
        /// Last modified time of the assembly.
        /// </summary>
        public DateTime BuildTime { get; set; }
        /// <summary>
        /// The git commit hash.
        /// </summary>
        public string GitSha { get; set; }
        /// <summary>
        /// The remote hostname and repo name.
        /// </summary>
        public string Remote { get; set; }
        /// <summary>
        /// Branch name.
        /// </summary>
        public string Branch { get; set; }
    }
}
