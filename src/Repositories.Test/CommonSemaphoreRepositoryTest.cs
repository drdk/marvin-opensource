using System;
using DR.Marvin.Model;
using DR.Marvin.Simulator;
using NUnit.Framework;

namespace DR.Marvin.Repositories.Test
{
    public abstract class CommonSemaphoreRepositoryTest
    {
        protected readonly VirtualTimeProvider TimeProvider = new VirtualTimeProvider(new DateTime(2001, 1, 1));
        private readonly string _semaphoreId = "UnitTest@" + Environment.MachineName;
        protected  ISemaphoreRepository SemaphoreRepository;

        [OneTimeSetUp]
        public abstract void OneTimeSetUp();

        [SetUp]
        public abstract void SetUp();

        [Test]
        public void SemaphoreTest()
        {
            Assert.That(SemaphoreRepository.MaxAge > TimeSpan.FromSeconds(2),
                "Can not complete the test with a SemaphoreMaxAge less than 2 sec.");
            var callerId1 = "CommonRepoTest1";
            string currentOwner;

            Assert.That(SemaphoreRepository.Get(_semaphoreId, callerId1, out currentOwner), Is.True,
                "Should get lock from empty repo");
            Assert.That(callerId1, Is.EqualTo(currentOwner),
                "Current owner should be set correctly");

            // Make a time step less than SemaphoreMaxAge
            TimeProvider.Step(SemaphoreRepository.MaxAge.Add(TimeSpan.FromSeconds(-1)));

            var callerId2 = "CommonRepoTest2";
            Assert.That(SemaphoreRepository.Get(_semaphoreId, callerId2, out currentOwner), Is.False,
                "Another caller should not get the lock before max age has expired.");
            Assert.That(callerId1, Is.EqualTo(currentOwner),
                "Current owner should not have changed.");
            Assert.That(SemaphoreRepository.Get(_semaphoreId, callerId1, out currentOwner), Is.True,
                "First caller should still get the lock");
            Assert.That(callerId1, Is.EqualTo(currentOwner),
                "Current owner should not have changed.");

            // Make a time step more than SemaphoreMaxAge
            TimeProvider.Step(SemaphoreRepository.MaxAge.Add(TimeSpan.FromSeconds(1)));
            Assert.That(SemaphoreRepository.Get(_semaphoreId, callerId2, out currentOwner), Is.True,
                "The semaphore max age has been exceded ownership change should be allowed.");
            Assert.That(callerId2, Is.EqualTo(currentOwner),
                "The current owner is changed correctly to 2nd caller id.");
            Assert.That(SemaphoreRepository.Get(_semaphoreId, callerId1, out currentOwner), Is.False,
                "The previous caller is denied, since the semaphore has moved to 2nd caller");
            Assert.That(callerId2, Is.EqualTo(currentOwner),
                "Current owner should not have changed.");

            var probe = SemaphoreRepository.Probe(_semaphoreId);
            Assert.That(probe.SemaphoreId, Is.EqualTo(_semaphoreId));
            Assert.That(probe.CurrentOwnerId, Is.EqualTo(callerId2));
            Assert.That(probe.HeartBeat, Is.EqualTo(TimeProvider.GetUtcNow()));
        }
    }
}
