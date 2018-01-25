using DR.Marvin.Model;
using DR.Marvin.Repositories.Test;
using NUnit.Framework;

namespace DR.Marvin.Simulator.Test
{
    [TestFixture]
    public class InMemoryCommandRepositoryTest : CommonCommandRepositoryTest
    {
        private InMemoryCommandRepository _inMemoryCommandRepository;

        public override void OneTimeSetUp()
        {
            AutoMapperHelper.EnsureInitialization();
            CommandRepository = _inMemoryCommandRepository = new InMemoryCommandRepository();
        }
        public override void SetUp()
        {
            _inMemoryCommandRepository.Reset();
        }

        [TearDown]
        public void TearDown()
        {
            _inMemoryCommandRepository.Reset();
        }
    }
}
