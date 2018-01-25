using System;
using System.Collections.Generic;
using DR.Marvin.Model;
using NUnit.Framework;
using DR.WfsService.Contract;
using DR.WfsService.JMServices;
using Moq;
using System.Linq;
using DR.Marvin.Plugins.Common;
using DR.Marvin.Plugins.Wfs;

namespace DR.Marvin.Plugins.Test
{
    [TestFixture]
    public class WfsTest : DynamicPluginTest
    {
        private Plugins.Wfs.Wfs sut;
        Mock<IWfsService> mockWfsService = new Mock<IWfsService>();
        Mock<CommonPresetProvider> mockPresetProvider = new Mock<CommonPresetProvider>() { CallBase = true };

        private readonly string _pluginUrn = $"{Wfs.Wfs.UrnPrefix}unittest1";

        [SetUp]
        public void SetUp()
        {
            DynamicPlugin.ClearSiblings(Wfs.Wfs.Type);
            mockWfsService.Setup(m => m.GetWorkingNodes(It.IsAny<string>())).Returns(new string[1]);
            mockPresetProvider.Setup(m => m.AsList()).Returns(new List<WorkflowConfiguration>
            {
                new WorkflowConfiguration
                {
                    AspectRatio = AspectRatio.ratio_16x9,
                    BurnInLogo = false,
                    Format = StateFormat.h264_od_standard,
                    Workflow = Guid.NewGuid()
                }
            });
            mockPresetProvider.SetupGet(m => m.MachineGroup).Returns("UnitTestGroup");
            Plugin = sut = new Wfs.Wfs(_pluginUrn, MockTimeProvider.Object, Logging, mockWfsService.Object, mockPresetProvider.Object);

            Task = new ExecutionTask
            {
                From = new Essence()
                {
                    Flags = StateFlags.None,
                    Path = @"\\sdfsd\sdfsdf\sdfsfsd",
                    Format = StateFormat.dv5p,
                    AspectRatio = AspectRatio.ratio_16x9,
                    Files = new List<EssenceFile> { "sdfsdfsdf.mov" },
                    Duration = 234234234
                },
                To = new Essence()
                {
                    Flags = StateFlags.None,
                    Path = @"\\sdfsdfsdf\sdgsdgsdg",
                    Format = StateFormat.h264_od_standard,
                    AspectRatio = AspectRatio.ratio_16x9,
                    Files = new List<EssenceFile>()
                },
                PluginUrn = _pluginUrn
            };
        }


        [Test]
        public void CheckAndEstimateReturnsTrueIfAllValuesAreValid()
        {
            Assert.That(sut.CheckAndEstimate(Task), Is.True);
        }

        private static IEnumerable<StateFlags> AllFlagsPermutations
        {
            get
            {
                var maxValue = ((StateFlags[])Enum.GetValues(typeof(StateFlags))).Aggregate((a, b) => a | b);
                ulong counter = 0;
                while (counter <= (ulong)maxValue)
                {
                    yield return (StateFlags)counter++;
                }
            }
        }

        private static IEnumerable<TestCaseData> FromAndToFlagsAreDifferentCases =>
            from fromArg in AllFlagsPermutations
            from toArg in AllFlagsPermutations
            where fromArg != toArg
            select new TestCaseData(fromArg, toArg).Returns((fromArg | StateFlags.Logo) == (toArg | StateFlags.Logo));


        [TestCaseSource(nameof(FromAndToFlagsAreDifferentCases))]
        public bool CheckAndEstimateReturnsFalseIfFromAndToFlagsAreDifferentAndNotLogo(StateFlags from, StateFlags to)
        {
            Task.From.Flags = from;
            Task.To.Flags = to;
            return sut.CheckAndEstimate(Task);
        }

        private static IEnumerable<TestCaseData> FromAndToFlagsAreEqualCases =>
            from fromArg in AllFlagsPermutations
            from toArg in AllFlagsPermutations
            where fromArg == toArg
            select new TestCaseData(fromArg, toArg);

