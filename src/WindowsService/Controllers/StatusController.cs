using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;
using AutoMapper;
using DR.Marvin.Model;
using DR.Marvin.WindowsService.Model;
//using Swashbuckle.Swagger.Annotations;
using WebApi.OutputCache.V2;

namespace DR.Marvin.WindowsService.Controllers
{
    /// <summary>
    /// Job status controller
    /// </summary>
    public class StatusController : ApiController
    {
        private readonly IJobRepository _jobRepository;
        private readonly IList<IPlugin> _plugins;
        private readonly ITimeProvider _timeProvider;
        /// <summary>
        /// Constructor
        /// </summary>
        public StatusController(IJobRepository jobRepository, IEnumerable<IPlugin> plugins, ITimeProvider timeProvider)
        {
            _jobRepository = jobRepository;
            _plugins = plugins.ToList();
            _timeProvider = timeProvider;
        }

        /// <summary>
        /// Request status on a job
        /// </summary>
        /// <param name="jobUrn">Urn of the job you want status on</param>
        /// <returns>JobStatus message</returns>
        [CacheOutput(ServerTimeSpan = 10, ClientTimeSpan = 10)]
        public JobStatus Get(string jobUrn)
        {
            if (string.IsNullOrEmpty(jobUrn))
                throw new ArgumentNullException(nameof(jobUrn));
            if (!jobUrn.ValidateUrn("job:"))
                throw new ArgumentException("invalid urn",nameof(jobUrn));

            var jobStatus = _jobRepository.Get(jobUrn);

            return Mapper.Map<JobStatus>(jobStatus);
        }

        /// <summary>
        /// Job status, used in the marvin dashboard.
        /// </summary>
        /// <param name="recentlyFilter">Optional filter, default to UtcNow() - 1 hour</param>

        [CacheOutput(ServerTimeSpan = 10, ClientTimeSpan = 10)]
        public DashboardInfo GetDashboardInfo(DateTime? recentlyFilter = null)
        {
            if (!recentlyFilter.HasValue)
                recentlyFilter = _timeProvider.GetUtcNow().AddHours(-1);
            var res = new DashboardInfo
            {
                WaitingJobs = _jobRepository.WaitingJobs().Select(Mapper.Map<DashboardJob>),
                ActiveJobs = _jobRepository.ActiveJobs().Select(Mapper.Map<DashboardJob>),
                RecentlyDoneJobs = _jobRepository.DoneJobs(recentlyFilter).Select(Mapper.Map<DashboardJob>),
                RecentlyFailedJobs = _jobRepository.FailedJobs(recentlyFilter).Select(Mapper.Map<DashboardJob>),
                RecentlyCanceledJobs = _jobRepository.CanceledJobs(recentlyFilter).Select(Mapper.Map<DashboardJob>),
                Plugins = _plugins.Select(Mapper.Map<DashbaordPlugin>)
            };
            return res;
        }
    }
}
