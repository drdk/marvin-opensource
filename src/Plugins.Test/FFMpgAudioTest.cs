using System;
using System.Collections.Generic;
using DR.Marvin.Model;
using NUnit.Framework;
using Moq;
using System.Linq;
using DR.Marvin.Plugins.FFMpeg;
using DR.FFMpegClient;
using System.Collections.ObjectModel;
using DR.Marvin.Plugins.Common;

namespace DR.Marvin.Plugins.Test
{
    [TestFixture]
    public class FFMpgAudioTest : DynamicPluginTest
    {
        private Plugins.FFMpeg.FFMpeg sut;

        private readonly string _pluginUrn = $"{FFMpeg.FFMpeg.UrnPrefix}unittest1";
        Mock<IFFMpegService> mockFFMPegService = new Mock<IFFMpegService>(MockBehavior.Strict);
        Mock<CommonAudioPresetProvider> mockPresetProvider = new Mock<CommonAudioPresetProvider>() { CallBase = true };

        [SetUp]
        public void SetUp()
        {
            DynamicPlugin.ClearSiblings(FFMpeg.FFMpeg.Type);
            mockFFMPegService.Setup(m => m.GetNumberOfSupportedPlugins()).Returns(1);
            mockPresetProvider.Setup(m => m.AsList()).Returns(new List<FFMpegConfiguration>
            {
                new FFMpegConfiguration
                {
                    Format = StateFormat.audio_od_standard,
                    AudioDestinationsList = new ObservableCollection<AudioDestinationFormat>
                    {
                        new AudioDestinationFormat
                        {
                            AudioCodec = AudioDestinationFormatAudioCodec.MP3,
                            Format = AudioDestinationFormatFormat.MP3,
                            Bitrate = 64,
                            Channels = AudioDestinationFormatChannels.Mono
                        }
                        
                    }
                }
            });

            Plugin = sut = new FFMpeg.FFMpeg(_pluginUrn, MockTimeProvider.Object, Logging, mockFFMPegService.Object, mockPresetProvider.Object);

            Task = new ExecutionTask
            {
                From = new Essence()
                {
                    Flags = StateFlags.None,
                    Path = @"\\sdfsd\sdfsdf\sdfsfsd",
                    Format = StateFormat.mpeg_audio,
                    Files = new List<EssenceFile> { "sdfsdfsdf.mov" },
                    Duration = 234234234
                },
                To = new Essence()
                {
                    Flags = StateFlags.None,
                    Path = @"\\sdfsdfsdf\sdgsdgsdg",
                    Format = StateFormat.audio_od_standard,
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

        private static IEnumerable<StateFormat> SupportedSourceFormats =>
            FFMpeg.FFMpeg.SupportedAudioSourceFormats;

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
            FFMpeg.FFMpeg.SupportedAudioDestinationFormats;

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

        [TestCase(StateFormat.wma, 5.5)]
        [TestCase(StateFormat.mpeg_audio, 5.5)]
        public void CheckAndEstimateSetsTaskEstimatCorrectly(StateFormat format, double transcodeFactor)
        {
            Assert.That(Task.Estimation, Is.EqualTo(TimeSpan.Zero));

            Task.From.Format = format;
            Task.From.Duration = 12345;
            sut.CheckAndEstimate(Task);

            Assert.That(Task.Estimation, 
                Is.EqualTo(TimeSpan.FromMinutes(1) + TimeSpan.FromMilliseconds(Task.From.Duration / transcodeFactor)));
        }

        [TestCase(null)]
        [TestCase("")]
        public void DoWorkCallsFFMPegServicePostIfForeignKeyIsNullOrEmpty(string foreignKey)
        {
            mockFFMPegService = new Mock<IFFMpegService>();
            mockFFMPegService.Setup(m => m.PostAudioJob(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IList<AudioDestinationFormat>>(), null, null))
                .Returns(Guid.NewGuid());

            FFMpegClient.FfmpegJobModel job = new FFMpegClient.FfmpegJobModel() { State = FfmpegJobModelState.InProgress };
            mockFFMPegService.Setup(m => m.GetAudioJob(It.IsAny<Guid>())).Returns(job);
            Task.ForeignKey = foreignKey;

            sut = new FFMpeg.FFMpeg(_pluginUrn, MockTimeProvider.Object, Logging, mockFFMPegService.Object, mockPresetProvider.Object);

            sut.Assign(Task);
            sut.Pulse(Task);

            mockFFMPegService.Verify(m => m.PostAudioJob(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IList<AudioDestinationFormat>>(), null, null), Times.Once);
        }

        [TestCase("This is a foreign key")]
        public void DoWorkDoesNotCallFFMPegServicePostIfForeignIsKeyNotEmpty(string foreignKey)
        {
            mockFFMPegService = new Mock<IFFMpegService>();
            mockFFMPegService.Setup(m => m.PostAudioJob(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IList<AudioDestinationFormat>>(), null, null))
                .Returns(Guid.NewGuid());

            FFMpegClient.FfmpegJobModel job = new FFMpegClient.FfmpegJobModel() { State = FfmpegJobModelState.InProgress };
            mockFFMPegService.Setup(m => m.GetAudioJob(It.IsAny<Guid>())).Returns(job);
            Task.ForeignKey = foreignKey;

            sut = new FFMpeg.FFMpeg(_pluginUrn, MockTimeProvider.Object, Logging, mockFFMPegService.Object, mockPresetProvider.Object);

            sut.Assign(Task);
            sut.Pulse(Task);

            mockFFMPegService.Verify(m => m.PostAudioJob(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ObservableCollection<AudioDestinationFormat>>(), null, null), Times.Never);
        }

        [TestCase(FfmpegJobModelState.Queued, ExecutionState.Running)]
        [TestCase(FfmpegJobModelState.InProgress, ExecutionState.Running)]
        [TestCase(FfmpegJobModelState.Done, ExecutionState.Done)]
        [TestCase(FfmpegJobModelState.Paused, ExecutionState.Running)]
        [TestCase(FfmpegJobModelState.Unknown, ExecutionState.Running)]
        [TestCase(FfmpegJobModelState.Failed, ExecutionState.Failed)]
        public void DoWorkSetsCurrentTaskStateToCorrectValue(FfmpegJobModelState jobStatus, ExecutionState assertState)
        {
            mockFFMPegService = new Mock<IFFMpegService>();
            mockFFMPegService.Setup(m => m.PostAudioJob(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ObservableCollection<AudioDestinationFormat>>(), null, null))
                .Returns(Guid.NewGuid());

            FfmpegJobModel job = new FfmpegJobModel() { State = jobStatus, Tasks = new ObservableCollection<FfmpegTaskModel>() { new FfmpegTaskModel() { DestinationFilename = "\\\\sdfsdfsdf\\sdfgsdf\\1" } } };
            mockFFMPegService.Setup(m => m.GetAudioJob(It.IsAny<Guid>())).Returns(job);

            sut = new FFMpeg.FFMpeg(_pluginUrn, MockTimeProvider.Object, Logging, mockFFMPegService.Object, mockPresetProvider.Object);
            Task.To.Path = "\\\\sdfsdfsdf\\sdfgsdf";

            sut.Assign(Task);
            sut.Pulse(Task);

            Assert.That(Task.State, Is.EqualTo(assertState));
        }

        [Test]
        public void DoWorkSetsTargetFilesIfExecutionStateIsDone()
        {
            mockFFMPegService = new Mock<IFFMpegService>();
            mockFFMPegService.Setup(m => m.PostAudioJob(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ObservableCollection<AudioDestinationFormat>>(), null, null))
                .Returns(Guid.NewGuid());

            FfmpegJobModel job = new FfmpegJobModel()
            {
                State = FfmpegJobModelState.Done,
                Tasks = new ObservableCollection<FfmpegTaskModel>()
                {
                    new FfmpegTaskModel() { DestinationFilename = "\\\\sdfsdfsdf\\sdfgsdf\\1_64.mp3" },
                    new FfmpegTaskModel() { DestinationFilename = "\\\\sdfsdfsdf\\sdfgsdf\\1_192.mp3" },
                    new FfmpegTaskModel() { DestinationFilename = "\\\\sdfsdfsdf\\sdfgsdf\\1_64.mp4" }
                }
            };
            mockFFMPegService.Setup(m => m.GetAudioJob(It.IsAny<Guid>())).Returns(job);

            sut = new FFMpeg.FFMpeg(_pluginUrn, MockTimeProvider.Object, Logging, mockFFMPegService.Object, mockPresetProvider.Object);
            Task.To.Path = "\\\\sdfsdfsdf\\sdfgsdf";

            sut.Assign(Task);
            sut.Pulse(Task);

            Assert.That(Task.To.Files, Is.Not.Empty);
        }

        [TestCase(FfmpegJobModelState.Queued)]
        [TestCase(FfmpegJobModelState.InProgress)]
        [TestCase(FfmpegJobModelState.Paused)]
        [TestCase(FfmpegJobModelState.Unknown)]
        [TestCase(FfmpegJobModelState.Failed)]
        public void DoWorkDoesNotSetTargetFilesIfExecutionStateIsNotDone(FFMpegClient.FfmpegJobModelState jobStatus)
        {
            mockFFMPegService = new Mock<IFFMpegService>();
            mockFFMPegService.Setup(m => m.PostAudioJob(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ObservableCollection<AudioDestinationFormat>>(), null, null))
                .Returns(Guid.NewGuid());

            FfmpegJobModel job = new FFMpegClient.FfmpegJobModel() { State = jobStatus };
            mockFFMPegService.Setup(m => m.GetAudioJob(It.IsAny<Guid>())).Returns(job);

            sut = new FFMpeg.FFMpeg(_pluginUrn, MockTimeProvider.Object, Logging, mockFFMPegService.Object, mockPresetProvider.Object);

            sut.Assign(Task);
            sut.Pulse(Task);

            Assert.That(Task.To.Files, Is.Empty);
        }

        [Test]
        public void DoWorkAddsCorrectNumberOfFilenamesToCurrentTask()
        {
            mockFFMPegService = new Mock<IFFMpegService>();
            mockFFMPegService.Setup(m => m.PostAudioJob(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ObservableCollection<AudioDestinationFormat>>(), null, null))
                .Returns(Guid.NewGuid());

            FFMpegClient.FfmpegJobModel job = new FFMpegClient.FfmpegJobModel() {
                State = FfmpegJobModelState.Done,
                Tasks = new ObservableCollection<FfmpegTaskModel>()
                {
                    new FfmpegTaskModel() { DestinationFilename = "\\\\sdfsdfsdf\\sdfgsdf\\1_64.mp3" },
                    new FfmpegTaskModel() { DestinationFilename = "\\\\sdfsdfsdf\\sdfgsdf\\1_192.mp3" },
                    new FfmpegTaskModel() { DestinationFilename = "\\\\sdfsdfsdf\\sdfgsdf\\1_64.mp4" }
                }
            };
            mockFFMPegService.Setup(m => m.GetAudioJob(It.IsAny<Guid>())).Returns(job);

            sut = new FFMpeg.FFMpeg(_pluginUrn, MockTimeProvider.Object, Logging, mockFFMPegService.Object, mockPresetProvider.Object);

            Task.To.Path = "\\\\sdfsdfsdf\\sdfgsdf";

            sut.Assign(Task);
            sut.Pulse(Task);

            Assert.That(sut.GetStatus().CurrentTask.To.Files.Count, Is.EqualTo(3));
        }

        [Test]
        public void DoWorkCallSetsCurrentTaskStateToFailedIfExceptionOrrurs()
        {
            mockFFMPegService = new Mock<IFFMpegService>();
            mockFFMPegService.Setup(m => m.PostAudioJob(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ObservableCollection<AudioDestinationFormat>>(), null, null))
                .Returns(Guid.NewGuid());

            FFMpegClient.FfmpegJobModel job = new FFMpegClient.FfmpegJobModel() {
                State = FfmpegJobModelState.Done,
                Tasks = new ObservableCollection<FfmpegTaskModel>()
                {
                    new FfmpegTaskModel() { DestinationFilename = "\\\\sdfsdfsdf\\sdfgsdf\\1_64.mp3" }
                }
            };
            mockFFMPegService.Setup(m => m.GetAudioJob(It.IsAny<Guid>())).Returns(job);

            sut = new FFMpeg.FFMpeg(_pluginUrn, MockTimeProvider.Object, Logging, mockFFMPegService.Object, mockPresetProvider.Object);

            Task.To.Path = "\\\\NOT ALIKE AT ALL"; // Forces exception

            sut.Assign(Task);
            sut.Pulse(Task);

            Assert.That(Task.State, Is.EqualTo(ExecutionState.Failed));
        }

        [Test]
        public void ConstructorThrowsIfFFMPegServiceIsNull()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new FFMpeg.FFMpeg(_pluginUrn, MockTimeProvider.Object, Logging, null, mockPresetProvider.Object));
        }

        [Test]
        public void ConstructorThrowsIfTimeProviderIsNull()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new FFMpeg.FFMpeg(_pluginUrn, null, Logging, mockFFMPegService.Object, mockPresetProvider.Object));
        }

