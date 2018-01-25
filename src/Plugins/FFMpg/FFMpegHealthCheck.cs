using DR.Common.Monitoring.Models;
using DR.Marvin.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DR.Marvin.Plugins.FFMpeg
{
    public class FFMpegHealthCheck : CommonHealthCheck
    {
        private readonly IFFMpegService _ffmpegService;
        private readonly int _pluginCount;

        public FFMpegHealthCheck(IFFMpegService ffmpegService, IEnumerable<IPlugin> plugins)
        {
            _ffmpegService = ffmpegService;
            _pluginCount = plugins.Count(p => p.PluginType == "ffmpeg");
        }
        public override string Name => "FFMPegService";
        protected override bool? RunTest(ref string message)
        {
            var allOK = true;
            var serviceStatus = _ffmpegService.GetServiceHealthStatus();
            message = $"FFMpeg api : OK {Environment.NewLine} ";
            foreach (var ws in serviceStatus.Workers)
            {
                if (ws.Status != FFMpegClient.WorkerStatusStatus.OK)
                    allOK = false;

                message += $"Worker {ws.WorkerName} status: {ws.Status} {Environment.NewLine} ";
            }

            var nodes = _ffmpegService.GetWorkingNodes();
            var supportedPlugins = _ffmpegService.GetNumberOfSupportedPlugins();
            message += $"Registered plugins: {_pluginCount} Available nodes: {nodes.Length}  # Supported plugins : {supportedPlugins} {Environment.NewLine} ";
            if (nodes.Any())
                message += nodes.Aggregate((current, next) => $"{current}, {next}");
            allOK = allOK && (_pluginCount <= supportedPlugins);

            return allOK;
        }

        protected override void HandleException(Exception ex, ref string message)
        {
            message =
                $"Unable to comunicate w. ffmpeg farm controller node. Please check connection from {Environment.MachineName} to {_ffmpegService.GetEnvironment()}. ";
        }
    }
}
