using System;
using System.Collections.Generic;
using System.Linq;
using DR.Marvin.Model;
using DR.Marvin.Plugins.Wfs;
using DR.Marvin.Plugins.FFMpeg;
using DR.Marvin.Plugins.FileRenamer;

namespace DR.Marvin.Planner
{
    /// <summary>
    /// Naïve planner. Orders waiting tasks by priority then due date. 
    /// </summary>
    public class SimplePlanner : CommonPlanner
    {
        private readonly ICallbackService _callbackService;
        public SimplePlanner(IEnumerable<IPlugin> plugins, IJobRepository jobRepository, ILogging logging, ICallbackService callbackService) :
            base(plugins, jobRepository, logging)
        {
            _callbackService = callbackService;
        }
        
        public override void Calculate()
        {
            var waiting = 
                JobRepository.WaitingJobs()
                .OrderByDescending(j => j.Priority)
                .ThenBy(j => j.DueDate);

            var pluginStatsDict = GetPluginStats();
            foreach (var job in waiting)
            {
                var mainTaskType = GetPluginTypeForJob(job);
                if (string.IsNullOrEmpty(mainTaskType)) // cancel unsupported job type
                {
                    CancelJob(job);
                    continue;
                }
                var stats = pluginStatsDict[mainTaskType];

                // always reserve the last node to high priority jobs (if we have more than one plugin)
                // if no high priority jobs check if there is a max 5 minutes file
                // Does not reserve plugin for high priority ffmpeg jobs
                if (mainTaskType != "ffmpeg" && job.Priority != Priority.high &&
                    stats.TotalCount > 1 &&
                    stats.BusyCount >= stats.TotalCount - 1 &&
                    job.Source.Duration > TimeSpan.FromMinutes(5).TotalMilliseconds
                    )
                    continue; // skip for now

                var ep = new ExecutionPlan { Tasks = new List<ExecutionTask>() };
                var temporaryTranscodingEssence = false;

                var transcodingSource = job.Source;

                #region optional hard subs task
                if(!PreprocessTask(StateFlags.HardSubtitles, job, ep, ref transcodingSource, pluginStatsDict, ref temporaryTranscodingEssence))
                    continue;
                #endregion

                #region optional muxing audio task
                if (!PreprocessTask(StateFlags.AlternativeAudio, job, ep, ref transcodingSource, pluginStatsDict, ref temporaryTranscodingEssence))
                    continue;
                #endregion

                #region transcoding task
                if (stats.FreePlugin.Count == 0)
                    continue; // skip for now
                var freePlugin = stats.FreePlugin.Peek();
                var transcodingTask = new ExecutionTask
                {
                    From = new Essence(transcodingSource),
                    To = new Essence(job.Destination) { Files = null },
                    Arguments = new Dictionary<string, string> { { "Name", job.Name } },
                    PluginUrn = freePlugin.Urn
                };
                if (temporaryTranscodingEssence)
                {
                    transcodingTask.Arguments.Add("TemporaryEssence", "From");
                }
                if (!freePlugin.CheckAndEstimate(transcodingTask))
                {
                    CancelJob(job);
                    continue;
                }
                ep.Tasks.Add(transcodingTask);
                #endregion

                #region optional renaming task
                if (job.Destination.Files?.Count > 0)
                {
                    if (pluginStatsDict[FileRenamer.Type].FreePlugin.Count != 1)
                        throw new Exception("Non-Async plugins, should always be free and only registred once.");

                    var renamer = pluginStatsDict[FileRenamer.Type].FreePlugin.Peek();
                    var renamingTask = new ExecutionTask
                    {
                        From = transcodingTask.To,
                        To = new Essence(job.Destination),
                        PluginUrn = renamer.Urn
                    };
                    if (!renamer.CheckAndEstimate(renamingTask))
                    {
                        CancelJob(job);
                        continue;
                    }
                    ep.Tasks.Add(renamingTask);
                }
                #endregion

                #region update plugin usage / busy count
                foreach (var taskPluginUrn in ep.Tasks.Select(t=>t.PluginUrn).Distinct())
                {
                    string type;
                    if (taskPluginUrn.StartsWith(FFMpeg.UrnPrefix))
                        type = FFMpeg.Type;
                    else if (taskPluginUrn.StartsWith(Wfs.UrnPrefix))
                        type = Wfs.Type;
                    else if (taskPluginUrn.StartsWith(FileRenamer.UrnPrefix))
                        type = FileRenamer.Type;
                    else
                        throw new NotImplementedException();

                    var ps = pluginStatsDict[type];

                    var plugin = ps.FreePlugin.Peek();

                    if (plugin.Urn != taskPluginUrn)
                        throw new Exception("Fatal planner error");

                    if (!plugin.AsyncOperation)
                        continue; // synchronously executed tasks, do not need to be counted as busy.
                    ps.FreePlugin.Pop();
                    ps.BusyCount++;
                }
                #endregion

                // save calulated plan
                job.Plan = ep;
                JobRepository.Update(job);
            }
        }

