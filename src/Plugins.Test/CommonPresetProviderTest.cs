using System;
using System.Collections.Generic;
using System.Linq;
using DR.Marvin.Model;
using DR.Marvin.Plugins.Wfs;
using Moq;
using NUnit.Framework;

namespace DR.Marvin.Plugins.Test
{
    [TestFixture]
    public class CommonPresetProviderTest
    {
        Mock<CommonPresetProvider> mockPresetProvider = new Mock<CommonPresetProvider>() { CallBase = true };

        private static IEnumerable<TestCaseData> ValidationTestCases =>
            new List<TestCaseData>
            {
                new TestCaseData(
                    new List<WorkflowConfiguration>
                    {
                        new WorkflowConfiguration { AspectRatio = AspectRatio.ratio_16x9, BurnInLogo = false, Format = StateFormat.h264_od_standard, Workflow = Guid.NewGuid() }
                    }) { TestName = "One cfg"},
                new TestCaseData(
                    new List<WorkflowConfiguration>
                    {
                        new WorkflowConfiguration { AspectRatio = AspectRatio.ratio_16x9, BurnInLogo = false, Format = StateFormat.h264_od_standard, Workflow = Guid.NewGuid() },
                        new WorkflowConfiguration { AspectRatio = AspectRatio.ratio_16x9, BurnInLogo = true, Format = StateFormat.h264_od_standard, Workflow = Guid.NewGuid() }
                    }) { TestName = "Two cfgs"}
            };

        private static IEnumerable<TestCaseData> ExceptionTestCases =>
            new List<TestCaseData>
            {
                new TestCaseData(null, typeof(NullReferenceException)) { TestName = "null cfg"},
                new TestCaseData(new List<WorkflowConfiguration>(),typeof(PresetProviderException)) { TestName = "Empty cfg"},
                new TestCaseData(
                    new List<WorkflowConfiguration>
                    {
                        new WorkflowConfiguration { AspectRatio = AspectRatio.ratio_16x9, BurnInLogo = false, Format = StateFormat.h264_od_standard, Workflow = Guid.NewGuid() },
                        new WorkflowConfiguration { AspectRatio = AspectRatio.ratio_16x9, BurnInLogo = false, Format = StateFormat.h264_od_standard, Workflow = Guid.NewGuid() }
                    }, typeof(PresetProviderException)) { TestName = "Identical cfgs"},
                new TestCaseData(
                    new List<WorkflowConfiguration>
                    {
                        new WorkflowConfiguration { AspectRatio = AspectRatio.ratio_16x9, BurnInLogo = false, Format = StateFormat.h264_od_standard, Workflow = Guid.Parse("00000000-0000-0000-0000-000000000001") },
                        new WorkflowConfiguration { AspectRatio = AspectRatio.ratio_16x9, BurnInLogo = true, Format = StateFormat.h264_od_standard, Workflow = Guid.Parse("00000000-0000-0000-0000-000000000001") }
                    }, typeof(PresetProviderException)) { TestName = "Identical guid tagets"},
                new TestCaseData(
                    new List<WorkflowConfiguration>
                    {
                        new WorkflowConfiguration { AspectRatio = AspectRatio.ratio_16x9, BurnInLogo = false, Format = StateFormat.h264_od_standard, Workflow = Guid.Empty }
                    }, typeof(PresetProviderException)) { TestName = "Empty guid"}
                    ,
                new TestCaseData(
                    new List<WorkflowConfiguration>
                    {
                        new WorkflowConfiguration { AspectRatio = AspectRatio.unknown, BurnInLogo = false, Format = StateFormat.h264_od_standard, Workflow = Guid.NewGuid() }
                    }, typeof(PresetProviderException)) { TestName = "Invalid aspect ratio"},
                new TestCaseData(
                    new List<WorkflowConfiguration>
                    {
                        new WorkflowConfiguration { AspectRatio = AspectRatio.ratio_16x9, BurnInLogo = false, Format = StateFormat.unknown, Workflow = Guid.NewGuid() }
                    }, typeof(PresetProviderException)) { TestName = "Invalid format"}
            };


        [Test]
        [TestCaseSource(nameof(ValidationTestCases))]
        public void ValidationTest(List<WorkflowConfiguration> list)
        {
            mockPresetProvider.Setup(m => m.AsList()).Returns(list);
            var dir = mockPresetProvider.Object.AsDictionary();
            Assert.That(dir.Count, Is.EqualTo(list.Count));
            foreach (var entry in dir)
            {
                var tuple = entry.Key;
                Assert.That(list.Count(c =>
                c.Format == tuple.Item1 &&
                c.AspectRatio == tuple.Item2 &&
                c.Resolution == tuple.Item3 &&
                c.BurnInLogo == tuple.Item4 &&
                c.Workflow == entry.Value), Is.EqualTo(1));
            }
            Assert.That(dir.Count, Is.EqualTo(list.Count));
        }

        [Test]
        [TestCaseSource(nameof(ExceptionTestCases))]
        public void ExceptionTest(List<WorkflowConfiguration> list, Type exceptionType)
        {
            mockPresetProvider.Setup(m => m.AsList()).Returns(list);
            Assert.Throws(exceptionType, () => mockPresetProvider.Object.AsDictionary());
        }

        private static IEnumerable<TestCaseData> NeededCfgs =>
            (from format in new [] { StateFormat.h264_od_single, StateFormat.h264_od_standard, StateFormat.h264_od_dropfolder, StateFormat.h264_od_podcast }
             from logo in new [] { true, false }
             from res in new [] { Resolution.sd, Resolution.hd }
             from aspectRatio in new [] { AspectRatio.ratio_16x9, AspectRatio.ratio_4x3 }
             where aspectRatio != AspectRatio.ratio_4x3 || res != Resolution.hd
             select new TestCaseData(format, logo, res, aspectRatio))
            .Concat(new [] {
                new TestCaseData(StateFormat.h264_od_single, false, Resolution.fullhd, AspectRatio.ratio_16x9),
            });

        [Test]
        [TestCaseSource(nameof(NeededCfgs))]
        public void NewEmbedCfgTest(StateFormat format, bool logo, Resolution res, AspectRatio aspectRatio)
        {
            EmbedCfgTest(format, logo, res, aspectRatio);
        }


        private void EmbedCfgTest(StateFormat format, bool logo, Resolution res, AspectRatio aspectRatio)
        {
            var provider = new EmbededPresetProvider().AsDictionary();
            var key = new Tuple<StateFormat, AspectRatio, Resolution, bool>(format,aspectRatio,res,logo);
            Assert.That(provider.ContainsKey(key),Is.True);
            Assert.That(provider[key],Is.Not.EqualTo(Guid.Empty));
        }

        //TODO :Mark as explicite if we want to decouple prod
        [Test]
        [TestCaseSource(nameof(NeededCfgs))]
        public void TestWfsWorkflowConfiguration(StateFormat format, bool logo, Resolution res, AspectRatio aspectRatio)
        {
            var provider = new EmbededPresetProvider().AsDictionary();
            var key = new Tuple<StateFormat, AspectRatio, Resolution, bool>(format, aspectRatio, res, logo);
            var wfs = new WfsService.WfsService("http://wfsctrl01.net.dr.dk:8731/Xpress/SOAP");
            string formattedKey = $"{format}_{res.ToString()}_{aspectRatio.ToString().Substring(6)}{(logo ? "_logo" : "")}".ToLower();
            string workflowName = wfs.GetWorkflowName(provider[key]).ToLower();
            Assert.That(workflowName, Is.EqualTo(formattedKey));
        }
    }
}
