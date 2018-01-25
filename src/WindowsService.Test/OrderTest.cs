using System;
using DR.Marvin.Model;
using DR.Marvin.WindowsService.Model;
using DR.Marvin.MediaInfoService;
using Moq;
using NUnit.Framework;
using StructureMap;
using Resolution = DR.Marvin.Model.Resolution;

namespace DR.Marvin.WindowsService.Test
{
    [TestFixture]
    public class OrderTest
    {
        private Order sut;
        private Mock<IMediaInfoFacade> mockMediaInfoFacade;
        private Mock<ITimeProvider> mockTimeProvider = new Mock<ITimeProvider>(MockBehavior.Strict);

        MediaInfoResult _videoMediaFileMetadata;
        MediaInfoResult _audioMediaFileMetadata;

        [SetUp]
        public void Setup()
        {
            mockMediaInfoFacade = new Mock<IMediaInfoFacade>(MockBehavior.Strict);
            _videoMediaFileMetadata = new MediaInfoResult
            {
                Duration = 42,
                Video = new Video {CodecId = "xd5c", DisplayAspectRatioRawValue = 16f/9f, Width = 640},
                Audio = new Audio {Format = "pcm", Channel = "2"}
            };
            _audioMediaFileMetadata = new MediaInfoResult { Duration = 42, Audio = new Audio { Format = "mpeg_audio", Channel = "2"} };

            ObjectFactory.Configure(configure => configure.For<IMediaInfoFacade>().Use(mockMediaInfoFacade.Object));

            mockTimeProvider.Setup(m => m.GetUtcNow()).Returns(DateTime.Now);
            ObjectFactory.Configure(configure => configure.For<ITimeProvider>().Use(mockTimeProvider.Object));

            sut = new Order
            {
                BurnInLogo = true,
                BurnInSubtitles = false,
                DestinationFormat = StateFormat.h264_od_single,
                DestinationPath = "\\\\ondnas01\\MediaCache\\Test\\WfsJob\\Marvin",
                DueDate = mockTimeProvider.Object.GetUtcNow(),
                FilePath = "\\\\ondnas01\\MediaCache\\Test\\detkommernaermere.dif",
                LogoPath = @"\\ondnas01\MediaCache\Test\pepe30.png",
                AlternateAudioPath = "\\\\ondnas01\\MediaCache\\Test\\WavFileTest.wav",
                Priority = Priority.medium,
                SubtitlesPath = "",
                CallbackUrl = "http://some/system",
                DestinationFilename = "destinationFilename"
            };

            mockMediaInfoFacade.Setup(m => m.Read(It.Is<string>(x => x.EndsWith("wav")))).Returns(_audioMediaFileMetadata);
            mockMediaInfoFacade.Setup(m => m.Read(It.Is<string>(x => !x.EndsWith("wav")))).Returns(_videoMediaFileMetadata);
        }

        [Test]
        public void ValidatePassesTest()
        {
            sut.Validate(mockMediaInfoFacade.Object, mockTimeProvider.Object);
        }

        [Test]
        public void AudioTranscodingTest()
        {
            sut.FilePath = sut.AlternateAudioPath;
            sut.AlternateAudioPath = null;
            sut.DestinationFormat = StateFormat.audio_od_standard;

            sut.Validate(mockMediaInfoFacade.Object, mockTimeProvider.Object);
            Assert.That(sut.Validated,Is.True);
            sut.DestinationFormat = StateFormat.h264_od_standard;
            var ex = Assert.Throws<OrderException>(() => sut.Validate(mockMediaInfoFacade.Object, mockTimeProvider.Object));
            Assert.That(ex.Message, Is.EqualTo("DestinationFormat h264_od_standard is invalid for source format mpeg_audio."));
        }

        [Test]
        [TestCase(
            null,
            ExpectedResult = AspectRatio.ratio_16x9,
            Description = "MediaInfo Reads AspectRatio If Not Forced")]
        [TestCase(
            AspectRatio.unknown,
            ExpectedResult = AspectRatio.ratio_16x9,
            Description = "MediaInfo Reads AspectRatio If Not Forced")]
        [TestCase(
            AspectRatio.ratio_4x3,
            ExpectedResult = AspectRatio.ratio_4x3,
            Description = "Disregards MediaInfo if forced")]
        [TestCase(
            AspectRatio.ratio_16x9,
            ExpectedResult = AspectRatio.ratio_16x9,
            Description = "Disregards MediaInfo if forced")]
        public AspectRatio MediaInfoReadAspectRatioIfNotPresent(AspectRatio value)
        {
            sut.ForceAspectRatio = value;
            sut.Validate(mockMediaInfoFacade.Object, mockTimeProvider.Object);
            return sut.AspectRatio;
        }

