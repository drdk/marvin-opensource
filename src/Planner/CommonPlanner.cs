using System.Collections.Generic;
using System.Linq;
using DR.Marvin.Model;

namespace DR.Marvin.Planner
{
    public abstract class CommonPlanner : IPlanner
    {
        protected readonly IPlugin[] Plugins;
        protected readonly IJobRepository JobRepository;
        protected readonly ILogging Logging;

        public CommonPlanner(IEnumerable<IPlugin> plugins, IJobRepository jobRepository, ILogging logging)
        {
            Plugins = plugins.ToArray();
            JobRepository = jobRepository;
            Logging = logging;
        }

        public abstract void Calculate();
    }
}