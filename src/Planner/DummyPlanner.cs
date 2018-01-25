using System.Collections.Generic;
using System.Linq;
using DR.Marvin.Model;

namespace DR.Marvin.Planner
{
    public class DummyPlanner : CommonPlanner
    {
        public DummyPlanner(IEnumerable<IPlugin> plugins, IJobRepository jobRepository, ILogging logging) : 
            base(plugins, jobRepository, logging) { }
        public override void Calculate()
        {
            // TODO: cancel, pause, retry , resume...

            var currentPlans = JobRepository.ActiveJobs(); //TODO: check already running task before planning next. 
            var waiting = JobRepository.WaitingJobs().OrderBy(j=>j.DueDate).GetEnumerator();
            if (!waiting.MoveNext()) return;
            foreach (var freePlugin in Plugins.Where(p=> ! p.Busy))
            {
                var et = new ExecutionTask
                {
                    From = new Essence(waiting.Current.Source),
                    To = new Essence(waiting.Current.Destination),
                    PluginUrn = freePlugin.Urn
                };
                if (!freePlugin.CheckAndEstimate(et)) continue;
                var ep = new ExecutionPlan { Tasks = new List<ExecutionTask> {et}};
                waiting.Current.Plan = ep;
                JobRepository.Update(waiting.Current);
                if (!waiting.MoveNext())
                    break;
            }
        }
    }
}
