using System.Linq;
using DR.Marvin.Model;
using NUnit.Framework;

namespace DR.Marvin.Repositories.Test
{
    [TestFixture]
    [Parallelizable(ParallelScope.None)]
    public class SqlJobRepositoryTest : CommonJobRepositoryTest
    {
        private SqlJobRepository _sqlJobRepository;

        public override void OneTimeSetUp()
        {
            AutoMapperHelper.EnsureInitialization();
            SqlConfigurationTestHelper.Configure();
            JobRepository = _sqlJobRepository = new SqlJobRepository(TimeProvider);
        }


        public override void SetUp()
        {
            _sqlJobRepository.Reset();
            TimeProvider.Reset();
        }

        [TearDown]
        public void TearDown()
        {
            _sqlJobRepository.Reset();
        }

        [Test]
        public void InstanceTest()
        {
            Assert.That(JobRepository, Is.Not.Null);
        }
        [Test]
        public void AddJobToDb()
        {
            var job = NewJob();
            _sqlJobRepository.Add(job);
        }
        [Test]
        public void AddWaitingJobToDb()
        {
            var job = ActiveJob();
            _sqlJobRepository.Add(job);
        }
        [Test]
        public void EssenceGetsCleanedOnUpdate()
        {
            var originJob = ActiveJob();
            int essenceAmount;
            JobRepository.Add(originJob);
            Assert.That(JobRepository.ActiveJobs().Count(), Is.EqualTo(1));
            Assert.That(JobRepository.DoneJobs(), Is.Empty);
            Assert.That(JobRepository.WaitingJobs(), Is.Empty);
            var jobFromRepo = JobRepository.Get(originJob.Urn);
            jobFromRepo.Plan.GetCurrentTask().State = ExecutionState.Done;
            jobFromRepo.Plan.MoveToNextTask();
            JobRepository.Update(jobFromRepo);
            using (var db = new MarvinEntities())
            {
                essenceAmount = db.essence.Count();
            }
            //There should only be four essencefiles (two job essence and two task essence)
            Assert.That(essenceAmount, Is.EqualTo(4));

        }

    }
}