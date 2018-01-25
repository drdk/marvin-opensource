using System;
using DR.Marvin.Logging;
using DR.Marvin.Model;
using DR.Marvin.Simulator;
using NUnit.Framework;

namespace DR.Marvin.Planner.Test
{
    public abstract class CommonPlannerTest
    {
        protected CommonPlannerTest()
        {
            JobRepository = new InMemoryJobRepository(TimeProvider);
            AutoMapperHelper.EnsureInitialization();
        }

        protected readonly VirtualTimeProvider TimeProvider = new VirtualTimeProvider(new DateTime(2001,1,1));
        protected readonly InMemoryJobRepository JobRepository;
        protected readonly ILogging Logging = new DummyLogging();
        
        [SetUp]
        public virtual void Setup()
        {
            TimeProvider.Reset();
            JobRepository.Reset();
        }
    }
}