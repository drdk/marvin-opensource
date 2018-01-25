using System;
using System.Collections.Generic;
using System.Linq;
using DR.Marvin.Model;
using DR.Marvin.WindowsService.Model;

namespace DR.Marvin.WindowsService.AutomapperHelper
{
    /// <summary>
    /// Utility class for non standard mappings
    /// </summary>
    public static class MappingHelper
    {
        /// <summary>
        /// Takes a job and calculates estimed percentage done
        /// </summary>
        /// <param name="job">job to calculate on</param>
        /// <param name="timeProvider">provider to supply the current time</param>
        /// <returns>percentage done</returns>
        public static double CalculatePercentageDone(Job job, ITimeProvider timeProvider)
        {
            var currentTime = timeProvider.GetUtcNow();
            //Check If we have a plan otherwise return 0
            if (job.Plan == null) return 0;
            //If we do have a plan, but it's completed return 100
            if (job.Plan.GetState() == ExecutionState.Done) return 100;
            //Take the first task and get start time
            var firstTask = job.Plan.Tasks.FirstOrDefault();
            var starttime = firstTask?.StartTime;
            //if it's null or in the future, return 0
            if (starttime == null || (starttime > currentTime)) return 0;
            //Calc total time estimated from all tasks
            var totalEstimation = TimeSpan.FromTicks(job.Plan.Tasks.Sum(a => a.Estimation.Ticks));
            //How much time has passed?
            var timeElapsed = currentTime - starttime;
            //Calculate percentage
            var percentDone = (timeElapsed.Value.Ticks / (double)totalEstimation.Ticks) * 100;
            //If we go overtime, just return 99 for prettyness
            return percentDone >= 100 ? 99 : percentDone;
        }
        
        private static double CalculatePercentageDone(ExecutionTask task, ITimeProvider timeProvider)
        {
            if (task.State == ExecutionState.Done)
                return 100;
            var currentTime = timeProvider.GetUtcNow();
            var starttime = task.StartTime;
            //if it's null or in the future, return 0
            if (starttime == null || (starttime > currentTime))
                return 0;
            //How much time has passed?
            var timeElapsed = currentTime - starttime;
            //Calculate percentage
            var percentDone = timeElapsed.Value.Ticks * 100 / (double)task.Estimation.Ticks;
            //If we go overtime, just return 99 for prettyness
            return percentDone >= 100 ? 99 : percentDone;
        }

        /// <summary>
        /// Takes a job and calculates estimed percentage done
        /// </summary>
        /// <param name="job">job to calculate on</param>
        /// <param name="timeProvider">provider to supply the current time</param>
        /// <returns>percentage done</returns>
        public static List<TaskProgress> CalculateTaskPercentDone(Job job, ITimeProvider timeProvider)
        {
            //Check If we have a plan otherwise return 0
            if (job.Plan == null)
                return new List<TaskProgress> { new TaskProgress { Name = "None", PercentDone = 0, PercentOfTotal = 0 } };

            var totalEstimation = TimeSpan.FromTicks(job.Plan.Tasks.Sum(a => a.Estimation.Ticks));

            return job.Plan.Tasks.Where(t => t.Estimation.Ticks != 0 /*Dont report progress for instant tasks*/).Select(exeTask => new TaskProgress
            {
                Name = exeTask.PluginUrn.GetPluginTypeFromUrn(),
                PercentOfTotal = (int) (exeTask.Estimation.Ticks*100/(double) totalEstimation.Ticks),
                PercentDone = (int) CalculatePercentageDone(exeTask, timeProvider)
            }).ToList();
        }

        /// <summary>
        /// Returns estimated time of arrival
        /// </summary>
        /// <param name="job">job to calculate on</param>
        /// <returns>Datetime with estimated done</returns>
        public static DateTime? CalculateEstimatedDone(Job job)
        {
            if (job.Plan != null)
            {
                var startTime = job.Plan.Tasks.FirstOrDefault()?.StartTime;
                var totalEstimation = TimeSpan.FromTicks(job.Plan.Tasks.Sum(a => a.Estimation.Ticks));

                return startTime + totalEstimation;
            }
            return null;
        }

