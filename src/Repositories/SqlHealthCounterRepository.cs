using System;
using System.Collections.Generic;
using System.Linq;
using System.Transactions;
using AutoMapper;
using DR.Marvin.Model;

namespace DR.Marvin.Repositories
{
    public class SqlHealthCounterRepository : IHealthCounterRepository
    {
        private readonly ITimeProvider _timeProvider;
        public TimeSpan MaxAge { get; }

        public SqlHealthCounterRepository(ITimeProvider timeProvider, TimeSpan maxAge)
        {
            if (timeProvider == null) throw new ArgumentException("timeProvider");

            _timeProvider = timeProvider;
            MaxAge = maxAge;
        }

        public void Increment(string id, string message)
        {
            using (var scope = CreateScope())
            using (var db = new MarvinEntities())
            {
                var entry = db.healthCounter.FirstOrDefault(hc => hc.id == id);
                var now = _timeProvider.GetUtcNow();
                if (entry == null)
                {
                    entry = new healthCounter
                    {
                        id = id,
                        message = message,
                        count = 1,
                        timestamp = now
                    };
                    db.healthCounter.Add(entry);
                }
                else
                {
                    entry.message = message;
                    if (entry.timestamp + MaxAge > now)
                        entry.count++;
                    else
                        entry.count = 1;
                    entry.timestamp = now;
                }
                db.SaveChanges();
                scope.Complete();
            }
        }

        public IEnumerable<HealthCounter> ProbeAndPrune()
        {
            using (var scope = CreateScope())
            using (var db = new MarvinEntities())
            {
                var limit = _timeProvider.GetUtcNow() - MaxAge;
                var old = db.healthCounter.Where(hc => hc.timestamp < limit);
                db.healthCounter.RemoveRange(old);
                db.SaveChanges();
                var res = db.healthCounter.Select(Mapper.Map<HealthCounter>).ToList();
                db.SaveChanges();
                scope.Complete();
                return res;
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
                db.healthCounter.RemoveRange(db.healthCounter);
                db.SaveChanges();
                scope.Complete();
            }
        }
    }
}