        [Test]
        [TestCase(
            null,
            ExpectedResult = Resolution.sd,
            Description = "MediaInfo Reads Resolution If Not Forced")]
        [TestCase(
            Resolution.unknown,
            ExpectedResult = Resolution.sd,
            Description = "MediaInfo Reads Resolution If Not Forced")]
        [TestCase(
            Resolution.sd,
            ExpectedResult = Resolution.sd,
            Description = "Disregards MediaInfo if forced")]
        [TestCase(
            Resolution.hd,
            ExpectedResult = Resolution.hd,
            Description = "Disregards MediaInfo if forced")]
        public Resolution MediaInfoReadResultionIfNotPresent(Resolution value)
        {
            sut.ForceResolution = value;
            sut.Validate(mockMediaInfoFacade.Object, mockTimeProvider.Object);
            return sut.Resolution;
        }

        [Test]
        public void ValidateThrowsCorrectOrderException()
        {
            sut.FilePath = "\\\\Invalid\\path\\name.dif";
            var ex = Assert.Throws<OrderException>(() => sut.Validate(mockMediaInfoFacade.Object, mockTimeProvider.Object));
            Assert.That(ex.Message, Is.EqualTo("Filepath does not exist."));
            sut.FilePath = "\\\\ondnas01\\MediaCache\\Test\\detkommernaermere.dif";

            sut.BurnInSubtitles = true;
            ex = Assert.Throws<OrderException>(() => sut.Validate(mockMediaInfoFacade.Object, mockTimeProvider.Object));
            Assert.That(ex.Message, Is.EqualTo("SubtitlesPath does not exist."));

            sut.SubtitlesPath = "\\\\ondnas01\\MediaCache\\Test\\detkommernaermere.dif";
            ex = Assert.Throws<OrderException>(() => sut.Validate(mockMediaInfoFacade.Object, mockTimeProvider.Object));
            Assert.That(ex.Message, Does.StartWith("SubtitlesPath is not a supported file type. Must be"));
            sut.BurnInSubtitles = false;

            sut.DestinationFormat = StateFormat.unknown;
            ex = Assert.Throws<OrderException>(() => sut.Validate(mockMediaInfoFacade.Object, mockTimeProvider.Object));
            Assert.That(ex.Message, Is.EqualTo("DestinationFormat is invalid."));
            sut.DestinationFormat = StateFormat.xd5c;

            sut.DestinationPath = "\\\\Invalid\\path";
            ex = Assert.Throws<OrderException>(() => sut.Validate(mockMediaInfoFacade.Object, mockTimeProvider.Object));
            Assert.That(ex.Message, Is.EqualTo("DestinationPath does not exist."));
            sut.DestinationPath = "\\\\ondnas01\\MediaCache\\Test\\WfsJob\\Marvin";

            sut.CallbackUrl = "InvalidCallbackUrl/blabla";
            ex = Assert.Throws<OrderException>(() => sut.Validate(mockMediaInfoFacade.Object, mockTimeProvider.Object));
            Assert.That(ex.Message, Is.EqualTo("Callback URL is not valid"));
            sut.CallbackUrl = "http://valid/url";
            sut.DestinationFilename = "illegal filename \\";
            ex = Assert.Throws<OrderException>(() => sut.Validate(mockMediaInfoFacade.Object, mockTimeProvider.Object));
            Assert.That(ex.Message, Is.EqualTo($"{sut.DestinationFilename} contains illigal character: \\"));
            sut.DestinationFilename = "destinationFilename";

            sut.LogoPath = sut.LogoPath + "invalid";
            ex = Assert.Throws<OrderException>(() => sut.Validate(mockMediaInfoFacade.Object, mockTimeProvider.Object));
            Assert.That(ex.Message, Is.EqualTo("LogoPath does not exist."));
            sut.LogoPath = sut.LogoPath.Replace("invalid", "");

            sut.LogoPath = @"\\ondnas01\MediaCache\Test\readme.txt";
            ex = Assert.Throws<OrderException>(() => sut.Validate(mockMediaInfoFacade.Object, mockTimeProvider.Object));
            Assert.That(ex.Message, Is.EqualTo("LogoPath is not a supported file type. Must be png/jpg/jpeg"));
            sut.BurnInLogo = false;

            sut.AlternateAudioPath = "\\\\Invalid\\path\\name.wav";
            ex = Assert.Throws<OrderException>(() => sut.Validate(mockMediaInfoFacade.Object, mockTimeProvider.Object));
            Assert.That(ex.Message, Is.EqualTo("AlternateAudioPath does not exist."));

            sut.AlternateAudioPath = "\\\\ondnas01\\MediaCache\\Test\\pepe30.png";
            ex = Assert.Throws<OrderException>(() => sut.Validate(mockMediaInfoFacade.Object, mockTimeProvider.Object));
            Assert.That(ex.Message, Is.EqualTo("AlternateAudioPath is not a supported file type. Must be wav format"));
            sut.AlternateAudioPath = string.Empty;

            #pragma warning disable 612
            sut.DestinationFormat = StateFormat.h264_od_q1;
            #pragma warning restore 612
            ex = Assert.Throws<OrderException>(() => sut.Validate(mockMediaInfoFacade.Object, mockTimeProvider.Object));
            Assert.That(ex.Message, Does.StartWith("DestinationFormat is obsolete."));
            sut.DestinationFormat = StateFormat.h264_od_single;
        }

