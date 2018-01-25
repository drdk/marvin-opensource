using System.IO;
using System.Linq;
using System.Reflection;
using DR.Marvin.Logging;
using DR.Marvin.Model;
using Moq;
using NUnit.Framework;

namespace DR.Marvin.Plugins.Test
{
    public abstract class CommonPluginTest
    {
        protected IPlugin Plugin;
        protected ExecutionTask Task;
        protected Mock<ITimeProvider> MockTimeProvider = new Mock<ITimeProvider>();
        protected ILogging Logging = new DummyLogging();

        [Test]
        public void ReassignTest()
        {
            Plugin.Assign(Task);
            Assert.That(Plugin.Busy, Is.True);
            var ex = Assert.Throws<PluginException>(() => Plugin.Reassign(Task));
            Assert.That(ex.Message,Is.EqualTo("Can not re-assign task to busy plugin."));
            var type = Plugin.GetType();
            var fieldInfo = type.GetField("CurrentTask", BindingFlags.Instance | BindingFlags.NonPublic);
            fieldInfo?.SetValue(Plugin, null);
            Assert.That(Plugin.Busy, Is.False);
            var tempStartTime = Task.StartTime;
            Task.StartTime = null;
            ex = Assert.Throws<PluginException>(() => Plugin.Reassign(Task));
            Assert.That(ex.Message, Is.EqualTo("Can not re-assign task that has not been started before."));
            Task.StartTime = tempStartTime;
            Plugin.Reassign(Task);
            Assert.That(Plugin.Busy, Is.True);
        }

        [Test]
        public void CleanUpTemporaryFilesTest()
        {
            var path = Directory.CreateDirectory($"C:\\temp\\temp-unittest-{Task.Id}\\");
            var file = File.Create($"{path.FullName}test.txt");
            Task.From.Path = path.FullName;
            Task.From.Files[0] = file.Name;
            Task.Arguments.Add("TemporaryEssence", nameof(Task.From));
            file.Close();
            Plugin.Assign(Task);
            Task.State = ExecutionState.Done;
            Plugin.Release(Task);
            Assert.That(Directory.Exists(path.FullName), Is.False);
        }

        [Test]
        public void DoNotCleanUpNonTemporaryFilesTest()
        {
            var path = Directory.CreateDirectory($"C:\\temp\\temp-unittest-{Task.Id}\\");
            var file = File.Create($"{path.FullName}test.txt");
            Task.From.Path = path.FullName;
            Task.From.Files[0] = file.Name;
            file.Close();
            Plugin.Assign(Task);
            Task.State = ExecutionState.Done;
            Plugin.Release(Task);
            Assert.That(Directory.Exists(path.FullName), Is.True);
            if (Directory.Exists(path.FullName))
                Directory.Delete(path.FullName, true);
        }
    }
}