        [TestCaseSource(nameof(FromAndToFlagsAreEqualCases))]
        public void CheckAndEstimateReturnsTrueIfFromAndToFlagsAreEqual(StateFlags from, StateFlags to)
        {
            Task.From.Flags = from;
            Task.To.Flags = to;
            Assert.That(sut.CheckAndEstimate(Task), Is.True);
        }

        private static IEnumerable<StateFormat> SupportedSourceFormats =>
            Wfs.Wfs.SupportedSourceFormats;

        private static IEnumerable<StateFormat> UnsupportedSourceFormats =>
            from sourceFormat in (StateFormat[])Enum.GetValues(typeof(StateFormat))
            where !SupportedSourceFormats.Contains(sourceFormat)
            select sourceFormat;


        [TestCaseSource(nameof(UnsupportedSourceFormats))]
        public void CheckAndEstimateReturnsFalseIfSourceFormatIsInvalid(StateFormat sourceFormat)
        {
            Task.From.Format = sourceFormat;
            Assert.That(sut.CheckAndEstimate(Task), Is.False);
        }

        [TestCaseSource(nameof(SupportedSourceFormats))]
        public void CheckAndEstimateReturnsTrueIfSourceFormatIsValid(StateFormat sourceFormat)
        {
            Task.From.Format = sourceFormat;
            Assert.That(sut.CheckAndEstimate(Task), Is.True);
        }

        private static IEnumerable<StateFormat> SupportedDestinationFormats => 
            Wfs.Wfs.SupportedDestinationFormats;

        private static IEnumerable<StateFormat> UnsupportedDestinationFormats =>
            from destinationFormat in (StateFormat[])Enum.GetValues(typeof(StateFormat))
            where !SupportedDestinationFormats.Contains(destinationFormat)
            select destinationFormat;

        [TestCaseSource(nameof(UnsupportedDestinationFormats))]
        public void CheckAndEstimateReturnsFalseIfDestinationFormatIsInvalid(StateFormat destinationFormat)
        {
            Task.To.Format = destinationFormat;
            Assert.That(sut.CheckAndEstimate(Task), Is.False);
        }

        [TestCaseSource(nameof(SupportedDestinationFormats))]
        public void CheckAndEstimateReturnsTrueIfDestinationFormatIsValid(StateFormat destinationFormat)
        {
            Task.To.Format = destinationFormat;
            Assert.That(sut.CheckAndEstimate(Task), Is.True);
        }

        [Test]
        public void CheckAndEstimateReturnsFalseIfFromHasLessThanOneFile()
        {
            Task.From.Files =  new List<EssenceFile>();
            Assert.That(sut.CheckAndEstimate(Task), Is.False);
        }

        [Test]
        public void CheckAndEstimateReturnsFalseIfFromHasMoreThanOneFile()
        {
            Task.From.Files = new List<EssenceFile> { "sdsdf.mov", "sdfgsadf.dif" };
            Assert.That(sut.CheckAndEstimate(Task), Is.False);
        }
        [Test]
        public void CheckAndEstimateReturnsTrueIfFromHasOneFile()
        {
            Task.From.Files = new List<EssenceFile> { "32423423.mov" };
            Assert.That(sut.CheckAndEstimate(Task), Is.True);
        }

        [Test]
        public void CheckAndEstimateReturnsFalseIfToHasMoreThanZeroFiles()
        {
            Task.To.Files = new List<EssenceFile> { "sdsdf.mov" };
            Assert.That(sut.CheckAndEstimate(Task), Is.False);
        }

        [Test]
        public void CheckAndEstimateReturnsTrueIfToHasZeroFiles()
        {
            Task.To.Files = new List<EssenceFile>();
            Assert.That(sut.CheckAndEstimate(Task), Is.True);
        }

        [TestCase("")]
        [TestCase(null)]
        public void CheckAndEstimateReturnsFalseIfFromPathIsEmptyOrNull(string path)
        {
            Task.From.Path = path;
            Assert.That(sut.CheckAndEstimate(Task), Is.False);
        }