        [Test]
        public void MediaInfoValidateThrowsCorrectOrderException()
        {
            Exception e = new Exception("Something is wrong with file");
            mockMediaInfoFacade.Setup(m => m.Read(It.IsAny<string>())).Throws(e);
            var ex = Assert.Throws<OrderException>(() => sut.Validate(mockMediaInfoFacade.Object, mockTimeProvider.Object));
            Assert.That(ex.Message, Is.EqualTo("Error while reading metadata from file. " + e.Message));
        }

        [Test]
        public void UnknownFormatFromMediaInfoSetsFormatToCustom()
        {
            _videoMediaFileMetadata.Video.CodecId = "UnknownFormatCodecId";
            sut.Validate(mockMediaInfoFacade.Object, mockTimeProvider.Object);
            Assert.That(sut.Format, Is.EqualTo(StateFormat.custom));
            Assert.That(sut.CustomFormat, Is.EqualTo(_videoMediaFileMetadata.Video.CodecId));
        }

        [Test]
        public void NullResponseFromMediaInfoThrowsCorrectOrderException()
        {
            _videoMediaFileMetadata = null;
            mockMediaInfoFacade.Setup(m => m.Read(It.Is<string>(x => !x.EndsWith("wav")))).Returns(_videoMediaFileMetadata);
            var ex = Assert.Throws<OrderException>(() => sut.Validate(mockMediaInfoFacade.Object, mockTimeProvider.Object));
            Assert.That(ex.Message, Is.EqualTo("File is not a valid media file. Source file must be dv5p or xd5c for video or wma, mpeg or pcm for audio format or wav for alternate audio."));
        }

        [Test]
        public void UnsupportedAspectRatioWithForceValueThrowsCorrectOrderException()
        {
            _videoMediaFileMetadata.Video.DisplayAspectRatioRawValue = 21f / 3f;
            var ex = Assert.Throws<OrderException>(() => sut.Validate(mockMediaInfoFacade.Object, mockTimeProvider.Object));
            Assert.That(ex.Message, Is.EqualTo("Unsupport aspect ratio. Only 16x9 and 4x3 is supported."));
        }

        [Test]
        public void CustomFormatTest()
        {
            _videoMediaFileMetadata.Video.CodecId = "custom42";
            sut.Validate(mockMediaInfoFacade.Object, mockTimeProvider.Object);
            Assert.That(sut.Format, Is.EqualTo(StateFormat.custom));
            Assert.That(sut.CustomFormat, Is.EqualTo("custom42"));
        }

        [Test]
        public void UnsupportedChannelsFromMediaInfoSetChannelToOne()
        {
            _videoMediaFileMetadata.Audio.Channel = "1";
            var ex = Assert.Throws<OrderException>(() => sut.Validate(mockMediaInfoFacade.Object, mockTimeProvider.Object));
            Assert.That(ex.Message, Is.EqualTo("Only audio with more than 1 channel is supported."));
        }

        [Test]
        [TestCase(false, false,TestName = "niether")]
        [TestCase(true, false, TestName = "intro")]
        [TestCase(false, true, TestName = "outro")]
        [TestCase(true, true, TestName = "both")]
        [TestCase(true, false, 1, TestName = "invalid path intro")]
        [TestCase(false, true, 1, TestName = "invalid path outro")]
        [TestCase(true, true, 1, TestName = "both invalid path")]
        [TestCase(true, false, 2, TestName = "invalid type intro")]
        [TestCase(false, true, 2, TestName = "invalid type outro")]
        [TestCase(true, true, 2, TestName = "both invalid type")]
        public void OrderValidationStichingTest(bool withIntro, bool withOutro, int errorTest = 0)
        {
            string testPath;
            switch (errorTest)
            {
                case 0:
                    testPath = sut.AlternateAudioPath;
                    break;
                case 1:
                    testPath = "g:\\lol.wav";
                    break;
                case 2:
                    testPath = sut.AlternateAudioPath.Replace(".wav", ".docx");
                    break;
                default:
                    throw new Exception("Unsupport error test");
            }

            if (withIntro)
                sut.IntroFilePath = testPath;

            if (withOutro)
                sut.OutroFilePath = testPath;

            if (errorTest > 0)
            {
                var ex = Assert.Throws(typeof(OrderException),
                    () => sut.Validate(mockMediaInfoFacade.Object, mockTimeProvider.Object));
                Assert.That(ex.Message.Contains("tro")); // intro or outro
                return;
            }

            sut.Validate(mockMediaInfoFacade.Object, mockTimeProvider.Object);

            Assert.That(sut.Validated, Is.True);
        }
    }
}

