using System;
using System.Collections.Generic;
using DR.Marvin.Model;
using DR.Marvin.WindowsService.AutomapperHelper;
using DR.Marvin.WindowsService.Model;
using DR.Marvin.MediaInfoService;
using Moq;
using NUnit.Framework;
using StructureMap;
using System.Linq;
using DR.Marvin.Simulator;

namespace WindowsService.Test
{
    [TestFixture]
    public class MappingHelperTest
    {
        private Order order;
        private Mock<IMediaInfoFacade> mockMediaInfoFacade;
        private Mock<ITimeProvider> mockTimeProvider = new Mock<ITimeProvider>(MockBehavior.Strict);
        private MediaInfoResult _videoMediaFileMetadata { get; set; }
        private MediaInfoResult _audioMediaFileMetadata { get; set; }

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            mockMediaInfoFacade = new Mock<IMediaInfoFacade>(MockBehavior.Strict);
            _videoMediaFileMetadata = new MediaInfoResult { Video = new Video { CodecId = "xd5c", DisplayAspectRatioRawValue = 16f / 9f , Width = 640 } };
            _audioMediaFileMetadata = new MediaInfoResult { Audio = new Audio { Format = "mpeg_audio", Channel = "2"} };

            ObjectFactory.Configure(configure => configure.For<IMediaInfoFacade>().Use(mockMediaInfoFacade.Object));

            mockTimeProvider.Setup(m => m.GetUtcNow()).Returns(DateTime.Now);
            ObjectFactory.Configure(configure => configure.For<ITimeProvider>().Use(mockTimeProvider.Object));

            order = new Order
            {
                BurnInLogo = false,
                BurnInSubtitles = false,
                DestinationFormat = StateFormat.h264_od_standard,
                DestinationPath = "\\\\ondnas01\\MediaCache\\Test\\WfsJob\\Marvin",
                DueDate = DateTime.Now,
                FilePath = "\\\\ondnas01\\MediaCache\\Test\\detkommernaermere.dif",
                AlternateAudioPath = "\\\\ondnas01\\MediaCache\\Test\\WavFileTest.wav",
                LogoPath = "",
                Priority = Priority.medium,
                SubtitlesPath = "",
                CallbackUrl = "http://some/system",
            };


            mockMediaInfoFacade.Setup(m => m.Read(It.Is<string>(x => !x.EndsWith("wav")))).Returns(_videoMediaFileMetadata);
            mockMediaInfoFacade.Setup(m => m.Read(It.Is<string>(x => x.EndsWith("wav")))).Returns(_audioMediaFileMetadata);

            order.Validate(mockMediaInfoFacade.Object, mockTimeProvider.Object);
        }

        [Test]
        public void GetSourceMapsCorrectly()
        {
            order.BurnInLogo = true;
            order.LogoPath = "\\\\some\\logopath\\file.jpg";
            order.BurnInSubtitles = true;

            var source = MappingHelper.GetSource(order);

            Assert.That(source.Attachments[0].Type, Is.EqualTo(AttachmentType.Logo));
            Assert.That(source.Attachments[1].Type, Is.EqualTo(AttachmentType.Subtitle));
            Assert.That(source.Attachments[2].Type, Is.EqualTo(AttachmentType.Audio));
            Assert.That(source.Attachments[1].Path, Is.EqualTo(order.SubtitlesPath));
            Assert.That(source.Attachments[2].Path, Is.EqualTo(order.AlternateAudioPath));
            Assert.That(source.Attachments.Count, Is.EqualTo(3));
            Assert.That(source.Path, Is.EqualTo(order.FilePath.Substring(0, order.FilePath.LastIndexOf("\\"))));
            Assert.That(source.Files[0].Value, Is.EqualTo(order.FilePath.Substring(order.FilePath.LastIndexOf("\\") + 1)));
            Assert.That(source.Files.Count, Is.EqualTo(1));

            order.BurnInSubtitles = false;
            order.BurnInLogo = false;
            source = MappingHelper.GetSource(order);

            Assert.That(source.Attachments.Count, Is.EqualTo(1));
        }