        private bool PreprocessTask(StateFlags flag, Job job, ExecutionPlan ep, ref Essence transcodingSource, Dictionary<string, PluginStats> pluginStatsDict, ref bool temporaryTranscodingEssence)
        {
            AttachmentType aType;
            string taskType;
            var toFormat = job.Source.Format;

            switch (flag)
            {
                case StateFlags.HardSubtitles:
                    aType = AttachmentType.Subtitle;
                    taskType = "hardsubs";
                    toFormat = StateFormat.xd5c; // hard sub'ing changes the format to xdcam
                    break;
                case StateFlags.AlternativeAudio:
                    aType = AttachmentType.Audio;
                    taskType = "mux";
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(flag), flag, "Unsupported preprocess task");
            }
            if (!job.Destination.Flags.HasFlag(flag))
                return true;

            // add preprocess if needed.
            if (pluginStatsDict[FFMpeg.Type].FreePlugin.Count == 0)
                return false; // skip for now

            var ffmpegPlugin = pluginStatsDict[FFMpeg.Type].FreePlugin.Peek();
            var preprocessTask = new ExecutionTask
            {
                From = new Essence(transcodingSource),
                To = new Essence(transcodingSource)
                {
                    Files = null,
                    Attachments = transcodingSource.Attachments.Where(a => a.Type != aType).ToList(),
                    Flags = transcodingSource.Flags | flag,
                    Format = toFormat,
                    Path = System.IO.Path.Combine(job.Source.Path, $"tmp-{taskType}-{job.Id}") //Tmp folder
                },
                PluginUrn = ffmpegPlugin.Urn
            };
            if (temporaryTranscodingEssence)
            {
                preprocessTask.Arguments.Add("TemporaryEssence", "From");
            }

            if (!ffmpegPlugin.CheckAndEstimate(preprocessTask))
            {
                CancelJob(job);
                return false;
            }
            ep.Tasks.Add(preprocessTask);
            transcodingSource = new Essence(preprocessTask.To)
            {
                Files = transcodingSource.Files
            };
            temporaryTranscodingEssence = true;
            return true;
        }
        private void CancelJob(Job job)
        {
            job.Plan = new ExecutionPlan(); //Empty plan == canceled
            JobRepository.Update(job);
            try
            {
                _callbackService.MakeCallback(job);
            }
            catch (Exception e)
            {
                Logging.LogException(e, $"Callback to  {job.CallbackUrl} failed", job.Urn);
            }
            Logging.LogWarning("Unable to calculate plan for job.", job.Urn);
        }

        private static string GetPluginTypeForJob(Job current)
        {
            //Find the first plugin type that can handle the required destination format
            if (Wfs.SupportedDestinationFormats.Contains(current.Destination.Format))
                return Wfs.Type;

            if (FFMpeg.SupportedAudioDestinationFormats.Contains(current.Destination.Format))
                return FFMpeg.Type;

            return null; //None of the plugins will work
        }

        private Dictionary<string, PluginStats> GetPluginStats()
        {
            var pluginTypes = Plugins.Select(p => p.PluginType).Distinct();

            // these plugins is already in use or planed to be used in the future, count'em as busy.
            var reservedPluginUrns = JobRepository.ActiveJobs()
               .SelectMany(j => j.Plan.Tasks.Where(t => t.State != ExecutionState.Done).Select(t => t.PluginUrn))
               .Distinct();

            return pluginTypes.ToDictionary(type => type, type => new PluginStats()
            {
                TotalCount = Plugins.Count(p => p.PluginType == type),
                BusyCount = Plugins.Count(
                    p => p.PluginType == type && 
                    (p.Busy || (p.AsyncOperation && reservedPluginUrns.Contains(p.Urn)))),
                FreePlugin = new Stack<IPlugin>(Plugins.Where(
                    p => p.PluginType == type && 
                    !(p.Busy || (p.AsyncOperation && reservedPluginUrns.Contains(p.Urn)))))
            });
        }

        private class PluginStats
        {
            public int TotalCount { get; set; }
            public int BusyCount { get; set; }
            public Stack<IPlugin> FreePlugin { get; set; }
        }
    }

    
}

