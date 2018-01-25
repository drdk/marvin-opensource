using DR.FFMpegClient;
using System;
using System.Collections.Generic;

namespace DR.Marvin.Plugins.FFMpeg
{
    public interface IFFMpegService
    {
        Guid PostAudioJob(string inputFileFullPath, string outputFolder, string destinationFilenamePrefix, IList<AudioDestinationFormat> targets, string introPath = null, string outroPath = null);
        Guid PostMuxAudioJob(string audioFileFullPath, string videoFileFullPath, string outputFolder);
        Guid PostHardSubtitlesJob(string subtitlesFullPath, string videoFileFullPath, string outputFolder);
        bool CancelJob(Guid id);
        FfmpegJobModel GetAudioJob(Guid id);
        ServiceStatus GetServiceHealthStatus();
        string[] GetWorkingNodes();
        string GetEnvironment();
        int GetNumberOfSupportedPlugins();
    }
}
