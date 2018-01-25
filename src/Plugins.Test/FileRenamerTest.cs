using System;
using System.Collections.Generic;
using System.IO;
using DR.Marvin.Model;
using Moq;
using NUnit.Framework;
using NUnit.Framework.Internal;

namespace DR.Marvin.Plugins.Test
{
    [TestFixture]
    public class FileRenamerTest
    {
        private FileRenamer.FileRenamer sut;
        Mock<ITimeProvider> mockTimeProvider = new Mock<ITimeProvider>();
        Mock<ILogging> mockLogging = new Mock<ILogging>();
        private readonly string _pluginUrn = $"{FileRenamer.FileRenamer.UrnPrefix}unittest1";
        private const string TestRoot = @"C:\Temp\MarvinUnitTest\";

        [SetUp]
        public void SetUp()
        {
            sut = new FileRenamer.FileRenamer(_pluginUrn, mockTimeProvider.Object, mockLogging.Object);
        }

        [TestCase(0, "name_%index%.%ext%", false)]
        [TestCase(1, "name_%index%.%ext%", true)]
        [TestCase(2, "name_%index%.%ext%", false)]
        [TestCase(1, "name_%invlaid%.%ext%", false)]
        [TestCase(1, "name_no_template", false)]
        public void CheckAndEstimateReturnsCorrectBoolValue(int destinationFilesCount, string fileTemplate, bool expectedResult)
        {
            var et = new ExecutionTask
            {
                To = new Essence {Files = new List<EssenceFile>()}
            };
            
            for (var i = 0 ; i < destinationFilesCount ; i++)
                et.To.Files.Add(EssenceFile.Template(fileTemplate + i));

            Assert.That(sut.CheckAndEstimate(et), Is.EqualTo(expectedResult));
        }

        [Test]
        public void CheckAndEstimateSetsTaskEstimationToFiveSeconds()
        {
            var et = new ExecutionTask
            {
                To = new Essence { Files = new List<EssenceFile>() }
            };
            et.To.Files.Add(EssenceFile.Template("FileName.%ext%"));
            sut.CheckAndEstimate(et);

            Assert.That(et.Estimation, Is.EqualTo(TimeSpan.FromSeconds(5)));
        }

        [Test]
        public void DoWorkSetsStateCorrect()
        {
            string path = TestRoot;
            Directory.CreateDirectory(path);
            string name = "RenamerTestFile.mov";
            var tempPath = Path.Combine(path, name);
            var tempFile = File.Create(tempPath);
            tempFile.Close();
            
            var et = new ExecutionTask
            {
                To = new Essence { Files = new List<EssenceFile>() },
                From = new Essence { Files = new List<EssenceFile>() }
            };
            et.To.Files.Add(EssenceFile.Template("name_%index%.%ext%"));
            et.To.Path = path;
            et.From.Files.Add("RenamerTestFile.mov");
            et.From.Path = path;
            var targetPath1 = Path.Combine(path, "name_1.mov");
            if (File.Exists(targetPath1))
                File.Delete(targetPath1);

            sut.Assign(et);
            sut.Pulse(et);

            Assert.That(sut.GetStatus().CurrentTask.State, Is.EqualTo(ExecutionState.Done));

            sut.Pulse(et);

            // Do work sets state to failed if file target exists
            Assert.That(et.State, Is.EqualTo(ExecutionState.Failed));

            File.Delete(tempPath);
            File.Delete(targetPath1);
        }

        [Test]
        public void DoWorkMapsToCorrectDestinationFileNames()
        {
            string path = TestRoot;
            Directory.CreateDirectory(path);
            string name1 = "RenamerTestFile1.mov";
            string name2 = "RenamerTestFile2.mov";
            var tempFile = File.Create(Path.Combine(path, name1));
            tempFile.Close();
            var tempFile2 = File.Create(Path.Combine(path, name2));
            tempFile2.Close();
            var targetName1 = "name_1.mov";
            var targetName2 = "name_2.mov";
            var targetPath1 = Path.Combine(path, "name_1.mov");
            var targetPath2 = Path.Combine(path, "name_2.mov");
            if(File.Exists(targetPath1))
                File.Delete(targetPath1);
            if (File.Exists(targetPath2))
                File.Delete(targetPath2);

            var et = new ExecutionTask
            {
                To = new Essence { Files = new List<EssenceFile>() },
                From = new Essence { Files = new List<EssenceFile>() }
            };
            et.To.Files.Add(EssenceFile.Template("name_%index%.%ext%"));
            et.To.Path = path;
            et.From.Files.Add(name1);
            et.From.Files.Add(name2);
            et.From.Path = path;

            sut.Assign(et);
            sut.Pulse(et);

            Assert.That(et.To.Files[0].Value, Is.EqualTo(targetName1));
            Assert.That(et.To.Files[1].Value, Is.EqualTo(targetName2));

            File.Delete(targetPath1);
            File.Delete(targetPath2);
        }
    }
}