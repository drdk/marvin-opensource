using System;
using System.Linq;
using DR.Marvin.Model;
using NUnit.Framework;

namespace DR.Marvin.Repositories.Test
{
    public abstract class CommonCommandRepositoryTest
    {
        protected ICommandRepository CommandRepository;

        [OneTimeSetUp]
        public abstract void OneTimeSetUp();

        [SetUp]
        public abstract void SetUp();

        [Test]
        public void InitTest()
        {
            Assert.That(CommandRepository.GetAll(), Is.Empty);
        }

        [Test]
        public void SimpleTest()
        {
            var cmd = new Command
            {
                Type = CommandType.Cancel,
                Urn = "Foo",
                Username = "Unittest"
            };
            CommandRepository.Add(cmd);
            var dbcmds = CommandRepository.GetAll().ToList();
            Assert.That(dbcmds.Count, Is.EqualTo(1));
            var dbcmd = dbcmds.First();
            Assert.That(dbcmd.Type, Is.EqualTo(cmd.Type));
            Assert.That(dbcmd.Urn, Is.EqualTo(cmd.Urn));
            Assert.That(dbcmd.Username, Is.EqualTo(cmd.Username));
            CommandRepository.Remove(cmd);
            Assert.That(CommandRepository.GetAll(), Is.Empty);
        }

        [Test]
        public void TwoCommandsTest()
        {
            
            CommandRepository.Add(new Command
            {
                Type = CommandType.Cancel,
                Urn = "Foo",
                Username = "Unittest"
            });
            CommandRepository.Add(new Command
            {
                Type = CommandType.Cancel,
                Urn = "Foo2",
                Username = "Unittest"
            });
            Assert.That(CommandRepository.GetAll().Count(), Is.EqualTo(2));
        }

        [Test]
        public void MultipleInsertsOfSameCommandTest()
        {

            var cmd = new Command
            {
                Type = CommandType.Cancel,
                Urn = "Foo",
                Username = "Unittest"
            };
            CommandRepository.Add(cmd);
            Assert.Throws(Is.AssignableTo(typeof(Exception)),()=> CommandRepository.Add(cmd));
        }
    }
}
