using System.Collections.Generic;
using DR.Marvin.Model;
using DR.Marvin.Planner;
using DR.Marvin.Plugins.Dummy;
using NUnit.Framework;

namespace DR.Marvin.Executor.Test
{
    [TestFixture]
    public class PPExecutorTest : CommonExecutorTest
    {
        [OneTimeSetUp]
        public override void OneTimeSetUp()
        {
            AutoMapperHelper.EnsureInitialization();
            var plugins = new List<IPlugin>
            {
                new DummyLogoPP($"{DummyLogoPP.UrnPrefix}1",TimeProvider, Logging),
                new DummyLogoPP($"{DummyLogoPP.UrnPrefix}2",TimeProvider, Logging),
                new DummyTCOnly($"{DummyTCOnly.UrnPrefix}1",TimeProvider, Logging)
            };
            Executor = new Executor(JobRepository, SemaphoreRepository, CommandRepository, new DummyPPPlanner(plugins, JobRepository, Logging), TimeProvider, plugins, MockCallbackService.Object, Logging);
        }
    }
}