        [Test]
        public void GetDestinationMapsCorrectly()
        {
            order.BurnInLogo = true;
            order.LogoPath = "\\\\some\\logopath\\file.jpg";
            order.BurnInSubtitles = true;
            var destination = MappingHelper.GetDestination(order);

            Assert.That(destination.Flags.HasFlag(StateFlags.HardSubtitles), Is.True);
            Assert.That(destination.Flags.HasFlag(StateFlags.Logo), Is.True);
            Assert.That(destination.Flags.HasFlag(StateFlags.AlternativeAudio), Is.True);
            Assert.That(destination.Path, Is.EqualTo(order.DestinationPath));
            Assert.That(destination.Format, Is.EqualTo(StateFormat.h264_od_standard));

            order.BurnInLogo = false;
            order.BurnInSubtitles = false;
            destination = MappingHelper.GetDestination(order);

            Assert.That(destination.Flags.HasFlag(StateFlags.HardSubtitles), Is.False);
            Assert.That(destination.Flags.HasFlag(StateFlags.Logo), Is.False);
            Assert.That(destination.Flags.HasFlag(StateFlags.AlternativeAudio), Is.True);
        }

        [Test]
        public void ReturnsZeroPercentIfNoPlanIsProvided()
        {
            var job = CreateJob();
            Assert.That(MappingHelper.CalculatePercentageDone(job, mockTimeProvider.Object), Is.EqualTo(0));
        }

        [Test]
        public void Return100PercentIfJobPlanExecutionStateIsDone()
        {
            var job = CreateJobMockWithExecutionStateDone();
            Assert.That(MappingHelper.CalculatePercentageDone(job.Object, mockTimeProvider.Object), Is.EqualTo(100));
        }

        [TestCase(null)]
        [TestCase("12/12/2100")]
        public void ReturnZeroPercentIfThereIsNoStartTimeOrStartTimeIsInTheFuture(DateTime? date)
        {
            var job = CreateJobWithPlan(date);
            Assert.That(MappingHelper.CalculatePercentageDone(job, mockTimeProvider.Object), Is.EqualTo(0));
        }

        [Test]
        public void Return99IfPercentageDoneIsOver100()
        {
            var job = CreateJobWithPlan(DateTime.UtcNow.AddDays(-1));
            Assert.That(MappingHelper.CalculatePercentageDone(job, mockTimeProvider.Object), Is.EqualTo(99));
        }

        [Test]
        public void ReturnsZeroTaskStatusPercentIfNoPlanIsProvided()
        {
            var job = CreateJob();
            List<TaskProgress> result = MappingHelper.CalculateTaskPercentDone(job, mockTimeProvider.Object);
            Assert.That(result.First().PercentDone, Is.EqualTo(0));
        }

        [Test]
        public void Return100PercentTaskStatusIfJobPlanExecutionStateIsDone()
        {
            var job = CreateJobWithLongPlan(null, null, ExecutionState.Done, ExecutionState.Done);
            List<TaskProgress> result = MappingHelper.CalculateTaskPercentDone(job, mockTimeProvider.Object);
            Assert.That(result[0].PercentDone, Is.EqualTo(100));
            Assert.That(result[1].PercentDone, Is.EqualTo(100));
        }

        [Test]
        public void ReturnsTaskProgressIfJobPlanExecutionStateIsInProgress()
        {
            DateTime now = new DateTime(2001, 01, 01, 12, 0, 0, DateTimeKind.Utc);
            DateTime task1Start = new DateTime(2001, 01, 01, 11, 30, 0, DateTimeKind.Utc);
            DateTime task2Start = new DateTime(2001, 01, 01, 12, 30, 0, DateTimeKind.Utc);
            VirtualTimeProvider TimeProvider = new VirtualTimeProvider(now);
            var job = CreateJobWithLongPlan(task1Start, task2Start, ExecutionState.Running, ExecutionState.Queued, ExecutionState.Queued);
            List<TaskProgress> result = MappingHelper.CalculateTaskPercentDone(job, TimeProvider);
            Assert.That(result[0].PercentDone, Is.EqualTo(50), "task 1 percent done is wrong");
            Assert.That(result[0].PercentOfTotal, Is.EqualTo(33), "task 1 percent total is wrong");
            Assert.That(result[0].Name, Is.EqualTo("foo"), "task 1 name is wrong");
            Assert.That(result[1].PercentDone, Is.EqualTo(0), "task 2 percent done is wrong");
            Assert.That(result[1].PercentOfTotal, Is.EqualTo(66), "task 2 percent total is wrong");
            Assert.That(result[1].Name, Is.EqualTo("bar"), "task 2 name is wrong");
        }

