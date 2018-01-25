using DR.Common.Monitoring.Models;
using DR.Marvin.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DR.Marvin.Repositories
{
    public class JobsHealthCheck : CommonHealthCheck
    {
        private readonly IJobRepository _jobRepository;
        private readonly TimeSpan _checkWindow;
        private readonly ITimeProvider _timeProvider;
        private readonly int _minimumFailCount;
        private readonly double _failureRatio;
        public override string Name => "Jobs";
        public JobsHealthCheck(IJobRepository jobRepository, ITimeProvider timeProvider, int minutes, int minimumFailCount, double failureRatio)
        {
            _jobRepository = jobRepository;
            _checkWindow = TimeSpan.FromMinutes(minutes);
            _timeProvider = timeProvider;
            _minimumFailCount = minimumFailCount;
            _failureRatio = failureRatio;
        }
        protected override bool? RunTest(ref string message)
        {
            IList<Job> failedJobs;
            IList<Job> doneJobs;
            var after = _timeProvider.GetUtcNow() - _checkWindow;
            try
            {
                failedJobs = _jobRepository.FailedJobs(after).ToList();
                doneJobs = _jobRepository.DoneJobs(after).ToList();
            }
            catch
            {
                message = $"SQL error, check database. See also {nameof(SqlRepositoryHealthCheck)}.";
                return false;
            }
            var total = doneJobs.Count + failedJobs.Count;

            var failedJobsPercentage = total == 0 ? 0.0 : failedJobs.Count / (double) total ;

            message = $"{doneJobs.Count} were succefully completed during the last {_checkWindow.TotalMinutes} minutes and ";
            if (failedJobs.Any())
            {
                message += $"{failedJobs.Count} job(s) failed, ";
                var alarmTriggered = failedJobs.Count >= _minimumFailCount && failedJobsPercentage > _failureRatio;
                message += $"alarm {(alarmTriggered ? "" : "not")} triggred. List of failed jobs : ";
                foreach (var job in failedJobs)
                {
                    message += $"  {job.Urn}, ";
                }

                return !alarmTriggered;
            }
            message += "no jobs failed.";
            
            return true;
        }
    }
}
