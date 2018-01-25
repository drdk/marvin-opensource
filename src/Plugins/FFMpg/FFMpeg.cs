using DR.Marvin.Model;
using DR.Marvin.Plugins.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DR.Marvin.Plugins.FFMpeg
{
    public class FFMpeg : DynamicPlugin
    {
        public static readonly string Type = nameof(FFMpeg).ToLower();
        public static string UrnPrefix => $"{UrnHelper.UrnBase}{PluginBaseUrn}{Type}:";
        public override string PluginType => Type;

        private readonly IFFMpegService _ffmpegService;
        private readonly IDictionary<StateFormat, IList<FFMpegClient.AudioDestinationFormat>> _audioDestinations;

        public FFMpeg(string urn, ITimeProvider timeProvider, ILogging logging, IFFMpegService ffmpegService,
            IAudioPresetProvider presetProvider) : base(urn, Type, timeProvider, logging)
        {
            if (logging == null)
                throw new ArgumentNullException(nameof(logging));

            if (ffmpegService == null)
                throw new ArgumentNullException(nameof(ffmpegService));

            if (presetProvider == null)
                throw new ArgumentNullException(nameof(presetProvider));

            _ffmpegService = ffmpegService;
            _audioDestinations = presetProvider.AsDictionary();
        }

        public static readonly IEnumerable<string> SupportedSubtitleExtensions =
            new string[]
            {
                ".VTT"
            };

        public static readonly IEnumerable<StateFormat> SupportedAudioDestinationFormats =
            new StateFormat[]
            {
                StateFormat.audio_od_standard,
            };

        public static readonly IEnumerable<StateFormat> SupportedAudioSourceFormats =
            new StateFormat[]
            {
                StateFormat.wma,
                StateFormat.mpeg_audio,
                StateFormat.pcm,
                StateFormat.custom,
                // TODO: remove custom format when we know every valid input format we need to support in marvin
            };

        public static readonly IEnumerable<StateFormat> SupportedAudioMuxingVideoDestinationFormats =
            new StateFormat[]
            {
                StateFormat.xd5c,
                StateFormat.dvpp,
                StateFormat.dv5p,
                StateFormat.dvh5,
                StateFormat.dvhq,
                StateFormat.dv,
                StateFormat.avc1,
                StateFormat.custom,
                // TODO: remove custom format when we know every valid input format we need to support in marvin
            };

        public static readonly IEnumerable<StateFormat> SupportedAudioMuxingVideoSourceFormats =
            new StateFormat[]
            {
                StateFormat.xd5c,
                StateFormat.dvpp,
                StateFormat.dv5p,
                StateFormat.dvh5,
                StateFormat.dvhq,
                StateFormat.dv,
                StateFormat.avc1,
                StateFormat.custom,
                // TODO: remove custom format when we know every valid input format we need to support in marvin
            };

        public static readonly IEnumerable<StateFormat> SupportedHardSubtitlesVideoSourceFormats =
            SupportedAudioMuxingVideoDestinationFormats;

        public static readonly IEnumerable<StateFormat> SupportedHardSubtitlesVideoDestinationFormats =
          new StateFormat[]
          {
                StateFormat.xd5c
          };

        private enum TaskType
        {
            Invalid = 0,
            Muxing,
            HardSubtitles,
            Transcoding, 
        }

        private static bool HasStateChange(ExecutionTask task, StateFlags flag)
        {
            return !task.From.Flags.HasFlag(flag) && task.To.Flags.HasFlag(flag);
        }

        private static TaskType DetermineTaskType(ExecutionTask task)
        {
            bool stichIntro, stichOutro;
            return DetermineTaskType(task, out stichIntro, out stichOutro);
        }
        private static TaskType DetermineTaskType(ExecutionTask task, out bool stichIntro, out bool stichOutro)
        {
            stichIntro = HasStateChange(task, StateFlags.Intro);
            stichOutro = HasStateChange(task, StateFlags.Outro);
            var muxingTask = HasStateChange(task, StateFlags.AlternativeAudio);
            var hardsubTask = HasStateChange(task, StateFlags.HardSubtitles);

            return
                 muxingTask && hardsubTask || // can't mux and hard sub in one step
                (stichIntro || stichOutro) && (muxingTask || hardsubTask) ?  //intro / outro not support for muxing or hard sub job.
                 TaskType.Invalid : muxingTask ? TaskType.Muxing : hardsubTask ? TaskType.HardSubtitles : TaskType.Transcoding;
        }

        public override bool CheckAndEstimate(ExecutionTask task)
        {
            InternalTaskCheck(task);
            bool stichIntro, stichOutro;
            var taskType = DetermineTaskType(task, out stichIntro, out stichOutro);
          
            switch (taskType)
            {
                case TaskType.Muxing:
                    #region muxing task checks ...
                    if (!SupportedAudioMuxingVideoSourceFormats.Contains(task.From.Format))
                        return false;

                    if (!SupportedAudioMuxingVideoDestinationFormats.Contains(task.To.Format))
                        return false;

                    // ReSharper disable once SwitchStatementMissingSomeCases
                    switch (task.From.Format)
                    {
                        case StateFormat.dv: //set to the same as xd5c.
                        case StateFormat.dv5p: //set to the same as xd5c.
                        case StateFormat.dvh5: //set to the same as xd5c.
                        case StateFormat.dvhq: //set to the same as xd5c.
                        case StateFormat.dvpp: //set to the same as xd5c.
                        case StateFormat.avc1:
                        //set to the same as xd5c. We didnt have any test data and supposedly they are rare -drexkrha
                        case StateFormat.xd5c:
                            task.Estimation = TimeSpan.FromMilliseconds(task.From.Duration / 4.5);
                            break;
                        case StateFormat.custom:
                            task.Estimation = TimeSpan.FromMilliseconds(task.From.Duration / 4.5);
                            //set to the same as xd5c.
                            break;
                        default:
                            throw new PluginException("Unsupported format for current task.");
                            // this should never been thrown, should have been caught above this.
                    }
                    #endregion
                    break;

                case TaskType.HardSubtitles:
                    #region hard subtitles task checks...
                    var subAttach = task.From.Attachments.FirstOrDefault(a => a.Type == AttachmentType.Subtitle);

                    if (subAttach == null || 
                        !SupportedSubtitleExtensions.Any(e=>subAttach.Path.EndsWith(e,StringComparison.InvariantCultureIgnoreCase)))
                        return false;

                    if (!SupportedHardSubtitlesVideoSourceFormats.Contains(task.From.Format))
                        return false;

                    if (!SupportedHardSubtitlesVideoDestinationFormats.Contains(task.To.Format))
                        return false;

                    task.Estimation = TimeSpan.FromMilliseconds(task.From.Duration);
                    #endregion
                    break;
                
                case TaskType.Transcoding:
                    #region transcoding task checks...
                    if (stichIntro)
                    {
                        var attachment = task.From.Attachments.FirstOrDefault(a => a.Type == AttachmentType.Intro);
                        if (attachment == null)
                            return false;
                    }

                    if (stichOutro)
                    {
                        var attachment = task.From.Attachments.FirstOrDefault(a => a.Type == AttachmentType.Outro);
                        if (attachment == null)
                            return false;
                    }

                    if (!SupportedAudioSourceFormats.Contains(task.From.Format))
                        return false;

                    if (!SupportedAudioDestinationFormats.Contains(task.To.Format))
                        return false;

                    if (task.To.Files?.Count > 0)
                        return false;

                    // ReSharper disable once SwitchStatementMissingSomeCases
                    switch (task.From.Format)
                    {
                        case StateFormat.wma:
                        //set to the same as mpeg_audio. We didnt have any test data and supposedly they never use wma -drexkrha
                        case StateFormat.pcm:
                        case StateFormat.mpeg_audio:
                            task.Estimation = TimeSpan.FromMilliseconds(task.From.Duration / 5.5);
                            break;
                        case StateFormat.custom:
                            task.Estimation = TimeSpan.FromMilliseconds(task.From.Duration / 5.5);
                            //set to the same as mpeg_audio
                            break;
                        default:
                            throw new PluginException("Unsupported format for current task.");
                            // this should never been thrown, should have been caught above this.
                    }
                    #endregion
                    break;

                case TaskType.Invalid:
                default:
                    return false;
            }

            if (task.From.Files.Count != 1)
                return false;
            
            if (string.IsNullOrEmpty(task.From.Path))
                return false;

            if (string.IsNullOrEmpty(task.To.Path))
                return false;


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
                    var fileFullPath = System.IO.Path.Combine(CurrentTask.From.Path, CurrentTask.From.Files[0].Value);
                    var taskType = DetermineTaskType(CurrentTask);
                    switch (taskType)
                    {
                        case TaskType.Muxing:
                            var alternativeAudioFileFullPath =
                                CurrentTask.From.Attachments.First(a => a.Type == AttachmentType.Audio).Path;
                            jobGuid = _ffmpegService.PostMuxAudioJob(
                                alternativeAudioFileFullPath, fileFullPath, CurrentTask.To.Path);
                            break;
                        case TaskType.HardSubtitles:
                            var subtitlesPath = CurrentTask.From.Attachments.First(a => a.Type == AttachmentType.Subtitle).Path;
                            jobGuid = _ffmpegService.PostHardSubtitlesJob(
                                subtitlesPath, fileFullPath, CurrentTask.To.Path);
                            break;
                        case TaskType.Transcoding:
                            var destinationFilenamePrefix =
                                System.IO.Path.GetFileNameWithoutExtension(CurrentTask.From.Files[0].Value);
                            var introPath =
                                CurrentTask.From.Attachments?.FirstOrDefault(a => a.Type == AttachmentType.Intro)?.Path;
                            var outroPath =
                                CurrentTask.From.Attachments?.FirstOrDefault(a => a.Type == AttachmentType.Outro)?.Path;
                            jobGuid = _ffmpegService.PostAudioJob(fileFullPath, CurrentTask.To.Path,
                                destinationFilenamePrefix, GetTargetFormatsFromDestinationFormat(CurrentTask.To.Format),
                                introPath, outroPath);
                            break;
                        case TaskType.Invalid:
                        default:
                            throw new PluginException($"Unsupported task type {taskType}");
                    }
                    CurrentTask.State = ExecutionState.Running;
                    CurrentTask.ForeignKey = jobGuid.ToString();
                }
                else
                {
                    jobGuid = Guid.Parse(CurrentTask.ForeignKey);
                }
                FFMpegClient.FfmpegJobModelState serviceStatus;
                FFMpegClient.FfmpegJobModel ffmpegJob;
                try
                {
                    ffmpegJob = _ffmpegService.GetAudioJob(jobGuid);
                    serviceStatus = ffmpegJob?.State ?? FFMpegClient.FfmpegJobModelState.Unknown;
                }
                catch (TaskCanceledException) // time out 
                {
                    Logging.LogWarning($"Got time out on ffmpeg status api on FFmpeg job {CurrentTask.ForeignKey}", CurrentTask.Urn);
                    return;
                }
                switch (serviceStatus)
                {
                    case FFMpegClient.FfmpegJobModelState.Unknown:
                    case FFMpegClient.FfmpegJobModelState.Queued:
                    case FFMpegClient.FfmpegJobModelState.Paused:
                    case FFMpegClient.FfmpegJobModelState.InProgress:
                        CurrentTask.State = ExecutionState.Running;
                        break;
                    case FFMpegClient.FfmpegJobModelState.Done:
                        CurrentTask.State = ExecutionState.Done;
                        break;
                    case FFMpegClient.FfmpegJobModelState.Failed:
                        throw new PluginException("FFMpeg error",
                            new ArgumentException("FFMpeg service failure", serviceStatus.ToString()));
                    case FFMpegClient.FfmpegJobModelState.Canceled:
                    default:
                        throw new PluginException("FFMpeg error",
                            new ArgumentException("Unsupported status", serviceStatus.ToString()));
                }

                if (CurrentTask.State == ExecutionState.Done && ffmpegJob != null)
                {
                    if (ffmpegJob.Tasks == null || ffmpegJob.Tasks.Count == 0)
                        throw new PluginException("Transcoded target file paths do not exist.");

                    foreach (var task in ffmpegJob.Tasks)
                    {
                        var targetFileName = task.DestinationFilename;
                        var index = targetFileName.LastIndexOf('\\');
                        if (targetFileName.Substring(0, index) != CurrentTask.To.Path)
                            throw new PluginException(
                                "Transcoded target file path does not match original order destination path.");

                        //Todo: We need filekind check here :alarm_clock:
                        CurrentTask.To.Files.Add(targetFileName.Substring(index + 1));
                    }
                }
            }
            catch (Exception e)
            {
                CurrentTask.State = ExecutionState.Failed;
                Release(CurrentTask);
                Logging.LogException(e, e.Message);
            }
        }

        private IList<FFMpegClient.AudioDestinationFormat> GetTargetFormatsFromDestinationFormat(StateFormat format)
        {
            if (!_audioDestinations.ContainsKey(format))
                throw new Exception("Usupported target format: " + format);

            return _audioDestinations[format];
        }

        protected override int GetWorkerNodeCount()
        {
            try
            {
                return _ffmpegService.GetNumberOfSupportedPlugins();
            }
            catch (Exception e)
            {
                Logging.LogException(e,"Failed to get ffmpeg node count", CurrentTask?.Urn);
                return 0;
            }
        }

        public override bool Cancel(ExecutionTask task)
        {
            ValidateAndUpdateTask(task);
            var res = _ffmpegService.CancelJob(new Guid(task.ForeignKey));
            if (res)
                CurrentTask.State = ExecutionState.Canceled;
            return res;
        }

        public override bool CanCancel => true;

        public override bool CanRetry => true;
    }
}