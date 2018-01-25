using DR.Marvin.Model;
using DR.Marvin.Repositories.Test;
using NUnit.Framework;

namespace DR.Marvin.Simulator.Test
{
    [TestFixture]
    public class InMemoryJobRepositoryTest : CommonJobRepositoryTest
    {
        private InMemoryJobRepository _inMemoryJobRepository;

        public override void OneTimeSetUp()
        {
            AutoMapperHelper.EnsureInitialization();
            JobRepository = _inMemoryJobRepository = new InMemoryJobRepository(TimeProvider);
        }
        public override void SetUp()
        {
            _inMemoryJobRepository.Reset();
            TimeProvider.Reset();
        }
    }
}
