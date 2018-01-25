using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using System.Transactions;
using AutoMapper;
using DR.Marvin.Model;
using DR.Marvin.Repositories.AutomapperProfiles;
using DR.Marvin.Repositories.Helpers;

namespace DR.Marvin.Repositories
{
    public class SqlJobRepository : IJobRepository
    {
        private readonly ITimeProvider _timeProvider;
        public SqlJobRepository(ITimeProvider timeProvider)
        {
            if (timeProvider == null) throw new ArgumentException("timeProvider");

            _timeProvider = timeProvider;
        }

        static SqlJobRepository() //init logic
        {
            using (var scope = CreateScope())
            using (var db = new MarvinEntities())
            {
                // UPDATE enum tables if needed
                foreach (var enumType in new []
                {
                    typeof(StateFormat), typeof(ExecutionState),typeof(JobEssenceType), typeof(ExecutionEssenceType),
                    typeof(EssenceFileKind), typeof(AttachmentType), typeof(CommandType)
                })
                {
                    var tableName = enumType.Name;
                    tableName = tableName.First().ToString().ToLower() + tableName.Substring(1);
                    foreach (var id in Enum.GetValues(enumType))
                    {
                        var name = Enum.GetName(enumType, id);
                        db.Database.ExecuteSqlCommand($@"
                            SELECT * FROM {tableName} WHERE id=@id AND name=@name
                            IF @@ROWCOUNT = 0
                            BEGIN
                                UPDATE {tableName} SET name=@name WHERE id=@id
                                IF @@ROWCOUNT = 0
                                INSERT INTO {tableName} ([id] ,[name]) VALUES (@id, @name)
                            END
                            ",
                            new SqlParameter("@id", id),
                            new SqlParameter("@name", name));
                    }
                }
                db.SaveChanges();
                scope.Complete();
            }
        }
        private static TransactionScope CreateScope()
        {
            return new TransactionScope(TransactionScopeOption.Required, new TransactionOptions
            {
                IsolationLevel = IsolationLevel.Snapshot,
                Timeout = TimeSpan.FromSeconds(30)
            });
        }
        void IJobRepository.Add(Job job)
        {
            InternalAdd(job);
        }

        public void Update(Job job)
        {
            job.LastModified = _timeProvider.GetUtcNow();

            using (var scope = CreateScope())
            using (var db = new MarvinEntities())
            {
                var dbJob = db.job.Where(a => a.id == job.Id).IncludeFullJob().FirstOrDefault();
                if (dbJob == null)
                    throw new KeyNotFoundException("Could'nt find job to update - are you missing and ID?");
                lock (JobMappingProfile.Lock)
                {
                    JobMappingProfile.Db = db; // :skull: :arrow_backward: this is a horrible ugly hack. But need for deletions.
                    Mapper.Map(job, dbJob);
                }
                db.SaveChanges();
                scope.Complete();
            }
        }

        public IEnumerable<Job> ActiveJobs()
        {
            using (var _ = CreateScope())
            using (var db = new MarvinEntities())
            {
                var activeJobs =
                    db.job.Where(a =>
                        a.executionPlan.executionState == (int)ExecutionState.Queued ||
                        a.executionPlan.executionState == (int)ExecutionState.Running).IncludeFullJob();
                var res = activeJobs.Select(Mapper.Map<Job>).ToList();
                return res;
            }
        }
        public IEnumerable<Job> WaitingJobs()
        {
            using (var _ = CreateScope())
            using (var db = new MarvinEntities())
            {
                var jobsWithoutPlan = db.job.Where(a => a.executionPlan == null).IncludeFullJob();
                var res = jobsWithoutPlan.Select(Mapper.Map<Job>).ToList();
                return res;
            }
        }

        private static IEnumerable<Job> RecentlyJobs(Expression<Func<job, bool>> predicate, DateTime? after = null)
        {
            using (var _ = CreateScope())
            using (var db = new MarvinEntities())
            {
                var resDb = db.job
                    .Where(predicate)
                    .Where(a => (!after.HasValue || a.modified >= after)).IncludeFullJob();
                var res = resDb.Select(Mapper.Map<Job>).ToList();
                return res;
            }
        }

        public IEnumerable<Job> DoneJobs(DateTime? after = null)
        {
            return RecentlyJobs(j=>j.executionPlan.executionState == (int)ExecutionState.Done, after);
        }

        public Job Get(string urn)
        {
            var id = Guid.Parse(Regex.Match(urn, "(?<=^urn:dr:marvin:job:).*",RegexOptions.IgnoreCase).Value);
            using (CreateScope())
            using (var db = new MarvinEntities())
            {
                var job = db.job.Where(a => a.id == id).IncludeFullJob().FirstOrDefault();
                if (job == null) throw new KeyNotFoundException();
                return Mapper.Map<Job>(job);
            }
        }

        private job InternalAdd(Job job)
        {
            job.LastModified = _timeProvider.GetUtcNow();

            using (var scope = CreateScope())
            using (var db = new MarvinEntities())
            {
                var mappedObject = Mapper.Map<job>(job);
                var result = db.job.Add(mappedObject);
                db.SaveChanges();
                scope.Complete();
                return result;
            }
        }

        public Job Add(Job job)
        {
            return Mapper.Map<Job>(InternalAdd(job));
        }

        public Job GetNewest()
        {
            using (var _ = CreateScope())
            using (var db = new MarvinEntities())
            {
                var job = db.job
                    .OrderByDescending(m => m.issued).Take(1).IncludeFullJob().FirstOrDefault();
                var res = Mapper.Map<Job>(job);
                return res;
            }
        }

        public IEnumerable<Job> FailedJobs(DateTime? after = null)
        {
            return RecentlyJobs(j=>j.executionPlan.executionState == (int)ExecutionState.Failed, after);
        }

        public IEnumerable<Job> CanceledJobs(DateTime? after = null)
        {
            return RecentlyJobs(j=>j.executionPlan.executionState == (int)ExecutionState.Canceled, after);
        }

        public string GetEnvironment()
        {
            using (var db = new MarvinEntities())
            {
                var connection = db.Database.Connection.ConnectionString.Split(';').FirstOrDefault(a => a.ToLowerInvariant().StartsWith("data source"));
                return connection != null ? connection.Split('=').Last() : "Unknown";
            }
        }

        /// <summary>
        /// :warning: Only use for unit testing on local db.
        /// </summary>
        internal void Reset()
        {
            //Resetting jobs - dangerzone! :fire:
            using (var scope = CreateScope())
            using (var db = new MarvinEntities())
            {
                var connectionString = db.Database.Connection.ConnectionString;
                if (!connectionString.Contains("MarvinLocal") || !connectionString.Contains("user id=nunit"))
                    throw new Exception("Reset method is only allow for MarvinLocal and nunit user.");
                db.essence.RemoveRange(db.essence);
                db.job.RemoveRange(db.job);
                db.SaveChanges();
                scope.Complete();
            }
        }
    }
}