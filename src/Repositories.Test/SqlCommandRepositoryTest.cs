using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DR.Marvin.Model;
using NUnit.Framework;

namespace DR.Marvin.Repositories.Test
{
    [TestFixture]
    [Parallelizable(ParallelScope.Self)]
    public class SqlCommandRepositoryTest : CommonCommandRepositoryTest
    {
        private SqlCommandRepository _sqlCommandRepository;
        public override void OneTimeSetUp()
        {
            AutoMapperHelper.EnsureInitialization();
            SqlConfigurationTestHelper.Configure();
            CommandRepository = _sqlCommandRepository = new SqlCommandRepository();
        }

        public override void SetUp()
        {
            _sqlCommandRepository.Reset();
        }

        [TearDown]
        public void TearDown()
        {
            _sqlCommandRepository.Reset();
        }
    }
}
