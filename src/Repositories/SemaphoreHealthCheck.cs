using DR.Common.Monitoring.Models;
using DR.Marvin.Model;
using System;
using System.CodeDom.Compiler;

namespace DR.Marvin.Repositories
{
    public class SemaphoreHealthCheck : CommonHealthCheck
    {
        public override string Name => "Semaphore";
        private readonly ISemaphoreRepository _repo;
        private readonly ITimeProvider _timeProvider;

        public SemaphoreHealthCheck(ITimeProvider timeProvider, ISemaphoreRepository repo)
        {
            _timeProvider = timeProvider;
            _repo = repo;
        }

        protected override bool? RunTest(ref string message)
        {
            Semaphore semaphore;
            try
            {
                semaphore = _repo.Probe(nameof(Executor));
            }
            catch
            {
                message = $"SQL error, check database. See also {nameof(SqlRepositoryHealthCheck)}.";
                return false;
            }

            DateTime cutOffPoint = _timeProvider.GetUtcNow().Add(new TimeSpan(0, -15, 0));

            if (semaphore.HeartBeat < cutOffPoint)
            {
                message = $"The hearthbeat is more than 15 minutes old and has therefore experired. Current owner id is {semaphore.CurrentOwnerId}. ";
                return false;
            }
            else
            {
                message = $"The hearthbeat has not experired.";
                return true;
            }
        }
    }
}
