using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DR.Marvin.Model;
using DR.Marvin.Plugins.Common;
using DR.WfsService.Contract;
using JobStatus = DR.WfsService.JMServices.JobStatus;

namespace DR.Marvin.Plugins.Wfs
{
    public class Wfs : DynamicPlugin
    {
        public static readonly string Type = nameof(Wfs).ToLower();
        public static string UrnPrefix => $"{UrnHelper.UrnBase}{PluginBaseUrn}{Type}:";
        public override string PluginType => Type;
        private readonly IWfsService _wfsService;
        private readonly IDictionary<Tuple<StateFormat,AspectRatio,Resolution,bool>, Guid> _presetToWorkflow;
        private readonly string _machineGroup;

        public Wfs(string urn, ITimeProvider timeProvider, ILogging logging, IWfsService wfsService, IPresetProvider presetProvider) : base(urn, Type, timeProvider, logging)
        {
            if (wfsService == null)
                throw new ArgumentNullException(nameof(wfsService));

            if(presetProvider==null)
                throw new ArgumentNullException(nameof(presetProvider));

            _wfsService = wfsService;
            _presetToWorkflow = presetProvider.AsDictionary();
            _machineGroup = presetProvider.MachineGroup;
        }

        public static readonly IEnumerable<StateFormat> SupportedDestinationFormats =
             new StateFormat[] {
                 StateFormat.h264_od_dropfolder,
                 StateFormat.h264_od_single,
                 StateFormat.h264_od_standard,
                 StateFormat.h264_od_podcast,
             };
        public static readonly IEnumerable<StateFormat> SupportedSourceFormats =
             new StateFormat[] {
                 StateFormat.xd5c,
                 StateFormat.dvpp,
                 StateFormat.dv5p,
                 StateFormat.dvh5,
                 StateFormat.dvhq,
                 StateFormat.dv,
                 StateFormat.avc1,
                 StateFormat.custom, // TODO: remove custom format when we know every valid input format we need to support in marvin
             };
        public override bool CheckAndEstimate(ExecutionTask task)
        {
            InternalTaskCheck(task);
            // Check from and to essence
            if (Enum.GetValues(typeof(StateFlags))
                .Cast<StateFlags>()
                .Where(value => value != StateFlags.Logo) // allow logo burn in
                .Any(value => task.To.Flags.HasFlag(value) != task.From.Flags.HasFlag(value)))
            { 
                // unsupported task
                return false;
            }

            if (!SupportedSourceFormats.Contains(task.From.Format))
                return false;

            if (!SupportedDestinationFormats.Contains(task.To.Format))
                return false;

            if (task.From.Files.Count != 1)
                return false;

            if (task.To.Files?.Count > 0)
                return false;

            if (string.IsNullOrEmpty(task.From.Path))
                return false;

            if (string.IsNullOrEmpty(task.To.Path))
                return false;
            
            // Set task estimation
            if (new [] { StateFormat.dv,StateFormat.dv5p, StateFormat.dvh5, StateFormat.dvhq, StateFormat.dvpp}
                .Contains(task.From.Format))
                task.Estimation = TimeSpan.FromMilliseconds(task.From.Duration / 2.5);
            else
                task.Estimation = TimeSpan.FromMilliseconds(task.From.Duration / 1.3);

            task.Estimation += TimeSpan.FromMinutes(1); //spin up time

            return true;
        }

        protected override void DoWork()
        {
            try
            {
                Guid jobGuid;
                if (string.IsNullOrEmpty(CurrentTask.ForeignKey))
                {
                    var friendlyName = CurrentTask.Arguments.ContainsKey("Name")
                        ? CurrentTask.Arguments["Name"]
                        : CurrentTask.From.Files[0].Value;

                    if (friendlyName.Length >= 31)
                        friendlyName = friendlyName.Substring(0, 30);
          
                    var addLogo =
                        !CurrentTask.From.Flags.HasFlag(StateFlags.Logo) &&
                        CurrentTask.To.Flags.HasFlag(StateFlags.Logo);
                    jobGuid = _wfsService.EnqueueJob(ConvertPresetToWorkflow(CurrentTask.To.Format, CurrentTask.To.AspectRatio, CurrentTask.To.Resolution, addLogo),
                        Path.Combine(CurrentTask.From.Path, CurrentTask.From.Files[0].Value),
                        CurrentTask.To.Path,
                        friendlyName);
                    CurrentTask.State = ExecutionState.Running;
                    CurrentTask.ForeignKey = jobGuid.ToString();
                }
                else
                {
                    jobGuid = Guid.Parse(CurrentTask.ForeignKey);
                }

                var job = _wfsService.GetJob(jobGuid);
                var serviceStatus = job?.Status ?? JobStatus.Fatal;

                switch (serviceStatus)
                {
                    case JobStatus.Pausing: // TODO: Pause not yet supported in executor. treat as running for now.
                    case JobStatus.Paused:
                    case JobStatus.Queued:
                    case JobStatus.Active: { CurrentTask.State = ExecutionState.Running; break; }
                    case JobStatus.Completed: { CurrentTask.State = ExecutionState.Done; break; }
                    //case JobStatus.Pausing:
                    //case JobStatus.Paused: { CurrentTask.State = ExecutionState.Paused; break; }
                    default:
                        throw new PluginException("WFS error",
                            new ArgumentException("Unsupported status", serviceStatus.ToString()));
                }

                if (CurrentTask.State == ExecutionState.Done)
                {
                    var targetFileNames = _wfsService.GetTargetFiles(jobGuid);

                    foreach (var targetFileName in targetFileNames)
                    {
                        var index = targetFileName.LastIndexOf('\\');
                        if (targetFileName.Substring(0, index) != CurrentTask.To.Path)
                            throw new PluginException("Transcoded target file path does not match original order destination path.");

                        //Todo: We need filekind check hier :alarm_clock:
                        CurrentTask.To.Files.Add(targetFileName.Substring(index + 1));
                    }
                    var errors = _wfsService.GetErrors(jobGuid);
                    if(errors.Any())
                        foreach (var error in errors)
                        {
                            Logging.LogWarning($"Wfs task done with error(s): {error}", CurrentTask?.Urn);
                        }
                }
            }
            catch (Exception e)
            {
                CurrentTask.State = ExecutionState.Failed;
                var urn = CurrentTask?.Urn;
                Release(CurrentTask);
                Logging.LogException(e, e.Message, urn);
            }
        }

        private Guid ConvertPresetToWorkflow(StateFormat format, AspectRatio ratio, Resolution resolution, bool burnInLogo)
        {
            return _presetToWorkflow[new Tuple<StateFormat, AspectRatio, Resolution, bool>(format,ratio,resolution, burnInLogo)];
        }

        public override bool Cancel(ExecutionTask task)
        {
            ValidateAndUpdateTask(task);
            var res = _wfsService.CancelJob(new Guid(task.ForeignKey));
            if (res)
                CurrentTask.State = ExecutionState.Canceled;
            return res;
        }

        public override bool CanRetry => true;

        public override bool CanCancel => true;

        protected override int GetWorkerNodeCount()
        {
            try
            {
                return _wfsService.GetWorkingNodes(_machineGroup).Length;
            }
            catch (Exception e)
            {
                Logging.LogException(e, "Failed to get wfs node count", Urn);
                return 0;
            }
        }
    }
}
