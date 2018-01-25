using System;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Transactions;
using DR.Marvin.Model;
using AutoMapper;

namespace DR.Marvin.Repositories
{
    public class SqlSemaphoreRepository : ISemaphoreRepository
    {
        private readonly ITimeProvider _timeProvider;
        private static TransactionScope CreateScope()
        {
            return new TransactionScope(TransactionScopeOption.Required, new TransactionOptions
            {
                IsolationLevel = IsolationLevel.Snapshot,
                Timeout = TimeSpan.FromSeconds(30)
            });
        }
        public SqlSemaphoreRepository(ITimeProvider timeProvider)
        {
            _timeProvider = timeProvider;
            MaxAge = TimeSpan.FromMinutes(5);
        }

        public TimeSpan MaxAge { get; set; }

        public bool Get(string semaphoreId, string callerId, out string currentOwnerId)
        {
            using (var scope = CreateScope())
            using (var db = new MarvinEntities())
            {
                try
                {
                    //Get semaphore by ID
                    var semaphore = db.semaphore.SingleOrDefault(a => a.semaphoreId == semaphoreId);
                    //if we don't get one, create it!
                    if (semaphore == null)
                    {
                        db.semaphore.Add(new semaphore
                        {
                            semaphoreId = semaphoreId,
                            currentOwnerId = callerId,
                            heartBeat = _timeProvider.GetUtcNow()
                        });
                        //Try to save changes
                        try
                        {
                            db.SaveChanges();
                            currentOwnerId = callerId;
                            return true;
                        }
                            //If we get updateexception due to new record already inserted - get that record and return false.
                        catch (DbUpdateException)
                        {
                            var newSemaphore = db.semaphore.Single(a => a.semaphoreId == semaphoreId);
                            currentOwnerId = newSemaphore.currentOwnerId;
                            return false;
                        }
                    }
                    //If caller isn't owner and maxage haven't been reached return currentowner.
                    if (semaphore.currentOwnerId != callerId &&
                        semaphore.heartBeat.Add(MaxAge) > _timeProvider.GetUtcNow())
                    {
                        currentOwnerId = semaphore.currentOwnerId;
                        return false;
                    }
                    //Otherwise we need to attach caller as owner and update semaphore.
                    semaphore.currentOwnerId = callerId;
                    semaphore.heartBeat = _timeProvider.GetUtcNow();
                    //Try to save Semaphore entry
                    try
                    {
                        db.SaveChanges();
                        currentOwnerId = callerId;
                    }
                        //If row has been updated in the meantime. Reload DB version and return it
                    catch (DbUpdateConcurrencyException ex)
                    {
                        ex.Entries.Single().Reload();
                        currentOwnerId = semaphore.currentOwnerId;
                        return false;
                    }
                    return true;
                }
                finally
                {
                    scope.Complete();
                }
            }
        }

        public void Release(string semaphoreId, string callerId)
        {
            using (var scope = CreateScope())
            using (var db = new MarvinEntities())
            {
                var semaphore =
                    db.semaphore.SingleOrDefault(a => a.semaphoreId == semaphoreId && a.currentOwnerId == callerId);
                if (semaphore == null) return;
                db.semaphore.Remove(semaphore);
                scope.Complete();
            }
        }
        
        public Semaphore Probe(string semaphoreId)
        {
            using (CreateScope())
            using (var db = new MarvinEntities())
            {
                var semaphore = db.semaphore.SingleOrDefault(a => a.semaphoreId == semaphoreId);
                return Mapper.Map<Semaphore>(semaphore);
            }
        }

        public void Reset()
        {
            using (var scope = CreateScope())
            using (var db = new MarvinEntities())
            {
                db.semaphore.RemoveRange(db.semaphore);
                db.SaveChanges();
                scope.Complete();
            }
        }
    }
}