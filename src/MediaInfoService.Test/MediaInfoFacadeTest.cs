using DR.Marvin.Model;
using NUnit.Framework;

namespace DR.Marvin.MediaInfoService.Test
{
    /// <summary>
    /// :warning: NB: Turn of shadow copying of nunit runner to run this test, else it will fail to load mediainfo.dll
    /// </summary>
    [TestFixture, Explicit]
    public class MediaInfoFacadeTest
    {
        private MediaInfoFacade _mediaInfoFacade;
        private const string TestRoot = @"\\isilontst\NLETST\XpressTest\testkit\";
        private const string File1Path =  TestRoot + "interview-xdcamhd-2min35sec.mov";
        private const string File2Path =  TestRoot + "sport-dvcp5-5min.mov";
        private const string File3Path =  TestRoot + "indslag-xdcamhd-49sec.mov";
        private const string File4Path =  TestRoot + "bamses-4x3.mov";
        private const string File5Path =  TestRoot + "indland-dvh5-10min59sec.mov";
        private const string File6Path =  TestRoot + "intro-avc1-30sec.mp4";
        private const string File7Path =  TestRoot + "deadline-dv-30min.dif";
                                          
        private const string File20Path = TestRoot + "audio_asf.asf";
        private const string File21Path = TestRoot + "audio_mp3.mp3";
        private const string File22Path = TestRoot + "audio_wav.wav";
        private const string File23Path = TestRoot + "audio_bwf.bwf";

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _mediaInfoFacade = new MediaInfoFacade();
            AutoMapperHelper.EnsureInitialization();
        }

        [Test]
        [TestCase(File1Path, DisplayAspectRatio.ratio_16x9)]
        [TestCase(File2Path, DisplayAspectRatio.ratio_16x9)]
        [TestCase(File3Path, DisplayAspectRatio.ratio_16x9)]
        [TestCase(File4Path, DisplayAspectRatio.ratio_4x3)]
        [TestCase(File5Path, DisplayAspectRatio.ratio_16x9)]
        public void AscpectRatioTest(string filePath, DisplayAspectRatio ratio)
        {
            var file = _mediaInfoFacade.Read(filePath);
            Assert.That(file.Video.DisplayAspectRatio, Is.EqualTo(ratio));
        }

        [Test]
        [TestCase(File1Path, "xd5c")]
        [TestCase(File2Path, "dv5p")]
        [TestCase(File3Path, "xd5c")]
        [TestCase(File4Path, "dv5p")]
        [TestCase(File5Path, "dvh5")]
        [TestCase(File6Path, "avc1")]
        [TestCase(File7Path, "dv")]
        public void VideoCodecIdTest(string filePath, string codecId)
        {
            var file = _mediaInfoFacade.Read(filePath);
            Assert.That(file.Video.CodecId, Is.EqualTo(codecId));
        }

        [TestCase(File20Path, "wma")]
        [TestCase(File21Path, "mpeg_audio")]
        [TestCase(File22Path, "mpeg_audio")]
        [TestCase(File23Path, "mpeg_audio")]
        public void AudioFormatTest(string filePath, string format)
        {
            var file = _mediaInfoFacade.Read(filePath);
            Assert.That(file.Audio.Format, Is.EqualTo(format));
        }
    }
}