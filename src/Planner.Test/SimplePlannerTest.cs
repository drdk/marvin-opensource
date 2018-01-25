using System;
using System.Collections.Generic;
using System.Linq;
using DR.Marvin.Model;
using DR.Marvin.Plugins.FileRenamer;
using DR.Marvin.Plugins.Wfs;
using DR.Marvin.Plugins.FFMpeg;
using Moq;
using NUnit.Framework;

namespace DR.Marvin.Planner.Test
{
    [TestFixture]
    public class SimplePlannerTest : CommonPlannerTest
    {
        private Mock<ICallbackService> _callBackService;
        [SetUp]
        public new void Setup()
        {
            base.Setup();
            _callBackService = new Mock<ICallbackService>();
        }

        [Test]
        public void InstanceTest()
        {
            var planner = new SimplePlanner(new List<IPlugin>(), JobRepository, Logging, _callBackService.Object);
            Assert.That(planner, Is.Not.Null);
        }

        [Test]
        public void PlanOneJobTest()
        {
            var wfsPluginMock = CreateWfsMock("1");
            wfsPluginMock.SetupGet(p => p.Busy).Returns(false);
            wfsPluginMock.Setup(p => p.CheckAndEstimate(It.IsAny<ExecutionTask>())).Returns(true);
            var planner = new SimplePlanner(new List<IPlugin> { wfsPluginMock.Object }, JobRepository, Logging, _callBackService.Object);
            var job = CreateNewWfsJob();
            var jobUrn = job.Urn;
            JobRepository.Add(job);
            Assert.That(JobRepository.WaitingJobs().Count(), Is.EqualTo(1));
            Assert.That(JobRepository.ActiveJobs().Count(), Is.EqualTo(0));
            planner.Calculate();
            Assert.That(JobRepository.WaitingJobs().Count(), Is.EqualTo(0));
            Assert.That(JobRepository.ActiveJobs().Count(), Is.EqualTo(1));
            Assert.That(JobRepository.ActiveJobs().First().Urn, Is.EqualTo(jobUrn));
        }

        [Test]
        public void PlanTwoJobsWithMultipleTranscodingPluginsTest()
        {
            var wfsPluginMock = CreateWfsMock("1");
            wfsPluginMock.SetupGet(p => p.Busy).Returns(false);
            wfsPluginMock.Setup(p => p.CheckAndEstimate(It.IsAny<ExecutionTask>())).Returns(true);
            var ffmpegPluginMock = CreateFFMpegMock("1");
            ffmpegPluginMock.SetupGet(p => p.Busy).Returns(false);
            ffmpegPluginMock.Setup(p => p.CheckAndEstimate(It.IsAny<ExecutionTask>())).Returns(true);
            var planner = new SimplePlanner(new List<IPlugin> { wfsPluginMock.Object, ffmpegPluginMock.Object }, JobRepository, Logging, _callBackService.Object);
            var audioJob = CreateNewFFMpegJob();
            var videoJob = CreateNewWfsJob();
            JobRepository.Add(audioJob);
            JobRepository.Add(videoJob);
            Assert.That(JobRepository.WaitingJobs().Count(), Is.EqualTo(2));
            Assert.That(JobRepository.ActiveJobs().Count(), Is.EqualTo(0));
            planner.Calculate();
            ffmpegPluginMock.Verify(x => x.CheckAndEstimate(It.IsAny<ExecutionTask>()), Times.Once(), "ffmpeg plugin should only be called once");
            wfsPluginMock.Verify(x => x.CheckAndEstimate(It.IsAny<ExecutionTask>()), Times.Once(), "Wfs plugin should only be called once");
            Assert.That(JobRepository.WaitingJobs().Count(), Is.EqualTo(0));
            Assert.That(JobRepository.ActiveJobs().Count(), Is.EqualTo(2));
            var dbAudioJob = JobRepository.ActiveJobs().First(j =>
                    j.Plan.Tasks.First().PluginUrn == ffmpegPluginMock.Object.Urn);
            var dbVideoJob = JobRepository.ActiveJobs().First(j =>
                   j.Plan.Tasks.First().PluginUrn == wfsPluginMock.Object.Urn);
            Assert.That(dbAudioJob.Urn, Is.EqualTo(audioJob.Urn));
            Assert.That(dbVideoJob.Urn, Is.EqualTo(videoJob.Urn));
        }

