using System;
using System.Linq;
using DR.Marvin.Model;
using DR.Marvin.Simulator;
using NUnit.Framework;

namespace DR.Marvin.Repositories.Test
{
    public abstract class CommonHealthCounterRepositoryTest
    {
        protected readonly VirtualTimeProvider TimeProvider = new VirtualTimeProvider(new DateTime(2001, 1, 1));
        protected readonly TimeSpan MaxAge = TimeSpan.FromMinutes(5);
        protected IHealthCounterRepository HealthCounterRepository;

        [OneTimeSetUp]
        public abstract void OneTimeSetUp();

        [SetUp]
        public abstract void SetUp();

        [Test]
        public void InitTest()
        {
            Assert.That(HealthCounterRepository.MaxAge, Is.EqualTo(MaxAge));
            Assert.That(HealthCounterRepository.ProbeAndPrune(), Is.Empty);
        }

        [Test]
        public void IncrementTest()
        {
            var testId = "PulseUnitTest";
            var message = "Hello World";
            HealthCounterRepository.Increment(testId,message);
            var res = HealthCounterRepository.ProbeAndPrune().ToList();
            Assert.That(res.Count, Is.EqualTo(1));
            var entry = res[0];
            Assert.That(entry.Count, Is.EqualTo(1));
            Assert.That(entry.Id, Is.EqualTo(testId));
            Assert.That(entry.Message, Is.EqualTo(message));
            Assert.That(entry.TimeStamp, Is.EqualTo(TimeProvider.GetUtcNow()));
            TimeProvider.Step(TimeSpan.FromSeconds(5));
            HealthCounterRepository.Increment(testId, message+"2");
            res = HealthCounterRepository.ProbeAndPrune().ToList();
            Assert.That(res.Count, Is.EqualTo(1));
            entry = res[0];
            Assert.That(entry.Count, Is.EqualTo(2));
            Assert.That(entry.Id, Is.EqualTo(testId));
            Assert.That(entry.Message, Is.EqualTo(message+"2"));
            Assert.That(entry.TimeStamp, Is.EqualTo(TimeProvider.GetUtcNow()));
            TimeProvider.Step(MaxAge + TimeSpan.FromSeconds(5));
            HealthCounterRepository.Increment(testId, message + "3");
            res = HealthCounterRepository.ProbeAndPrune().ToList();
            entry = res[0];
            Assert.That(entry.Count, Is.EqualTo(1));
        }

        [Test]
        public void PruneTest()
        {
            var testId = "PulseUnitTest";
            var message = "Hello World";
            HealthCounterRepository.Increment(testId, message);
            TimeProvider.Step(MaxAge + TimeSpan.FromSeconds(5));
            Assert.That(HealthCounterRepository.ProbeAndPrune(), Is.Empty);
        }

        [Test]
        public void MultipleKeysTest()
        {
            var testId = "PulseUnitTest";
            var message = "Hello World";
            HealthCounterRepository.Increment(testId+"1", message+"1");
            TimeProvider.Step(MaxAge - TimeSpan.FromSeconds(5));
            HealthCounterRepository.Increment(testId + "2", message + "2");
            var res = HealthCounterRepository.ProbeAndPrune().ToList();
            Assert.That(res.Count, Is.EqualTo(2));
            Assert.That(res.Any(hc=>hc.Id == testId + "1" && hc.Message == message + "1" && hc.Count == 1), Is.True);
            Assert.That(res.Any(hc => hc.Id == testId + "2" && hc.Message == message + "2" && hc.Count == 1), Is.True);
            TimeProvider.Step(TimeSpan.FromSeconds(10));
            res = HealthCounterRepository.ProbeAndPrune().ToList();
            Assert.That(res.Count, Is.EqualTo(1));
            Assert.That(res.Any(hc => hc.Id == testId + "2" && hc.Message == message + "2" && hc.Count == 1), Is.True);
        }
    }
}


