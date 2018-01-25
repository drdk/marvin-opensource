using System;
using System.Data.Entity;
using System.Threading;
using System.Threading.Tasks;
using DR.Marvin.Model;
using NUnit.Framework;

namespace DR.Marvin.Repositories.Test
{
    [TestFixture]
    [Parallelizable(ParallelScope.None)]
    public class SqlSemaphoreRepositoryTest :CommonSemaphoreRepositoryTest
    {
        private SqlSemaphoreRepository _sqlSemaphoreRepository;
        public override void OneTimeSetUp()
        {
            AutoMapperHelper.EnsureInitialization();
            SqlConfigurationTestHelper.Configure();
            SemaphoreRepository = _sqlSemaphoreRepository = new SqlSemaphoreRepository(TimeProvider);
        }

        public override void SetUp()
        {
            _sqlSemaphoreRepository.Reset();
        }

        [TearDown]
        public void TearDown()
        {
            _sqlSemaphoreRepository.Reset();
        }

        [Test]
        public void StoreWinsSemaphoreRaceCondition()
        {
            using (var db = new MarvinEntities())
            {
                var entry = db.semaphore.Add(new semaphore
                {
                    currentOwnerId = "owner1",
                    heartBeat = TimeProvider.GetUtcNow(),
                    semaphoreId = "testSemaphore"
                });
                db.SaveChanges();
                //If this fucks up, be sure that the semaphore rowversion property concurrencymode, is set to fixed!
                var edit1 = EditSemaphoreDelayedBy(1000, entry.semaphoreId, "MrSlow");
                var edit2 = EditSemaphoreDelayedBy(1, entry.semaphoreId, "MrFast");
                Assert.That(edit1.Result, Is.False);
                Assert.That(edit2.Result, Is.True);
                db.Entry(entry).Reload();
                db.semaphore.Remove(entry);
                db.SaveChanges();
            }
        }
        private static async Task<bool> EditSemaphoreDelayedBy(int delayTime, string seamaphoreId, string currentOwner)
        {
            using (var db = new MarvinEntities())
            {
                var semaphore = await db.semaphore.FirstOrDefaultAsync(a => a.semaphoreId == seamaphoreId);
                semaphore.currentOwnerId = currentOwner;
                Thread.Sleep(delayTime);
                try
                {
                    await db.SaveChangesAsync();
                }
                catch (Exception)
                {
                    return false;
                }
                return true;
            }
        }
    }
}