        [Test]
        public void PlanAJobWithMuxingTest()
        {
            var wfsPluginMock = CreateWfsMock("1");
            wfsPluginMock.SetupGet(p => p.Busy).Returns(false);
            wfsPluginMock.Setup(p => p.CheckAndEstimate(It.IsAny<ExecutionTask>())).Returns(true);
            var ffmpegPluginMock = CreateFFMpegMock("1");
            ffmpegPluginMock.SetupGet(p => p.Busy).Returns(false);
            ffmpegPluginMock.Setup(p => p.CheckAndEstimate(It.IsAny<ExecutionTask>())).Returns(true);
            var planner = new SimplePlanner(new List<IPlugin> { wfsPluginMock.Object, ffmpegPluginMock.Object }, JobRepository, Logging, _callBackService.Object);
            var audioJob = CreateNewAudioMuxJob();
            JobRepository.Add(audioJob);
            Assert.That(JobRepository.WaitingJobs().Count(), Is.EqualTo(1));
            Assert.That(JobRepository.ActiveJobs().Count(), Is.EqualTo(0));
            planner.Calculate();
            ffmpegPluginMock.Verify(x => x.CheckAndEstimate(It.IsAny<ExecutionTask>()), Times.Exactly(1), "ffmpeg plugin should only be called once for muxing");
            wfsPluginMock.Verify(x => x.CheckAndEstimate(It.IsAny<ExecutionTask>()), Times.Exactly(1), "Wfs plugin should only be called once for transcoding");
            Assert.That(JobRepository.WaitingJobs().Count(), Is.EqualTo(0));
            Assert.That(JobRepository.ActiveJobs().Count(), Is.EqualTo(1));
            var dbMuxJob = JobRepository.ActiveJobs().First();
            Assert.That(dbMuxJob.Plan.Tasks.Count(), Is.EqualTo(2));
            Assert.That(dbMuxJob.Plan.Tasks[0].PluginUrn, Is.EqualTo(ffmpegPluginMock.Object.Urn));
            Assert.That(dbMuxJob.Plan.Tasks[1].PluginUrn, Is.EqualTo(wfsPluginMock.Object.Urn));
        }

        [Test]
        public void PlanAnAudioJobWhileMuxingTest()
        {
            var wfsPluginMock = CreateWfsMock("1");
            wfsPluginMock.SetupGet(p => p.Busy).Returns(false);
            wfsPluginMock.Setup(p => p.CheckAndEstimate(It.IsAny<ExecutionTask>())).Returns(true);
            var ffmpegPluginMock = CreateFFMpegMock("1");
            ffmpegPluginMock.SetupGet(p => p.Busy).Returns(false);
            ffmpegPluginMock.Setup(p => p.CheckAndEstimate(It.IsAny<ExecutionTask>())).Returns(true);
            var planner = new SimplePlanner(new List<IPlugin> { wfsPluginMock.Object, ffmpegPluginMock.Object }, JobRepository, Logging, _callBackService.Object);

            //Plan the mux job
            var muxJob = CreateNewAudioMuxJob();
            JobRepository.Add(muxJob);
            planner.Calculate();

            //Advance muxjob progress to wfs, leaving the ffmpeg plugin free
            var dbMuxJob = JobRepository.ActiveJobs().First();
            dbMuxJob.Plan.Tasks[0].State = ExecutionState.Done;
            dbMuxJob.Plan.Tasks[1].State = ExecutionState.Running;
            JobRepository.Update(dbMuxJob);

            //Plan the audio trancoding job
            var audioJob = CreateNewFFMpegJob();
            JobRepository.Add(audioJob);
            planner.Calculate();

            //Check both jobs are running
            Assert.That(JobRepository.WaitingJobs().Count(), Is.EqualTo(0));
            Assert.That(JobRepository.ActiveJobs().Count(), Is.EqualTo(2));
        }

        [Test]
        public void PlannerUsesRenamePluginIfDestinationEssenceFilenameIsPresent()
        {
            var wfsPluginMock = CreateWfsMock("1");
            wfsPluginMock.SetupGet(p => p.Busy).Returns(false);
            wfsPluginMock.Setup(p => p.CheckAndEstimate(It.IsAny<ExecutionTask>())).Returns(true);
            var fileRenamerPluginMock = CreateFileRenamerMock("1");
            fileRenamerPluginMock.SetupGet(p => p.Busy).Returns(false);
            fileRenamerPluginMock.Setup(p => p.CheckAndEstimate(It.IsAny<ExecutionTask>())).Returns(true);

            var planner = new SimplePlanner(new List<IPlugin> { wfsPluginMock.Object, fileRenamerPluginMock.Object }, JobRepository, Logging, _callBackService.Object);
            var job = CreateNewWfsJob();
            job.Destination.Files  = new List<EssenceFile> { EssenceFile.Template("NewName_%index%.%ext%") };
            var jobUrn = job.Urn;
            JobRepository.Add(job);
            planner.Calculate();
            Assert.That(JobRepository.Get(jobUrn).Plan.Tasks.Count, Is.EqualTo(2));
            Assert.That(JobRepository.Get(jobUrn).Plan.Tasks[1].PluginUrn, Is.EqualTo(fileRenamerPluginMock.Object.Urn));
        }

