using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;

namespace DR.Marvin.WindowsService
{
    /// <summary>
    /// Reads git info from data embeded in assembly. May be updated by Team City.
    /// </summary>
    public static class VersionHelper
    {
        static VersionHelper()
        {
            var assembly = Assembly.GetExecutingAssembly();
            if (assembly.Location == null) return;

            var fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
            var match = Regex.Match(fvi.ProductVersion,
                @"(?<gitRemote>git@(?<gitRepo>.+)\.git);(?<branch>.*);(?<hash>.*);");
            if (match.Success)
            {
                GitRemote = match.Groups["gitRemote"].Value;
                GitRepo = match.Groups["gitRepo"].Value.Replace(':', '/');
                Branch = match.Groups["branch"].Value;
                if (Branch == string.Empty)
                    Branch = null;
                CommitHash = match.Groups["hash"].Value;
                if (CommitHash == string.Empty)
                    CommitHash = null;
                BuildNumber = !string.IsNullOrEmpty(Branch) ? (int?)fvi.FileMajorPart : null;
            }
           
            BuildTime = new FileInfo(assembly.Location).LastWriteTime;
        }

        /// <summary>
        /// Build Number if compiled by team city.
        /// </summary>
        public static int? BuildNumber { get; }
        /// <summary>
        /// Git origin 
        /// </summary>
        public static string GitRemote { get; } = string.Empty;
        /// <summary>
        /// Repo name
        /// </summary>
        public static string GitRepo { get; } = string.Empty;
        /// <summary>
        /// Build branch. Only writing my team city builds
        /// </summary>
        public static string Branch { get; }

        private static string CommitHash { get; }
        /// <summary>
        /// 8 char commit hash.
        /// </summary>
        public static string ShortCommitHash => CommitHash?.Substring(0, 8);

        /// <summary>
        /// Build time
        /// </summary>
        public static DateTime BuildTime { get; }

        /// <summary>
        /// Link to git repo
        /// </summary>
        public static Uri GitRepoUri => string.IsNullOrEmpty(GitRemote) ? null : 
            new Uri($"http://{GitRepo}");

        /// <summary>
        /// Link to branch
        /// </summary>
        public static Uri GitBranchUri => string.IsNullOrEmpty(Branch) ? null :
            new Uri($"{GitRepoUri}/tree/{Branch}");
        /// <summary>
        /// Link to commit
        /// </summary>
        public static Uri GitCommitUri => string.IsNullOrEmpty(CommitHash) ? null :
            new Uri($"{GitRepoUri}/commit/{CommitHash}");
    }
}