        [Test]
        public void CheckAndEstimateReturnsTrueIfFromPathIsNotEmptyHasZeroFiles()
        {
            Task.From.Path = "\\\\SomePath\\here";
            Assert.That(sut.CheckAndEstimate(Task), Is.True);
        }

        [TestCase("")]
        [TestCase(null)]
        public void CheckAndEstimateReturnsFalseIfToPathIsEmptyOrNull(string path)
        {
            Task.To.Path = path;
            Assert.That(sut.CheckAndEstimate(Task), Is.False);
        }

        [Test]
        public void CheckAndEstimateReturnsTrueIfToPathIsNotEmptyHasZeroFiles()
        {
            Task.To.Path = "\\\\SomePath\\here";
            Assert.That(sut.CheckAndEstimate(Task), Is.True);
        }

        [TestCase(StateFormat.dv5p, 2.5)]
        [TestCase(StateFormat.xd5c, 1.3)]
        public void CheckAndEstimateSetsTaskEstimatCorrectIfDv5p(StateFormat format, double transcodeFactor)
        {
            Assert.That(Task.Estimation, Is.EqualTo(TimeSpan.Zero));

            Task.From.Format = format;
            Task.From.Duration = 12345;
            sut.CheckAndEstimate(Task);

            Assert.That(Task.Estimation, 
                Is.EqualTo(TimeSpan.FromMinutes(1) +
                TimeSpan.FromMilliseconds(Task.From.Duration / transcodeFactor)));
        }