        [Test]
        public void PrioritySortTest()
        {
            var jobDue1DayLow = CreateNewWfsJob(dueDate: TimeProvider.GetUtcNow().AddDays(1));
            jobDue1DayLow.Priority = Priority.low;
            JobRepository.Add(jobDue1DayLow);
            var jobDue1DayHigh = CreateNewWfsJob(dueDate: TimeProvider.GetUtcNow().AddDays(1));
            jobDue1DayHigh.Priority = Priority.high;
            JobRepository.Add(jobDue1DayHigh);
            var jobDueNowDayLow = CreateNewWfsJob();
            jobDueNowDayLow.Priority = Priority.low;
            JobRepository.Add(jobDueNowDayLow);
            var jobDueNowDayHigh = CreateNewWfsJob();
            jobDueNowDayHigh.Priority = Priority.high;
            JobRepository.Add(jobDueNowDayHigh);
            Assert.That(JobRepository.WaitingJobs().Count(), Is.EqualTo(4));

            var wfsPluginMock = CreateWfsMock("1");
            var wfsPluginMock2 = CreateWfsMock("2");
            wfsPluginMock.SetupGet(p => p.Busy).Returns(false);
            wfsPluginMock.Setup(p => p.CheckAndEstimate(It.IsAny<ExecutionTask>())).Returns(true);
            wfsPluginMock2.SetupGet(p => p.Busy).Returns(false);
            wfsPluginMock2.Setup(p => p.CheckAndEstimate(It.IsAny<ExecutionTask>())).Returns(true);
            var planner = new SimplePlanner(new List<IPlugin> {  wfsPluginMock.Object, wfsPluginMock2.Object }, JobRepository, Logging, _callBackService.Object);

            planner.Calculate();
            var activeJob = JobRepository.ActiveJobs().First(m => m.Id == jobDueNowDayHigh.Id);
            Assert.That(activeJob.Urn, Is.EqualTo(jobDueNowDayHigh.Urn));
            activeJob.Plan.GetCurrentTask().State = ExecutionState.Done;
            activeJob.Plan.MoveToNextTask();
            JobRepository.Update(activeJob);

            planner.Calculate();
            activeJob = JobRepository.ActiveJobs().First(m => m.Id == jobDue1DayHigh.Id);
            Assert.That(activeJob.Urn, Is.EqualTo(jobDue1DayHigh.Urn));
            activeJob.Plan.GetCurrentTask().State = ExecutionState.Done;
            activeJob.Plan.MoveToNextTask();
            JobRepository.Update(activeJob);

            planner.Calculate();
            activeJob = JobRepository.ActiveJobs().First(m => m.Id == jobDueNowDayLow.Id);
            Assert.That(activeJob.Urn, Is.EqualTo(jobDueNowDayLow.Urn));
            activeJob.Plan.GetCurrentTask().State = ExecutionState.Done;
            activeJob.Plan.MoveToNextTask();
            JobRepository.Update(activeJob);

            planner.Calculate();
            activeJob = JobRepository.ActiveJobs().First(m => m.Id == jobDue1DayLow.Id);
            Assert.That(activeJob.Urn, Is.EqualTo(jobDue1DayLow.Urn));
            activeJob.Plan.GetCurrentTask().State = ExecutionState.Done;
            activeJob.Plan.MoveToNextTask();
            JobRepository.Update(activeJob);

            Assert.That(JobRepository.WaitingJobs(), Is.Empty);
            Assert.That(JobRepository.ActiveJobs(), Is.Empty);
            Assert.That(JobRepository.DoneJobs().Count(), Is.EqualTo(4));
        }

