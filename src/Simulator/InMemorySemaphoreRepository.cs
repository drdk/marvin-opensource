using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using AutoMapper;
using DR.Marvin.Model;

namespace DR.Marvin.Simulator
{
    public class InMemorySemaphoreRepository : ISemaphoreRepository
    {
        private readonly IDictionary<string,KeyValuePair<string,DateTime>> _semaphoreDict = new ConcurrentDictionary<string, KeyValuePair<string, DateTime>>();
        private readonly ITimeProvider _timeProvider;

        public InMemorySemaphoreRepository(ITimeProvider timeProvider)
        {
            _timeProvider = timeProvider;
            MaxAge = TimeSpan.FromMinutes(1);
        }

        public TimeSpan MaxAge { get; }

        public bool Get(string semaphoreId, string callerId, out string currentOwnerId)
        {

            KeyValuePair<string, DateTime> currentOwner;
            if (_semaphoreDict.TryGetValue(semaphoreId, out currentOwner)
                && currentOwner.Key != callerId
                && currentOwner.Value.Add(MaxAge) > _timeProvider.GetUtcNow())
            {
                currentOwnerId = currentOwner.Key;
                return false;
            }
            _semaphoreDict[semaphoreId] = new KeyValuePair<string, DateTime>(callerId,_timeProvider.GetUtcNow());
            currentOwnerId = callerId;
            return true;
        }

        public Semaphore Probe(string semaphoreId)
        {
            var res = _semaphoreDict[semaphoreId];
            return  new Semaphore
            {
                SemaphoreId = semaphoreId,
                CurrentOwnerId = res.Key,
                HeartBeat = res.Value
            };
        }

        public void Release(string semaphoreId, string callerId)
        {
            KeyValuePair<string, DateTime> currentOwner;
            if (_semaphoreDict.TryGetValue(semaphoreId, out currentOwner) && currentOwner.Key == callerId)
                _semaphoreDict.Remove(semaphoreId);
        }

        public void Reset()
        {
            _semaphoreDict.Clear();
        }
    }
}