        /// <summary>
        /// Extracts data from an order to make a source essence object
        /// </summary>
        /// <param name="order">Order to extract from</param>
        /// <returns>Source essence object</returns>
        public static Essence GetSource(Order order)
        {
            IList<Attachment> attachments = new List<Attachment>();

            if (order.BurnInLogo.GetValueOrDefault(false) && !string.IsNullOrEmpty(order.LogoPath))
                attachments.Add(new Attachment() { Path = order.LogoPath, Type = AttachmentType.Logo });

            if (order.BurnInSubtitles.GetValueOrDefault(false))
                attachments.Add(new Attachment() { Path = order.SubtitlesPath, Type = AttachmentType.Subtitle });

            if (!string.IsNullOrEmpty(order.AlternateAudioPath))
                attachments.Add(new Attachment() { Path = order.AlternateAudioPath, Type = AttachmentType.Audio });

            if (!string.IsNullOrEmpty(order.IntroFilePath))
                attachments.Add(new Attachment()
                {
                    Path = order.IntroFilePath, Type = AttachmentType.Intro,
                    Arguments = new Dictionary<string, string> { { "Duration", $"{order.IntroDuration.GetValueOrDefault(0)}"}}
                });

            if (!string.IsNullOrEmpty(order.OutroFilePath))
                attachments.Add(new Attachment()
                {
                    Path = order.OutroFilePath, Type = AttachmentType.Outro,
                    Arguments = new Dictionary<string, string> { { "Duration", $"{order.OutroDuration.GetValueOrDefault(0)}" } }
                });

            return new Essence
            {
                Path = order.FilePath.Substring(0, order.FilePath.LastIndexOf("\\")),
                Files = new List<EssenceFile> { order.FilePath.Substring(order.FilePath.LastIndexOf("\\") + 1) },
                Format = order.Format,
                Attachments = attachments,
                Duration = order.Duration,
                AspectRatio = order.AspectRatio,
                CustomFormat = order.CustomFormat
            };
        }

        /// <summary>
        /// Extracts data from an order to make a destination essence object
        /// </summary>
        /// <param name="order">Order to extract from</param>
        /// <returns>Destination essence object</returns>
        public static Essence GetDestination(Order order)
        {
            var dState = 
                (order.BurnInLogo.GetValueOrDefault(false) ? StateFlags.Logo : StateFlags.None) |
                (order.BurnInSubtitles.GetValueOrDefault(false) ? StateFlags.HardSubtitles : StateFlags.None) |
                (!string.IsNullOrEmpty(order.AlternateAudioPath) ? StateFlags.AlternativeAudio : StateFlags.None) |
                (!string.IsNullOrEmpty(order.IntroFilePath) ? StateFlags.Intro : StateFlags.None) |
                (!string.IsNullOrEmpty(order.OutroFilePath) ? StateFlags.Outro : StateFlags.None);
            return new Essence
            {
                Path = order.DestinationPath,
                Format = order.DestinationFormat,
                Flags = dState,
                AspectRatio = order.AspectRatio,
                Resolution = order.Resolution,
                Files = ConvertDestinationFilenameToEssenceFile(order.DestinationFilename),
                Duration = order.IntroDuration.GetValueOrDefault(0) + order.Duration + order.OutroDuration.GetValueOrDefault(0)
            };
        }

        private static IList<EssenceFile> ConvertDestinationFilenameToEssenceFile(string destinationFilename)
        {
            if (string.IsNullOrEmpty(destinationFilename))
                return null;
            return new List<EssenceFile> {
                EssenceFile.Template(
                    destinationFilename.Contains('%') ?
                    destinationFilename :
                    $"{destinationFilename}_%index%.%ext%") };
        }
    }
}