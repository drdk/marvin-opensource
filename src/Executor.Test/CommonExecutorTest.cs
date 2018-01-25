using System;
using System.Collections.Generic;
using System.Linq;
using DR.Marvin.Model;
using DR.Marvin.Simulator;
using Moq;
using NUnit.Framework;

namespace DR.Marvin.Executor.Test
{
    public abstract class CommonExecutorTest
    {
        protected CommonExecutorTest()
        {
            JobRepository = new InMemoryJobRepository(TimeProvider);
            SemaphoreRepository = new InMemorySemaphoreRepository(TimeProvider);
            CommandRepository = new InMemoryCommandRepository();
            _mockLogging = new Mock<ILogging>(MockBehavior.Loose);
        }

        protected IExecutor Executor;
        protected readonly InMemoryJobRepository JobRepository;
        protected readonly InMemorySemaphoreRepository SemaphoreRepository;
        protected readonly InMemoryCommandRepository CommandRepository;
        protected readonly VirtualTimeProvider TimeProvider = new VirtualTimeProvider(new DateTime(2001, 01, 01));
        private readonly Mock<ILogging> _mockLogging;
        protected ILogging Logging => _mockLogging.Object;
        protected readonly Mock<ICallbackService> MockCallbackService = new Mock<ICallbackService>();
        
        public abstract void OneTimeSetUp();

        [SetUp]
        public void Setup()
        {
            _mockLogging.Reset();
            JobRepository.Reset();
            SemaphoreRepository.Reset();
            TimeProvider.Reset();
            MockCallbackService.Reset();
            CommandRepository.Reset();
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
        public void OneJob()
        {
            var oldJob = CreateNewJob();
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
            MockCallbackService.Verify(m => m.MakeCallback(It.IsAny<Job>()), Times.Once, "MakeCallback have not been called once.");
            Assert.That(TimeProvider.GetUtcNow() < maxiumJobFinishTime,"Most finish the jobs before the time limit.");
            Assert.That(JobRepository.WaitingJobs().Count(), Is.EqualTo(0));
            Assert.That(JobRepository.ActiveJobs().Count(), Is.EqualTo(0));
            Assert.That(JobRepository.DoneJobs().Count(), Is.EqualTo(1));
            job = JobRepository.Get(job.Urn);
            var flags = job.Plan.GetCurrentEssence().Flags;
            Assert.That((bool) (flags.HasFlag(StateFlags.HardSubtitles) && flags.HasFlag(StateFlags.Logo)));
            Assert.That(job.Destination.Files.Count, Is.EqualTo(3));
        }

        [Test]
        public void CommandCancelCallsCancelOnQueuedJobAndSetsStateToCanceled()
        {
            var job = new Job();
            JobRepository.Add(job);
            CommandRepository.Add(new Command() { Type = CommandType.Cancel, Urn = job.Urn, Username = "unittest"});

            Executor.Pulse();

            Assert.That(JobRepository.Get(job.Urn).Plan.GetState(), Is.EqualTo(ExecutionState.Canceled));
            Assert.That(JobRepository.ActiveJobs().Count(), Is.EqualTo(0));
        }


        [Test]
        public void CommandCancelDoesntFailIfJobDontHaveCurrentTask()
        {
            var job = new Job
            {
                Plan =
                    new ExecutionPlan()
                    {
                        Tasks = new List<ExecutionTask> {new ExecutionTask {State = ExecutionState.Done}}
                    }
            };
            JobRepository.Add(job);
            CommandRepository.Add(new Command() { Type = CommandType.Cancel, Urn = job.Urn, Username = "unittest" });
            Assert.That(JobRepository.ActiveJobs().Count(), Is.EqualTo(0));
            Assert.That(JobRepository.DoneJobs().Count(), Is.EqualTo(1));
            Executor.Pulse();

            Assert.That(JobRepository.Get(job.Urn).Plan.GetState(), Is.EqualTo(ExecutionState.Done));
            Assert.That(JobRepository.ActiveJobs().Count(), Is.EqualTo(0));
            Assert.That(JobRepository.DoneJobs().Count(), Is.EqualTo(1));
            _mockLogging.Verify(l => l.LogWarning(It.Is<string>(x=>x.StartsWith("Unable to cancel job")), job.Urn), Times.Once);
        }

        [Test]
        public void CommandCancelCallsCancelOnJobRunningWfsPluginAndSetsStateToCanceled()
        {
            var job = CreateNewJob();
            JobRepository.Add(job);
            
            Executor.Pulse(); //planner.calculate is not called on the first pulse
            Executor.Pulse();

            Assert.That(JobRepository.Get(job.Urn).Plan.GetState(), Is.EqualTo(ExecutionState.Running));

            CommandRepository.Add(new Command() { Type = CommandType.Cancel, Urn = job.Urn, Username = "unittest"});

            Executor.Pulse();

            Assert.That(JobRepository.Get(job.Urn).Plan.GetState(), Is.EqualTo(ExecutionState.Canceled));
        }

        private Job CreateNewJob(DateTime? issued=null, DateTime? dueDate=null)
        {
            issued = issued ?? TimeProvider.GetUtcNow();
            dueDate = dueDate ?? TimeProvider.GetUtcNow();
            return new Job
            {
                Id = Guid.NewGuid(),
                Source = new Essence
                {
                    Files =  new List<EssenceFile> { "foo" },
                    Flags = StateFlags.None,
                    Format = StateFormat.dv5p,
                    Path = "C:\\Temp\\"
                },
                Destination = new Essence
                {
                    Flags = StateFlags.HardSubtitles | StateFlags.Logo,
                    Format = StateFormat.h264_od_standard,
                    Path = "C:\\Output\\"
                },
                DueDate = dueDate.Value,
                Issued = issued.Value,
                CallbackUrl = "http://some/callback/url"
            };
        }
    }
}