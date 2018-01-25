using DR.FFMpegClient;
using DR.Marvin.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Command = DR.FFMpegClient.Command;

namespace DR.Marvin.Plugins.FFMpeg
{
    public class FFMpegService : IFFMpegService
    {
        private readonly AudioJobClient _audioClient;
        private readonly MuxJobClient _audioMuxClient;
        private readonly HardSubtitlesJobClient _hardSubtitlesClient;
        private readonly StatusClient _statusClient;
        private readonly HealthCheckClient _healthClient;
        private readonly JobClient _jobClient;
        private readonly HttpClient _httpClient;
        private readonly string _serviceUri;
        private readonly int _threadsPerMachine;
        private readonly int _tasksPerAudioJob;
        public int RetryCount { get; set; }
        public int RetrySleepMs { get; set; }


        private TOut Retry<TOut>(Func<TOut> method)
        {
            var tries = 0;
            while (true)
                try
                {
                    return method.Invoke();
                }
                catch
                {
                    if (++tries >= RetryCount)
                        throw;
                    Thread.Sleep(RetrySleepMs);
                }
        }


        public FFMpegService(string uri, int threadsPerMachine, IAudioPresetProvider audioPresetProvider, ITimeProvider timeProvider)
        {
            _httpClient = new HttpClient {Timeout = TimeSpan.FromSeconds(60)};
            _serviceUri = uri;
            _audioClient = new AudioJobClient(_httpClient) { BaseUrl = _serviceUri };
            _audioMuxClient = new MuxJobClient(_httpClient) { BaseUrl = _serviceUri };
            _hardSubtitlesClient = new HardSubtitlesJobClient(_httpClient) { BaseUrl = _serviceUri };
            _statusClient = new StatusClient(_httpClient) { BaseUrl = _serviceUri };
            _healthClient = new HealthCheckClient(_httpClient) { BaseUrl = _serviceUri };
            _jobClient = new JobClient(_httpClient) { BaseUrl = _serviceUri };
            _timeProvider = timeProvider;
            _threadsPerMachine = threadsPerMachine;
            _tasksPerAudioJob = audioPresetProvider.AsDictionary()[StateFormat.audio_od_standard].Count;
            RetryCount = 3;
            RetrySleepMs = 10 * 1000;
        }

        ~FFMpegService()
        {
            _httpClient?.Dispose();
        }

        public Guid PostAudioJob(string inputFileFullPath, string outputFolder, string destinationFilenamePrefix, IList<AudioDestinationFormat> targets, string introPath = null, string outroPath = null)
        {
            var sourceFileNames = new ObservableCollection<string>();
            if (introPath != null) sourceFileNames.Add(introPath);
            sourceFileNames.Add(inputFileFullPath);
            if (outroPath != null) sourceFileNames.Add(outroPath);
            return Retry(() =>
            {
                var task = _audioClient.CreateNewAsync(new AudioJobRequestModel() {
                    Targets = new ObservableCollection<AudioDestinationFormat>(targets),
                    Needed = DateTime.UtcNow,
                    OutputFolder = outputFolder,
                    Inpoint = "0",
                    SourceFilenames = sourceFileNames,
                    DestinationFilenamePrefix = destinationFilenamePrefix
                });
          
                Task.WaitAll(task);
                return task.Result;
            });
        }

        public Guid PostMuxAudioJob(string audioFileFullPath, string videoFileFullPath , string outputFolder)
        {
            return Retry(() =>
            {
                var task = _audioMuxClient.CreateNewAsync(new MuxJobRequestModel()
                {
                    DestinationFilename = System.IO.Path.GetFileName(videoFileFullPath), 
                    Needed = DateTime.UtcNow,
                    OutputFolder = outputFolder,
                    Inpoint = "0",
                    AudioSourceFilename = audioFileFullPath,
                    VideoSourceFilename = videoFileFullPath
                });

                Task.WaitAll(task);
                return task.Result;
            });
        }

        public bool CancelJob(Guid id)
        {
            return Retry(() =>
            {
                var statusTask = _jobClient.PatchJobAsync(id, Command.Cancel);
                Task.WaitAll(statusTask);
                bool result = statusTask.Result;
                return result;
            });
        }

        public FfmpegJobModel GetAudioJob(Guid id)
        {
            return Retry(() =>
            {
                var statusTask = _statusClient.GetAsync(id);
                Task.WaitAll(statusTask);
                FfmpegJobModel result = statusTask.Result;
                return result;
            });
        }

        private static DateTime _healthStatusRefresh = DateTime.MinValue;
        private static readonly TimeSpan HealthStatusTimeOut = TimeSpan.FromSeconds(10);
        private static ServiceStatus _cachedServiceStatus = null;
        private static readonly object ServiceStatusLock = new object();
        private readonly ITimeProvider _timeProvider;

        public ServiceStatus GetServiceHealthStatus()
        {
            lock (ServiceStatusLock)
            {
                var now = _timeProvider.GetUtcNow();
                if (now - _healthStatusRefresh > HealthStatusTimeOut)
                {
                    _cachedServiceStatus = Retry(() =>
                        {
                            var statusTask = _healthClient.GetAsync();
                            Task.WaitAll(statusTask);
                            return statusTask.Result;
                        });
                    _healthStatusRefresh = now;
                }
                return _cachedServiceStatus;
            }
        }

        public string GetEnvironment()
        {
            return _serviceUri;
        }

        public string[] GetWorkingNodes()
        {
            var result = GetServiceHealthStatus().Workers;
            return result.Where(p => p.Status == WorkerStatusStatus.OK).Select(p => p.WorkerName).ToArray();
        }

        public int GetNumberOfSupportedPlugins()
        {
            var machineCount = GetWorkingNodes().Length;
            return (int) Math.Floor((machineCount * _threadsPerMachine) / (float) _tasksPerAudioJob);
        }

        public Guid PostHardSubtitlesJob(string subtitlesFullPath, string videoFileFullPath, string outputFolder)
        {
            return Retry(() =>
            {
                var task = _hardSubtitlesClient.CreateNewAsync(new HardSubtitlesJobRequestModel()
                {
                    DestinationFilename = System.IO.Path.GetFileName(videoFileFullPath),
                    Needed = DateTime.UtcNow,
                    OutputFolder = outputFolder,
                    Inpoint = "0",
                    SubtitlesFilename = subtitlesFullPath,
                    VideoSourceFilename = videoFileFullPath
                });

                Task.WaitAll(task);
                return task.Result;
            });
        }
    }
}