        [Test]
        public void CustomFormatMappingTest()
        {
            var customorder = new Order
            {
                BurnInLogo = false,
                BurnInSubtitles = false,
                DestinationFormat = StateFormat.h264_od_standard,
                DestinationPath = "\\\\ondnas01\\MediaCache\\Test\\WfsJob\\Marvin",
                DueDate = DateTime.Now,
                FilePath = "\\\\ondnas01\\MediaCache\\Test\\detkommernaermere.dif",
                LogoPath = "",
                Priority = Priority.medium,
                SubtitlesPath = "",
                CallbackUrl = "http://some/system",
            };
            _videoMediaFileMetadata = new MediaInfoResult { Video = new Video { CodecId = "custom42", DisplayAspectRatioRawValue = 16f / 9f, Width = 640 } };
            mockMediaInfoFacade.Setup(m => m.Read(It.IsAny<string>()))
                .Returns(_videoMediaFileMetadata);
            customorder.Validate(mockMediaInfoFacade.Object,mockTimeProvider.Object);
            var source = MappingHelper.GetSource(customorder);
            Assert.That(source.Format, Is.EqualTo(StateFormat.custom));
            Assert.That(source.CustomFormat, Is.EqualTo("custom42"));

        }

        private Job CreateJob()
        {
            return new Job
            {
                Id = Guid.NewGuid(),
                Source = new Essence
                {
                    Files = new List<EssenceFile> { "foo" },
                    Flags = StateFlags.None,
                    Format = StateFormat.dv5p,
                    Path = "C:\\Temp\\"
                },
                Destination = new Essence
                {
                    Flags = StateFlags.HardSubtitles | StateFlags.Logo,
                    Format = StateFormat.h264_od_standard,
                    Path = "C:\\Output\\",
                    Attachments =
                        new List<Attachment>
                        {
                            new Attachment {Path = "c:\\Path", Arguments = new Dictionary<string, string>{ {"foo","bar"} }, Type = AttachmentType.Logo}
                        },
                    Files = new List<EssenceFile> { "filename", "filename2" },
                },
                DueDate = new DateTime(2001, 1, 1),
                Issued = new DateTime(2001, 1, 1),
                LastModified = DateTime.UtcNow
            };
        }

        private Job CreateJobWithPlan(DateTime? executionTaskstartTime = null)
        {
            var job = CreateJob();
            job.Plan = new ExecutionPlan
            {
                Tasks = new List<ExecutionTask>
                {
                    new ExecutionTask
                    {
                        Estimation = TimeSpan.FromHours(1),
                        From = job.Source,
                        To = job.Destination,
                        PluginUrn = "dr:marvin:plugin:foo",
                        State = ExecutionState.Running,
                        StartTime = executionTaskstartTime
                    }
                }
            };
            return job;
        }

        private Job CreateJobWithLongPlan(DateTime? executionTask1startTime = null, DateTime? executionTask2startTime = null, ExecutionState task1State = ExecutionState.Running, ExecutionState task2State = ExecutionState.Queued, ExecutionState task3State = ExecutionState.Queued)
        {
            var job = CreateJob();
            job.Plan = new ExecutionPlan
            {
                Tasks = new List<ExecutionTask>
                {
                    new ExecutionTask
                    {
                        Estimation = TimeSpan.FromHours(1),
                        From = job.Source,
                        To = job.Destination,
                        PluginUrn = "urn:dr:marvin:plugin:foo:01",
                        State = task1State,
                        StartTime = executionTask1startTime
                    },
                    new ExecutionTask
                    {
                        Estimation = TimeSpan.FromHours(2),
                        From = job.Source,
                        To = job.Destination,
                        PluginUrn = "urn:dr:marvin:plugin:bar:01",
                        State = task2State,
                        StartTime = executionTask2startTime
                    },
                    new ExecutionTask
                    {
                        Estimation = TimeSpan.FromHours(0),
                        From = job.Source,
                        To = job.Destination,
                        PluginUrn = "urn:dr:marvin:plugin:renamer:01",
                        State = task3State,
                        StartTime = executionTask2startTime
                    }
                }
            };
            return job;
        }

        private static Mock<Job> CreateJobMockWithExecutionStateDone()
        {
            var res = new Mock<Job>();
            res.Setup(a => a.Plan).Returns(new ExecutionPlan());
            res.Setup(a => a.Plan.GetState()).Returns(ExecutionState.Done);
            return res;
        }
    }
}