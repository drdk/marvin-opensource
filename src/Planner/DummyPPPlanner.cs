using System.Collections.Generic;
using System.Linq;
using DR.Marvin.Model;
using DR.Marvin.Plugins.Dummy;

namespace DR.Marvin.Planner
{
    public class DummyPPPlanner : CommonPlanner
    {
        public DummyPPPlanner(IEnumerable<IPlugin> plugins, IJobRepository jobRepository, ILogging logging) : 
            base(plugins, jobRepository, logging) { }
        public override void Calculate()
        {
            // TODO: cancel, pause, retry , resume...

            var currentPlans = JobRepository.ActiveJobs(); //TODO: check already running task before planning next. 
            var waiting = JobRepository.WaitingJobs().OrderBy(j=>j.DueDate).GetEnumerator();
            if (!waiting.MoveNext()) return;
            foreach (var freePlugin in Plugins.Where(p=> ! p.Busy && p.PluginType == DummyLogoPP.Type))
            {
                var tcPlugin = Plugins.First(p => p.PluginType  == DummyTCOnly.Type);
                var et1 = new ExecutionTask
                {
                    From = new Essence(waiting.Current.Source),
                    To = new Essence(waiting.Current.Source) { Flags = waiting.Current.Destination.Flags, Files = null },
                    PluginUrn = freePlugin.Urn
                };
                var et2 = new ExecutionTask
                {
                    From = new Essence(et1.To),
                    To = new Essence(waiting.Current.Destination),
                    PluginUrn = tcPlugin.Urn
                };
                if (!freePlugin.CheckAndEstimate(et1)) continue;
                if (!tcPlugin.CheckAndEstimate(et2)) continue;
                var ep = new ExecutionPlan { Tasks = new List<ExecutionTask> {et1, et2}};
                waiting.Current.Plan = ep;
                JobRepository.Update(waiting.Current);
                if (!waiting.MoveNext())
                    break;
            }
        }
    }
}