        [Test]
        public void OnlyHighPriorityJobsWillBeGivenTheLastWfsNode()
        {
            var wfsPluginMock = CreateWfsMock("1");
            wfsPluginMock.SetupGet(p => p.Busy).Returns(false);
            wfsPluginMock.Setup(p => p.CheckAndEstimate(It.IsAny<ExecutionTask>())).Returns(true);
            var planner = new SimplePlanner(new List<IPlugin> { wfsPluginMock.Object }, JobRepository, Logging, _callBackService.Object);
            var jobMedium = CreateNewWfsJob();
            jobMedium.Priority = Priority.medium;
            JobRepository.Add(jobMedium);
            planner.Calculate();
            
            // Medium prio job will be started with only one wfs node
            Assert.That(JobRepository.WaitingJobs().Count(), Is.EqualTo(0));
            Assert.That(JobRepository.ActiveJobs().Count(), Is.EqualTo(1));
            JobRepository.Reset();
            jobMedium = CreateNewWfsJob();
            jobMedium.Priority = Priority.medium;
            JobRepository.Add(jobMedium);
            var jobHigh = CreateNewWfsJob();
            jobHigh.Priority = Priority.high;

            JobRepository.Add(jobHigh);
            planner.Calculate();

            // High prio job will start with one wfs node
            Assert.That(JobRepository.ActiveJobs().First().Urn, Is.EqualTo(jobHigh.Urn));
            Assert.That(JobRepository.WaitingJobs().First().Urn, Is.EqualTo(jobMedium.Urn));

            JobRepository.ActiveJobs().First().Plan.GetCurrentTask().State = ExecutionState.Done;
            JobRepository.ActiveJobs().First().Plan.MoveToNextTask();

            JobRepository.Reset();
            var wfsPluginMock2 = CreateWfsMock("2");
            wfsPluginMock2.SetupGet(p => p.Busy).Returns(false);
            wfsPluginMock2.Setup(p => p.CheckAndEstimate(It.IsAny<ExecutionTask>())).Returns(true);
            planner = new SimplePlanner(new List<IPlugin> { wfsPluginMock.Object, wfsPluginMock2.Object }, JobRepository, Logging, _callBackService.Object);
            
            var jobMedium2 = CreateNewWfsJob();
            jobMedium2.Priority = Priority.medium;
            JobRepository.Add(jobMedium2);
            planner.Calculate();

            // Medium prio job will start with two wfs nodes
            Assert.That(JobRepository.ActiveJobs().First().Urn, Is.EqualTo(jobMedium2.Urn));
            Assert.That(JobRepository.WaitingJobs().Count(), Is.EqualTo(0));
        }

        [Test]
        public void MaxFiveMinutesJobsWillBeTreatedAsHighPriorityJobsAndBeGivenTheLastWfsNode()
        {
            var wfsPluginMock = CreateWfsMock("1");
            wfsPluginMock.SetupGet(p => p.Busy).Returns(false);
            wfsPluginMock.Setup(p => p.CheckAndEstimate(It.IsAny<ExecutionTask>())).Returns(true);
            var wfsPluginMock2 = CreateWfsMock("2");
            wfsPluginMock2.SetupGet(p => p.Busy).Returns(false);
            wfsPluginMock2.Setup(p => p.CheckAndEstimate(It.IsAny<ExecutionTask>())).Returns(true);
            var planner = new SimplePlanner(new List<IPlugin> { wfsPluginMock.Object, wfsPluginMock2.Object }, JobRepository, Logging, _callBackService.Object);

            var jobHighPrio = CreateNewWfsJob();
            jobHighPrio.Priority = Priority.high;
            JobRepository.Add(jobHighPrio);

            var jobMediumPrioLong = CreateNewWfsJob();
            jobMediumPrioLong.Priority = Priority.medium;
            jobMediumPrioLong.Source.Duration = 300001;
            JobRepository.Add(jobMediumPrioLong);
            planner.Calculate();

            // Medium prio long job (more than 5 min) will not start if one node left
            Assert.That(JobRepository.WaitingJobs().First().Urn, Is.EqualTo(jobMediumPrioLong.Urn));

            JobRepository.Reset();
            var jobMediumPrioShort = CreateNewWfsJob();
            jobMediumPrioShort.Priority = Priority.medium;
            jobMediumPrioShort.Source.Duration = 30000;
            JobRepository.Add(jobMediumPrioShort);
            JobRepository.Add(jobHighPrio);
            planner.Calculate();

            // Medium prio short job (less than 5 min) will start if one node left
            Assert.That(JobRepository.WaitingJobs().Count(), Is.EqualTo(0));
            Assert.That(JobRepository.ActiveJobs().Select(t => t.Urn == jobMediumPrioShort.Urn), Is.Not.Null);
        }
        [Test]
        public void CalculateDoesNotReserveTheLastFFmpegPluginForHighPriorityOnly()
        {
            var wfsPluginMock = CreateFFMpegMock("1");
            wfsPluginMock.SetupGet(p => p.Busy).Returns(false);
            wfsPluginMock.Setup(p => p.CheckAndEstimate(It.IsAny<ExecutionTask>())).Returns(true);
           
            var wfsPluginMock2 = CreateFFMpegMock("2");
            wfsPluginMock2.SetupGet(p => p.Busy).Returns(false);
            wfsPluginMock2.Setup(p => p.CheckAndEstimate(It.IsAny<ExecutionTask>())).Returns(true);
            var planner = new SimplePlanner(new List<IPlugin> { wfsPluginMock.Object, wfsPluginMock2.Object }, JobRepository, Logging, _callBackService.Object);

            var jobMedium = CreateNewFFMpegJob();
            jobMedium.Priority = Priority.medium;
            JobRepository.Add(jobMedium);

            var jobLow = CreateNewFFMpegJob();
            jobLow.Priority = Priority.low;
            JobRepository.Add(jobLow);
            planner.Calculate();
            
            Assert.That(JobRepository.WaitingJobs().Count(), Is.EqualTo(0));
            Assert.That(JobRepository.ActiveJobs().Count(), Is.EqualTo(2));
        }

