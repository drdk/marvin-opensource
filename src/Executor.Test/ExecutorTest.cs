using System;
using System.Collections.Generic;
using DR.Marvin.Model;
using DR.Marvin.Planner;
using DR.Marvin.Plugins.Dummy;
using NUnit.Framework;

namespace DR.Marvin.Executor.Test
{
    [TestFixture]
    public class ExecutorTest : CommonExecutorTest
    {
        [OneTimeSetUp]
        public override void OneTimeSetUp()
        {
            AutoMapperHelper.EnsureInitialization();
            var plugins = new List<IPlugin>
            {
                new Dummy($"{Dummy.UrnPrefix}1",TimeProvider, Logging),
                new Dummy($"{Dummy.UrnPrefix}2",TimeProvider, Logging)
            };

            Executor = new Executor(JobRepository, SemaphoreRepository, CommandRepository, new DummyPlanner(plugins, JobRepository, Logging), TimeProvider, plugins, MockCallbackService.Object, Logging);
        }

        [Test]
        public void PluginsWithNonUniqueUrnsTest()
        {
            var plugins = new List<IPlugin>
            {
                new Dummy($"{Dummy.UrnPrefix}1",TimeProvider, Logging),
                new Dummy($"{Dummy.UrnPrefix}1",TimeProvider, Logging)
            };
            Assert.Throws<ArgumentException>(() => 
                new Executor(JobRepository, SemaphoreRepository, CommandRepository, new DummyPlanner(plugins, JobRepository, Logging), TimeProvider, plugins, MockCallbackService.Object, Logging));
        }
    }
}