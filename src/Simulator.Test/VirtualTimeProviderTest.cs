using System;
using NUnit.Framework;

namespace DR.Marvin.Simulator.Test
{
    [TestFixture]
    public class VirtualTimeProviderTest
    {
        private VirtualTimeProvider timeProvider;
        private DateTime startTime;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            startTime = new DateTime(2001, 1, 1).ToUniversalTime();
            timeProvider = new VirtualTimeProvider(startTime);
        }

        [SetUp]
        public void Setup()
        {
            timeProvider.Reset();
        }

        [Test]
        public void InitTest()
        {
            Assert.That(timeProvider.GetUtcNow(), Is.EqualTo(startTime));
        }

        [Test]
        public void StepTest()
        {
            timeProvider.Step(TimeSpan.FromMinutes(1));
            Assert.That(timeProvider.GetUtcNow(), Is.EqualTo(startTime.AddMinutes(1)));
            timeProvider.Step(TimeSpan.FromMinutes(1));
            Assert.That(timeProvider.GetUtcNow(), Is.EqualTo(startTime.AddMinutes(2)));
        }

        [Test]
        public void NegativeStepTest()
        {
            Assert.Throws<Exception>(()=>timeProvider.Step(TimeSpan.FromMinutes(-1)));
        }
    }
}