        [Test]
        [TestCase(null, typeof(ArgumentNullException))]
        [TestCase("", typeof(ArgumentNullException))]
        [TestCase("urn:ikke-dr:blah:blah", typeof(ArgumentException))]
        public void ConstructorThrowsIfUrnIsInvalid(string urn, System.Type exceptionType)
        {
            Assert.Throws(exceptionType, () =>
                new FFMpeg.FFMpeg(urn, MockTimeProvider.Object, Logging, mockFFMPegService.Object, mockPresetProvider.Object));
        }
        
        [Test]
        [TestCase(StateFlags.None)]
        [TestCase(StateFlags.Intro)]
        [TestCase(StateFlags.Outro)]
        [TestCase(StateFlags.Intro|StateFlags.Outro)]
        public void IntroOutroTest(StateFlags flags)
        {
            Task.From.Attachments = new List<Attachment>();
            string introArg = null;
            if (flags.HasFlag(StateFlags.Intro))
            {
                introArg = "D:\\temp\\intro.wav";
                Task.From.Attachments.Add(new Attachment
                {
                    Type = AttachmentType.Intro,
                    Path = introArg,
                    Arguments = new Dictionary<string, string>() { { "Duration","10000"} }
                });
                Task.To.Flags |= StateFlags.Intro;
            }
            string outroArg = null;
            if (flags.HasFlag(StateFlags.Outro))
            {
                outroArg = "D:\\temp\\outro.wav";
                Task.From.Attachments.Add(new Attachment
                {
                    Type = AttachmentType.Outro,
                    Path = outroArg,
                    Arguments = new Dictionary<string, string>() { { "Duration", "10000" } }
                });
                Task.To.Flags |= StateFlags.Outro;
            }
            Assert.That(sut.CheckAndEstimate(Task));

            mockFFMPegService = new Mock<IFFMpegService>(MockBehavior.Strict);
            var jobGuid = Guid.NewGuid();
            mockFFMPegService.Setup(m => m.PostAudioJob(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ObservableCollection<AudioDestinationFormat>>(), introArg, outroArg))
                .Returns(jobGuid);

            FfmpegJobModel job = new FfmpegJobModel()
            {
                State = FfmpegJobModelState.Done,
                Tasks = new ObservableCollection<FfmpegTaskModel>()
                {
                    new FfmpegTaskModel() { DestinationFilename = "\\\\sdfsdfsdf\\sdfgsdf\\1_64.mp3" },
                    new FfmpegTaskModel() { DestinationFilename = "\\\\sdfsdfsdf\\sdfgsdf\\1_192.mp3" },
                    new FfmpegTaskModel() { DestinationFilename = "\\\\sdfsdfsdf\\sdfgsdf\\1_64.mp4" }
                }
            };
            mockFFMPegService.Setup(m => m.GetAudioJob(jobGuid)).Returns(job);

            sut = new FFMpeg.FFMpeg(_pluginUrn, MockTimeProvider.Object, Logging, mockFFMPegService.Object, mockPresetProvider.Object);
            Task.To.Path = "\\\\sdfsdfsdf\\sdfgsdf";

            sut.Assign(Task);
            sut.Pulse(Task);
            Assert.That(Task.To.Files, Is.Not.Empty);
        }


        protected override DynamicPlugin[] SetupMockForDynmaicSlotsTest(int numberOfWorkingMachines)
        {
            mockFFMPegService = new Mock<IFFMpegService>(MockBehavior.Strict);
            mockFFMPegService.Setup(m => m.GetNumberOfSupportedPlugins()).Returns(numberOfWorkingMachines);
            DynamicPlugin.ClearSiblings(FFMpeg.FFMpeg.Type);

            var p1 = new FFMpeg.FFMpeg(FFMpeg.FFMpeg.UrnPrefix + 1, MockTimeProvider.Object, Logging, mockFFMPegService.Object, mockPresetProvider.Object);
            var p2 = new FFMpeg.FFMpeg(FFMpeg.FFMpeg.UrnPrefix + 2, MockTimeProvider.Object, Logging, mockFFMPegService.Object, mockPresetProvider.Object);
            var p3 = new FFMpeg.FFMpeg(FFMpeg.FFMpeg.UrnPrefix + 3, MockTimeProvider.Object, Logging, mockFFMPegService.Object, mockPresetProvider.Object);

            return new DynamicPlugin[] { p1, p2, p3 };
        }
    }
}
