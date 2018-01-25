using System;
using System.Linq;
using System.Collections.Generic;
using DR.Marvin.Model;
using DR.Marvin.Planner;
using DR.Marvin.Plugins.Dummy;
using NUnit.Framework;
using Moq;
using DR.Marvin.Simulator;
using Moq.Protected;

namespace DR.Marvin.Executor.Test
{
    [TestFixture]
    public class ExecutorRetryTest
    {
        private readonly string RetryPluginUrn = $"{DummyRetry.UrnPrefix}1";
        private readonly Mock<DummyRetry> retryPlugin;

        public ExecutorRetryTest()
        {

            JobRepository = new InMemoryJobRepository(TimeProvider);
            SemaphoreRepository = new InMemorySemaphoreRepository(TimeProvider);
            CommandRepository = new InMemoryCommandRepository();
            retryPlugin = new Mock<DummyRetry>(MockBehavior.Loose,RetryPluginUrn, TimeProvider, Logging) {CallBase = true};
        }

        protected IExecutor Executor;
        protected readonly InMemoryJobRepository JobRepository;
        protected readonly InMemorySemaphoreRepository SemaphoreRepository;
        protected readonly InMemoryCommandRepository CommandRepository;
        protected readonly VirtualTimeProvider TimeProvider = new VirtualTimeProvider(new DateTime(2001, 01, 01));
        protected readonly ILogging Logging = new Mock<ILogging>(MockBehavior.Loose).Object;
        protected readonly Mock<ICallbackService> MockCallbackService = new Mock<ICallbackService>();

        [SetUp]
        public void Setup()
        {
            JobRepository.Reset();
            SemaphoreRepository.Reset();
            TimeProvider.Reset();
            MockCallbackService.Reset();
            CommandRepository.Reset();
            retryPlugin.Reset();
        }

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            AutoMapperHelper.EnsureInitialization();
            var plugins = new List<IPlugin>
            {
                retryPlugin.Object
            };

            Executor = new Executor(JobRepository, SemaphoreRepository, CommandRepository, new DummyPlanner(plugins, JobRepository, Logging), TimeProvider, plugins, MockCallbackService.Object, Logging);
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            Executor?.Dispose();
        }

        [Test]
        public void InstanceTest()
        {
            Assert.That(Executor, Is.Not.Null);
        }

        [Test]
        public void RetryPluginTest()
        {
            var oldJob = CreateRetryJob(2);
            var job = oldJob;
            JobRepository.Add(job);
            Assert.That(JobRepository.WaitingJobs().Count(), Is.EqualTo(1));
            var maxiumJobFinishTime = TimeProvider.GetUtcNow().AddMinutes(5);
            while ((JobRepository.WaitingJobs().Any() || JobRepository.ActiveJobs().Any()) && TimeProvider.GetUtcNow() < maxiumJobFinishTime)
            {
                MockCallbackService.Verify(m => m.MakeCallback(It.IsAny<Job>()), Times.Never, "MakeCallback should not be called yet.");
                Executor.Pulse();
                TimeProvider.Step(TimeSpan.FromSeconds(5));
            }

            Assert.That(TimeProvider.GetUtcNow() < maxiumJobFinishTime, "Must finish the jobs before the time limit.");
            retryPlugin.Verify(m => m.Assign(It.IsAny<ExecutionTask>()), Times.Once);
            retryPlugin.Verify(m=>m.Retry(It.IsAny<ExecutionTask>()),Times.Exactly(2));
            Assert.That(JobRepository.WaitingJobs().Count(), Is.EqualTo(0));
            Assert.That(JobRepository.ActiveJobs().Count(), Is.EqualTo(0));
            Assert.That(JobRepository.FailedJobs().Count(), Is.EqualTo(0));
            Assert.That(JobRepository.DoneJobs().Count(), Is.EqualTo(1));
            job = JobRepository.Get(job.Urn);

            Assert.That(job.Plan.Tasks.FirstOrDefault(t => t.PluginUrn == RetryPluginUrn).NumberOfRetries, Is.GreaterThan(0));
        }

        [Test]
        public void RetryPluginMax3TimesTest()
        {
            var oldJob = CreateRetryJob(4);
            var job = oldJob;
            JobRepository.Add(job);
            Assert.That(JobRepository.WaitingJobs().Count(), Is.EqualTo(1));
            var maxiumJobFinishTime = TimeProvider.GetUtcNow().AddMinutes(5);
            while ((JobRepository.WaitingJobs().Any() || JobRepository.ActiveJobs().Any()) && TimeProvider.GetUtcNow() < maxiumJobFinishTime)
            {
                MockCallbackService.Verify(m => m.MakeCallback(It.IsAny<Job>()), Times.Never, "MakeCallback should not be called yet.");
                Executor.Pulse();
                TimeProvider.Step(TimeSpan.FromSeconds(5));
            }

            Assert.That(TimeProvider.GetUtcNow() < maxiumJobFinishTime, "Must finish the jobs before the time limit.");

            retryPlugin.Verify(m => m.Retry(It.IsAny<ExecutionTask>()), Times.Exactly(retryPlugin.Object.RetryMax));
            Assert.That(JobRepository.WaitingJobs().Count(), Is.EqualTo(0));
            Assert.That(JobRepository.ActiveJobs().Count(), Is.EqualTo(0));
            Assert.That(JobRepository.DoneJobs().Count(), Is.EqualTo(0));
            Assert.That(JobRepository.FailedJobs().Count(), Is.EqualTo(1));
            job = JobRepository.Get(job.Urn);

            Assert.That(job.Plan.Tasks.FirstOrDefault(t => t.PluginUrn == RetryPluginUrn).NumberOfRetries, Is.EqualTo(retryPlugin.Object.RetryMax));
            Assert.That(job.Plan.Tasks.FirstOrDefault(t => t.PluginUrn == RetryPluginUrn).State, Is.EqualTo(ExecutionState.Failed));
        }

        private Job CreateRetryJob(int failTimes)
        {
            DateTime issued = TimeProvider.GetUtcNow();
            DateTime dueDate = TimeProvider.GetUtcNow();
            return new Job
            {
                Id = Guid.NewGuid(),
                Source = new Essence
                {
                    CustomFormat = "FailNumber." + failTimes
                },
                Destination = new Essence
                {

                },
                DueDate = dueDate,
                Issued = issued,
                CallbackUrl = "http://some/callback/url"
            };
        }
    }
}