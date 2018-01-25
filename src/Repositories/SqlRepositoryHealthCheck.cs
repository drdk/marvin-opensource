using System;
using DR.Common.Monitoring.Models;
using DR.Marvin.Model;

namespace DR.Marvin.Repositories
{
    public class SqlRepositoryHealthCheck : CommonHealthCheck
    {
        private readonly IJobRepository _jobRepository;
        public override string Name => "SqlRepository";

        public SqlRepositoryHealthCheck(IJobRepository jobRepository)
        {
            _jobRepository = jobRepository;
        }

        protected override bool? RunTest(ref string message)
        {
            var job = _jobRepository.GetNewest();
            var sqlRepo = _jobRepository.GetEnvironment();
            message = $"Sql repository @{sqlRepo} is accessible. ";
            if (job != null)
                message += $"Newest job added: {job.Urn}";
            else
                message += "No jobs in job table.";
            return true;
        }

        protected override void HandleException(Exception ex, ref string message)
        {
            message =
                $"Unable to comunicate w. SQL database. Please check connection from {Environment.MachineName} to {_jobRepository.GetEnvironment()}. ";
        }
    }
}