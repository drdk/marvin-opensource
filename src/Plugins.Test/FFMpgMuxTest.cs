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
    public class FFMpgMuxTest : CommonPluginTest
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
                    Format = StateFormat.dv5p,
                    AspectRatio = AspectRatio.ratio_16x9,
                    Files = new List<EssenceFile> { "sdfsdfsdf.mov" },
                    Duration = 234234234,
                    Attachments = new List<Attachment>() { new Attachment() { Type = AttachmentType.Audio, Path = @"\\sdfsd\sdfsdf\sdfsfsd.wav" } }
                },
                To = new Essence()
                {
                    Flags = StateFlags.AlternativeAudio,
                    Path = @"\\sdfsdfsdf\sdgsdgsdg",
                    Format = StateFormat.dv5p,
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

        private static IEnumerable<StateFormat> SupportedSourceFormats =>
            FFMpeg.FFMpeg.SupportedAudioMuxingVideoSourceFormats;

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
            FFMpeg.FFMpeg.SupportedAudioMuxingVideoDestinationFormats;

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
        public void CheckAndEstimateReturnsTrueIfToHasMoreThanZeroFiles()
        {
            Task.To.Files = new List<EssenceFile> { "sdsdf.mov" };
            Assert.That(sut.CheckAndEstimate(Task), Is.True);
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

        [TestCase(StateFormat.dv5p, 4.5)]
        [TestCase(StateFormat.xd5c, 4.5)]
        public void CheckAndEstimateSetsTaskEstimatCorrectly(StateFormat format, double transcodeFactor)
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
        public void DoWorkCallsFFMPegServiceMuxPostIfForeignKeyIsNullOrEmpty(string foreignKey)
        {
            mockFFMPegService = new Mock<IFFMpegService>();
            mockFFMPegService.Setup(m => m.PostMuxAudioJob(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Guid.NewGuid());

            FFMpegClient.FfmpegJobModel job = new FFMpegClient.FfmpegJobModel() { State = FfmpegJobModelState.InProgress };
            mockFFMPegService.Setup(m => m.GetAudioJob(It.IsAny<Guid>())).Returns(job);
            Task.ForeignKey = foreignKey;

            sut = new FFMpeg.FFMpeg(_pluginUrn, MockTimeProvider.Object, Logging, mockFFMPegService.Object, mockPresetProvider.Object);

            sut.Assign(Task);
            sut.Pulse(Task);

            mockFFMPegService.Verify(m => m.PostMuxAudioJob(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [TestCase("This is a foreign key")]
        public void DoWorkDoesNotCallFFMPegServiceIfForeignIsKeyNotEmpty(string foreignKey)
        {
            mockFFMPegService = new Mock<IFFMpegService>();
            mockFFMPegService.Setup(m => m.PostMuxAudioJob(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Guid.NewGuid());

            FFMpegClient.FfmpegJobModel job = new FFMpegClient.FfmpegJobModel() { State = FfmpegJobModelState.InProgress };
            mockFFMPegService.Setup(m => m.GetAudioJob(It.IsAny<Guid>())).Returns(job);
            Task.ForeignKey = foreignKey;

            sut = new FFMpeg.FFMpeg(_pluginUrn, MockTimeProvider.Object, Logging, mockFFMPegService.Object, mockPresetProvider.Object);

            sut.Assign(Task);
            sut.Pulse(Task);

            mockFFMPegService.Verify(m => m.PostMuxAudioJob(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            mockFFMPegService.Verify(m => m.PostAudioJob(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IList<AudioDestinationFormat>>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [TestCase(FfmpegJobModelState.Queued, ExecutionState.Running)]
        [TestCase(FfmpegJobModelState.InProgress, ExecutionState.Running)]
        [TestCase(FfmpegJobModelState.Done, ExecutionState.Done)]
        [TestCase(FfmpegJobModelState.Paused, ExecutionState.Running)]
        [TestCase(FfmpegJobModelState.Unknown, ExecutionState.Running)]
        [TestCase(FfmpegJobModelState.Failed, ExecutionState.Failed)]
        public void DoWorkSetsCurrentTaskStateToCorrectValue(FfmpegJobModelState FfmpegJobModel, ExecutionState assertState)
        {
            mockFFMPegService = new Mock<IFFMpegService>();
            mockFFMPegService.Setup(m => m.PostMuxAudioJob(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Guid.NewGuid());

            var job = new FfmpegJobModel()
            {
                State = FfmpegJobModel,
                Tasks = new ObservableCollection<FfmpegTaskModel>
                {
                    new FfmpegTaskModel
                    {
                        DestinationFilename = "\\\\sdfsdfsdf\\sdfgsdf\\test.mp4"
                    }
                }
            };
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
            mockFFMPegService.Setup(m => m.PostMuxAudioJob(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Guid.NewGuid());

            var job = new FfmpegJobModel()
            {
                State = FfmpegJobModelState.Done,
                Tasks = new ObservableCollection<FfmpegTaskModel>
                {
                    new FfmpegTaskModel
                    {
                        DestinationFilename = "\\\\sdfsdfsdf\\sdfgsdf\\testfilename.mp4"
                    }
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
        public void DoWorkDoesNotSetTargetFilesIfExecutionStateIsNotDone(FfmpegJobModelState FfmpegJobModel)
        {
            mockFFMPegService = new Mock<IFFMpegService>();
            mockFFMPegService.Setup(m => m.PostMuxAudioJob(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Guid.NewGuid());

            FFMpegClient.FfmpegJobModel job = new FFMpegClient.FfmpegJobModel() { State = FfmpegJobModel };
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
            mockFFMPegService.Setup(m => m.PostMuxAudioJob(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Guid.NewGuid());

            FFMpegClient.FfmpegJobModel job = new FFMpegClient.FfmpegJobModel() {
                State = FfmpegJobModelState.Done,
                Tasks = new ObservableCollection<FfmpegTaskModel>
                {
                    new FfmpegTaskModel
                    {
                        DestinationFilename = "\\\\sdfsdfsdf\\sdfgsdf\\test.mp4"
                    }
                }
            };
            mockFFMPegService.Setup(m => m.GetAudioJob(It.IsAny<Guid>())).Returns(job);

            sut = new FFMpeg.FFMpeg(_pluginUrn, MockTimeProvider.Object, Logging, mockFFMPegService.Object, mockPresetProvider.Object);

            Task.To.Path = "\\\\sdfsdfsdf\\sdfgsdf";

            sut.Assign(Task);
            sut.Pulse(Task);

            Assert.That(sut.GetStatus().CurrentTask.To.Files.Count, Is.EqualTo(1));
        }

        [Test]
        public void DoWorkCallSetsCurrentTaskStateToFailedIfExceptionOrrurs()
        {
            mockFFMPegService = new Mock<IFFMpegService>();
            mockFFMPegService.Setup(m => m.PostMuxAudioJob(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Guid.NewGuid());

            var job = new FfmpegJobModel() {
                State = FfmpegJobModelState.Done,
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
    }
}
