using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using DR.Marvin.Model;

namespace DR.Marvin.Simulator
{
    public class InMemoryJobRepository : IJobRepository
    {
        private readonly ITimeProvider _timeProvider;
        private readonly IDictionary<string, Job> _dict = new ConcurrentDictionary<string, Job>();

        public InMemoryJobRepository(ITimeProvider timeProvider)
        {
            _timeProvider = timeProvider;
        }

        public void Update(Job job)
        {
            Add(job);
        }

        public IEnumerable<Job> ActiveJobs()
        {
            return
                _dict.Values.Where(
                    job => job.Plan?.GetState() == ExecutionState.Queued || job.Plan?.GetState() == ExecutionState.Running)
                    .Select(Mapper.Map<Job>);
        }

        public IEnumerable<Job> WaitingJobs()
        {
            return
               _dict.Values.Where(job => job.Plan == null).Select(Mapper.Map<Job>);
        }

        public IEnumerable<Job> DoneJobs(DateTime? after = null)
        {
            return
              _dict.Values.Where(job => job.Plan?.GetState() == ExecutionState.Done &&
                    (!after.HasValue || job.LastModified >= after)).Select(Mapper.Map<Job>);
        }

        public void Add(Job job)
        {
            var dbJob = Mapper.Map<Job>(job);
            dbJob.LastModified = _timeProvider.GetUtcNow();
            _dict[job.Urn] = Mapper.Map<Job>(dbJob);

        }

        public Job Get(string urn)
        {
            return Mapper.Map<Job>(_dict[urn]);
        }
        
        public void Reset()
        {
            _dict.Clear();
        }

        public Job GetNewest()
        {
            var job = _dict.Values.OrderByDescending(m => m.Issued).FirstOrDefault();
            return Mapper.Map<Job>(job);
        }

        public IEnumerable<Job> FailedJobs(DateTime? after = null)
        {
            return
              _dict.Values
              .Where(job => job.Plan?.GetState() == ExecutionState.Failed &&
                    (!after.HasValue || job.LastModified >= after))
                    .Select(Mapper.Map<Job>);
        }

        public IEnumerable<Job> CanceledJobs(DateTime? after = null)
        {
            return
              _dict.Values
              .Where(job => job.Plan?.GetState() == ExecutionState.Canceled &&
                    (!after.HasValue || job.LastModified >= after))
                    .Select(Mapper.Map<Job>);
        }

        public string GetEnvironment()
        {
            return "INMEMORY";
        }
    }
}