        private Mock<IPlugin> CreateWfsMock(string id)
        {
            var res = new Mock<IPlugin>(MockBehavior.Strict);
            res.SetupGet(p => p.PluginType).Returns(Wfs.Type);
            res.SetupGet(p => p.Urn).Returns(Wfs.UrnPrefix + id);
            res.SetupGet(p => p.AsyncOperation).Returns(true);
            return res;
        }

        private Mock<IPlugin> CreateFFMpegMock(string id)
        {
            var res = new Mock<IPlugin>(MockBehavior.Strict);
            res.SetupGet(p => p.PluginType).Returns(FFMpeg.Type);
            res.SetupGet(p => p.Urn).Returns(FFMpeg.UrnPrefix + id);
            res.SetupGet(p => p.AsyncOperation).Returns(true);
            return res;
        }

        private Mock<IPlugin> CreateFileRenamerMock(string id)
        {
            var res = new Mock<IPlugin>(MockBehavior.Strict);
            res.SetupGet(p => p.PluginType).Returns(FileRenamer.Type);
            res.SetupGet(p => p.Urn).Returns(FileRenamer.UrnPrefix + id);
            res.SetupGet(p => p.AsyncOperation).Returns(false);
            return res;
        }

        private Job CreateNewWfsJob(DateTime? issued = null, DateTime? dueDate = null)
        {
            issued = issued ?? TimeProvider.GetUtcNow();
            dueDate = dueDate ?? TimeProvider.GetUtcNow();
            return new Job
            {
                Id = Guid.NewGuid(),
                Source = new Essence
                {
                    Files = new List<EssenceFile> { "foo" },
                    Format = StateFormat.dv5p,
                    Path = "C:\\Temp\\"
                },
                Destination = new Essence
                {
                    Format = StateFormat.h264_od_standard,
                    Path = "C:\\Output\\"
                },
                DueDate = dueDate.Value,
                Issued = issued.Value,
                Priority = Priority.high
            };
        }
        private Job CreateNewFFMpegJob(DateTime? issued = null, DateTime? dueDate = null)
        {
            issued = issued ?? TimeProvider.GetUtcNow();
            dueDate = dueDate ?? TimeProvider.GetUtcNow();
            return new Job
            {
                Id = Guid.NewGuid(),
                Source = new Essence
                {
                    Files = new List<EssenceFile> { "foo" },
                    Format = StateFormat.audio_od_standard,
                    Path = "C:\\Temp\\"
                },
                Destination = new Essence
                {
                    Format = StateFormat.audio_od_standard,
                    Path = "C:\\Output\\"
                },
                DueDate = dueDate.Value,
                Issued = issued.Value,
                Priority = Priority.high
            };
        }

        private Job CreateNewAudioMuxJob(DateTime? issued = null, DateTime? dueDate = null)
        {
            issued = issued ?? TimeProvider.GetUtcNow();
            dueDate = dueDate ?? TimeProvider.GetUtcNow();
            return new Job
            {
                Id = Guid.NewGuid(),
                Source = new Essence
                {
                    Files = new List<EssenceFile> { "foo" },
                    Format = StateFormat.dv5p,
                    Path = "C:\\Temp\\",
                    Attachments = new List<Attachment>() { new Attachment() { Type = AttachmentType.Audio, Path = "C:\\Temp\\some.wav" } }
                },
                Destination = new Essence
                {
                    Flags =  StateFlags.AlternativeAudio,
                    Format = StateFormat.h264_od_standard,
                    Path = "C:\\Output\\"
                },
                DueDate = dueDate.Value,
                Issued = issued.Value,
                Priority = Priority.high
            };
        }

    }
}