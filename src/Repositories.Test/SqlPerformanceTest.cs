using System;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using DR.Marvin.Model;
using NUnit.Framework;
using DR.Marvin.Simulator;

namespace DR.Marvin.Repositories.Test
{
    [TestFixture (Explicit = true)]
    [Parallelizable(ParallelScope.None)]
    public class SqlPerformanceTest
    {

        private SqlJobRepository _sqlJobRepository;
        protected readonly VirtualTimeProvider _timeProvider = new VirtualTimeProvider(new DateTime(2001, 1, 1));

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            AutoMapperHelper.EnsureInitialization();
            var instance = System.Data.Entity.SqlServer.SqlProviderServices.Instance;
            typeof(ConfigurationElementCollection)
               .GetField("bReadOnly", BindingFlags.Instance | BindingFlags.NonPublic)
               ?.SetValue(ConfigurationManager.ConnectionStrings, false);

            if (ConfigurationManager.ConnectionStrings["MarvinEntities"] == null)
                ConfigurationManager.ConnectionStrings.Add(
                    new ConnectionStringSettings(
                        "MarvinEntities",
                        $"metadata=res://*/MarvinEntities.csdl|res://*/MarvinEntities.ssdl|res://*/MarvinEntities.msl;provider=System.Data.SqlClient;provider connection string='data source=localhost;initial catalog=MarvinDump;persist security info=True;MultipleActiveResultSets=True;integrated security=false;user id=nunit;password=test;App=EntityFramework'",
                        "System.Data.EntityClient"));
            else 
                throw new Exception("MarvinEntities already exists");

            _sqlJobRepository = new SqlJobRepository(_timeProvider);
        }
        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            ConfigurationManager.ConnectionStrings.Remove("MarvinEntities");
            if (ConfigurationManager.ConnectionStrings["MarvinEntities"] != null)
                throw new Exception("MarvinEntities was not cleared");
        }
        

        [Test]
        public void DummyTest()
        {
            var filter = DateTime.Parse("2016-09-18T22:24:56.500Z").AddDays(-7);
            var timer = new Stopwatch();
            timer.Start();
            var a = _sqlJobRepository.ActiveJobs().Count();
            var d = _sqlJobRepository.DoneJobs(filter).Count();
            var c = _sqlJobRepository.CanceledJobs(filter).Count();
            var f = _sqlJobRepository.FailedJobs(filter).Count();
            var w = _sqlJobRepository.WaitingJobs().Count();
            var n = _sqlJobRepository.GetNewest() != null ? 1:0;

            timer.Stop();
            Console.WriteLine($"a:{a} d:{d} c:{c} f:{f} w:{w} n:{n} timer : {timer.Elapsed:g}");
            var total = a + d + c + f + w + n;
            if(total!=0)
                Console.WriteLine($"Speed: {timer.ElapsedMilliseconds/(float)total:0.00} ms/entry");
        }


    }
}
