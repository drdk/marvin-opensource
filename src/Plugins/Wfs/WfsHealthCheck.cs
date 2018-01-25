using System;
using System.Collections.Generic;
using System.Linq;
using DR.Common.Monitoring.Models;
using DR.Marvin.Model;
using DR.WfsService.Contract;
using DR.WfsService.JMServices;

namespace DR.Marvin.Plugins.Wfs
{
    public class WfsHealthCheck : CommonHealthCheck
    {
        private readonly IWfsService _wfsService;
        private readonly string _machineGroup;
        private readonly int _pluginCount;
        private readonly ITimeProvider _timeProvider;

        public WfsHealthCheck(IWfsService wfsService, IPresetProvider pluginCfg, IEnumerable<IPlugin> plugins, ITimeProvider timeProvider )
        {
            _wfsService = wfsService;
            _machineGroup = pluginCfg.MachineGroup;
            _pluginCount = plugins.Count(p => p.PluginType == "wfs");
            _timeProvider = timeProvider;
        }
        public override string Name => "WfsService";
        protected override bool? RunTest(ref string message)
        {
            //controller 
            var controllerStatus = _wfsService.GetJobManagerStatus();
            message = $"Wfs Job manager status : {controllerStatus} {Environment.NewLine} ";
            var res = controllerStatus == JobManagerStatus.Active;

            //Working machines 
            var nodes = _wfsService.GetWorkingNodes(_machineGroup);
            message += $"Registered plugins: {_pluginCount} Available nodes: {nodes.Length} {Environment.NewLine} ";
            if (nodes.Any())
            {
                message += nodes.Aggregate((current, next) => $"{current}, {next}");
                message += Environment.NewLine;
            }

            //Queued jobs 
            var now = _timeProvider.GetUtcNow();
            var jobs = _wfsService.GetJobsByStatus(0, 50, now.AddDays(-1), JobStatus.Queued);
            var queuedLongerThen5Min =
                jobs.Any(j => now.Subtract(new DateTime(j.Created, DateTimeKind.Local).ToUniversalTime()).TotalMinutes > 5);

            if (queuedLongerThen5Min)
            {
               message += $"It should be possible to run {nodes.Length} simultaneous on wfs. {Environment.NewLine}" +
                          $"One or more of the wfs are not accepting job. Go to WFS Manager -> Machines. " +
                          $"If none are red, try restrting the wfs controller node. " +
                          $"If this doen't solve the problem, try removing the failing nodes from '{_machineGroup}' {Environment.NewLine}";
            }

            return res && (_pluginCount <= nodes.Length) && !queuedLongerThen5Min;
        }

        protected override void HandleException(Exception ex, ref string message)
        {
            message =
                $"Unable to comunicate w. Wfs controller node. Please check connection from {Environment.MachineName} to {_wfsService.GetEnvironment()}. ";
        }
    }
}