        [TestCase(null)]
        [TestCase("")]
        public void DoWorkCallsWfsServiceEnqueueJobIfForeignKeyIsNullOrEmpty(string foreignKey)
        {
            mockWfsService = new Mock<IWfsService>();
            mockWfsService.Setup(m => m.EnqueueJob(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Guid.NewGuid());

            WfsService.JMServices.Job job = new WfsService.JMServices.Job() { Status = JobStatus.Active };
            mockWfsService.Setup(m => m.GetJob(It.IsAny<Guid>())).Returns(job);
            Task.ForeignKey = foreignKey;

            sut = new Wfs.Wfs(_pluginUrn, MockTimeProvider.Object, Logging, mockWfsService.Object, mockPresetProvider.Object);

            sut.Assign(Task);
            sut.Pulse(Task);

            mockWfsService.Verify(m => m.EnqueueJob(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [TestCase("This is a foreign key")]
        public void DoWorkDoesNotCallWfsServiceEnqueueJobIfForeignIsKeyNotEmpty(string foreignKey)
        {
            mockWfsService = new Mock<IWfsService>();
            mockWfsService.Setup(m => m.EnqueueJob(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Guid.NewGuid());

            WfsService.JMServices.Job job = new WfsService.JMServices.Job() { Status = JobStatus.Active };
            mockWfsService.Setup(m => m.GetJob(It.IsAny<Guid>())).Returns(job);
            Task.ForeignKey = foreignKey;

            sut = new Wfs.Wfs(_pluginUrn, MockTimeProvider.Object, Logging, mockWfsService.Object, mockPresetProvider.Object);

            sut.Assign(Task);
            sut.Pulse(Task);

            mockWfsService.Verify(m => m.EnqueueJob(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [TestCase(JobStatus.Queued, ExecutionState.Running)]
        [TestCase(JobStatus.Active, ExecutionState.Running)]
        [TestCase(JobStatus.Completed, ExecutionState.Done)]
        [TestCase(JobStatus.Pausing, ExecutionState.Running)] // TODO chance to Pause when executor supports it.
        [TestCase(JobStatus.Paused, ExecutionState.Running)] // TODO chance to Pause when executor supports it.
        [TestCase(JobStatus.Abort, ExecutionState.Failed)]
        [TestCase(JobStatus.Fatal, ExecutionState.Failed)]
        [TestCase(JobStatus.Inactive, ExecutionState.Failed)]
        public void DoWorkDoesSetsCurrentTaskStateToCorrectValue(JobStatus jobStatus, ExecutionState assertState)
        {
            mockWfsService = new Mock<IWfsService>();
            mockWfsService.Setup(m => m.EnqueueJob(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Guid.NewGuid());

            mockWfsService.Setup(m => m.GetErrors(It.IsAny<Guid>())).Returns(new List<string>());
            WfsService.JMServices.Job job = new WfsService.JMServices.Job() { Status = jobStatus };
            mockWfsService.Setup(m => m.GetJob(It.IsAny<Guid>())).Returns(job);

            sut = new Wfs.Wfs(_pluginUrn, MockTimeProvider.Object, Logging, mockWfsService.Object, mockPresetProvider.Object);

            sut.Assign(Task);
            sut.Pulse(Task);

            Assert.That(Task.State, Is.EqualTo(assertState));
        }

        [Test]
        public void DoWorkCallsWfsServiceGetTargetFilesIfExecutionStateIsDone()
        {
            mockWfsService = new Mock<IWfsService>();
            mockWfsService.Setup(m => m.EnqueueJob(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Guid.NewGuid());

            WfsService.JMServices.Job job = new WfsService.JMServices.Job() { Status = JobStatus.Completed };
            mockWfsService.Setup(m => m.GetJob(It.IsAny<Guid>())).Returns(job);

            sut = new Wfs.Wfs(_pluginUrn, MockTimeProvider.Object, Logging, mockWfsService.Object, mockPresetProvider.Object);

            sut.Assign(Task);
            sut.Pulse(Task);

            mockWfsService.Verify(m => m.GetTargetFiles(It.IsAny<Guid>()), Times.Once);
        }

        [TestCase(JobStatus.Queued)]
        [TestCase(JobStatus.Active)]
        [TestCase(JobStatus.Pausing)]
        [TestCase(JobStatus.Paused)]
        [TestCase(JobStatus.Abort)]
        [TestCase(JobStatus.Fatal)]
        public void DoWorkDoesNotCallWfsServiceGetTargetFilesIfExecutionStateIsNotDone(JobStatus jobStatus)
        {
            mockWfsService = new Mock<IWfsService>();
            mockWfsService.Setup(m => m.EnqueueJob(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Guid.NewGuid());

            WfsService.JMServices.Job job = new WfsService.JMServices.Job() { Status = jobStatus };
            mockWfsService.Setup(m => m.GetJob(It.IsAny<Guid>())).Returns(job);
            mockWfsService.Setup(m => m.GetErrors(It.IsAny<Guid>())).Returns(new List<string>());

            sut = new Wfs.Wfs(_pluginUrn, MockTimeProvider.Object, Logging, mockWfsService.Object, mockPresetProvider.Object);

            sut.Assign(Task);
            sut.Pulse(Task);

            mockWfsService.Verify(m => m.GetTargetFiles(It.IsAny<Guid>()), Times.Never);
        }

        [Test]
        public void DoWorkCallsAddsCorrectNumberOfFilenamesToCurrentTaskFromWfsServiceGetTargetFiles()
        {
            mockWfsService = new Mock<IWfsService>();
            mockWfsService.Setup(m => m.EnqueueJob(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Guid.NewGuid());
            mockWfsService.Setup(m => m.GetErrors(It.IsAny<Guid>())).Returns(new List<string>());

            WfsService.JMServices.Job job = new WfsService.JMServices.Job() { Status = JobStatus.Completed };
            mockWfsService.Setup(m => m.GetJob(It.IsAny<Guid>())).Returns(job);
            mockWfsService.Setup(m => m.GetTargetFiles(It.IsAny<Guid>())).Returns(new List<string>
            {
                "\\\\sdfsdfsdf\\sdfgsdf\\1",
                "\\\\sdfsdfsdf\\sdfgsdf\\2",
                "\\\\sdfsdfsdf\\sdfgsdf\\3"
            });

            sut = new Wfs.Wfs(_pluginUrn, MockTimeProvider.Object, Logging, mockWfsService.Object, mockPresetProvider.Object);

            Task.To.Path = "\\\\sdfsdfsdf\\sdfgsdf";

            sut.Assign(Task);
            sut.Pulse(Task);

            Assert.That(sut.GetStatus().CurrentTask.To.Files.Count, Is.EqualTo(3));
        }

        [Test]
        public void DoWorkCallSetsCurrentTaskStateToFailedIfExceptionOrrurs()
        {
            mockWfsService = new Mock<IWfsService>();
            mockWfsService.Setup(m => m.EnqueueJob(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Guid.NewGuid());
            mockWfsService.Setup(m => m.GetErrors(It.IsAny<Guid>())).Returns(new List<string>());
            WfsService.JMServices.Job job = new WfsService.JMServices.Job() { Status = JobStatus.Completed };
            mockWfsService.Setup(m => m.GetJob(It.IsAny<Guid>())).Returns(job);
            mockWfsService.Setup(m => m.GetTargetFiles(It.IsAny<Guid>())).Returns(new List<string>
            {
                "\\\\sdfsdfsdf\\sdfgsdf\\1",
                "\\\\sdfsdfsdf\\sdfgsdf\\2",
                "\\\\sdfsdfsdf\\sdfgsdf\\3"
            });

            sut = new Wfs.Wfs(_pluginUrn, MockTimeProvider.Object, Logging, mockWfsService.Object, mockPresetProvider.Object);

            Task.To.Path = "\\\\NOT ALIKE AT ALL"; // Forces exception

            sut.Assign(Task);
            sut.Pulse(Task);

            Assert.That(Task.State, Is.EqualTo(ExecutionState.Failed));
        }

        [Test]
        public void ConstructorThrowsIfWfsServiceIsNull()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new Wfs.Wfs(_pluginUrn, MockTimeProvider.Object, Logging, null, mockPresetProvider.Object));
        }

        [Test]
        public void ConstructorThrowsIfPresetProviderIsNull()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new Wfs.Wfs(_pluginUrn, MockTimeProvider.Object, Logging, mockWfsService.Object, null));
        }

        [Test]
        public void ConstructorThrowsIfTimeProviderIsNull()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new Wfs.Wfs(_pluginUrn, null, Logging, mockWfsService.Object, mockPresetProvider.Object));
        }

        [Test]
        [TestCase(null, typeof(ArgumentNullException))]
        [TestCase("", typeof(ArgumentNullException))]
        [TestCase("urn:ikke-dr:blah:blah", typeof(ArgumentException))]
        public void ConstructorThrowsIfUrnIsInvalid(string urn, Type exceptionType)
        {
            Assert.Throws(exceptionType, () =>
                new Wfs.Wfs(urn, MockTimeProvider.Object, Logging, mockWfsService.Object, mockPresetProvider.Object));
        }

        protected override DynamicPlugin[] SetupMockForDynmaicSlotsTest(int numberOfWorkingMachines)
        {
            mockWfsService = new Mock<IWfsService>();
            mockWfsService.Setup(m => m.GetWorkingNodes(It.IsAny<string>())).Returns(new string[numberOfWorkingMachines]);
            DynamicPlugin.ClearSiblings(Wfs.Wfs.Type);

            var p1 = new Wfs.Wfs(Wfs.Wfs.UrnPrefix + 1, MockTimeProvider.Object, Logging, mockWfsService.Object, mockPresetProvider.Object);
            var p2 = new Wfs.Wfs(Wfs.Wfs.UrnPrefix + 2, MockTimeProvider.Object, Logging, mockWfsService.Object, mockPresetProvider.Object);
            var p3 = new Wfs.Wfs(Wfs.Wfs.UrnPrefix + 3, MockTimeProvider.Object, Logging, mockWfsService.Object, mockPresetProvider.Object);
            return new DynamicPlugin[] { p1, p2, p3 };
        }
    }
}
