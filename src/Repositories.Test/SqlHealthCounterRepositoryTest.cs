using DR.Marvin.Model;
using NUnit.Framework;

namespace DR.Marvin.Repositories.Test
{
    [TestFixture]
    public class SqlHealthCounterRepositoryTest : CommonHealthCounterRepositoryTest
    {
        private SqlHealthCounterRepository _sqlHealthCounterRepository;
        public override void OneTimeSetUp()
        {
            AutoMapperHelper.EnsureInitialization();
            SqlConfigurationTestHelper.Configure();
            HealthCounterRepository = _sqlHealthCounterRepository = new SqlHealthCounterRepository(TimeProvider, MaxAge);
        }

        public override void SetUp()
        {
            TimeProvider.Reset();
            _sqlHealthCounterRepository.Reset();
        }
    }
}
