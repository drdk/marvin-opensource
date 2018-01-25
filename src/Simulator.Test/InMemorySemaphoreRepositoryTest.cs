using DR.Marvin.Model;
using DR.Marvin.Repositories.Test;
using NUnit.Framework;

namespace DR.Marvin.Simulator.Test
{
    [TestFixture]
    public class InMemorySemaphoreRepositoryTest: CommonSemaphoreRepositoryTest
    {
        private InMemorySemaphoreRepository _inMemorySemaphoreRepository;
        public override void OneTimeSetUp()
        {
            AutoMapperHelper.EnsureInitialization();
            SemaphoreRepository = _inMemorySemaphoreRepository = new InMemorySemaphoreRepository(TimeProvider);
        }

        public override void SetUp()
        {
            _inMemorySemaphoreRepository.Reset();
        }
    }
}
