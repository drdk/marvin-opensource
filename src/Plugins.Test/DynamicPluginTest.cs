using System;
using System.Linq;
using DR.Marvin.Plugins.Common;
using NUnit.Framework;

namespace DR.Marvin.Plugins.Test
{
    public abstract class DynamicPluginTest : CommonPluginTest
    {
        protected abstract DynamicPlugin[] SetupMockForDynmaicSlotsTest(int numberOfWorkingMachines);

        [Test]
        [TestCase(0, 1, ExpectedResult = new[] { false, true, true })]
        [TestCase(0, 2, ExpectedResult = new[] { false, false, true })]
        [TestCase(0, 3, ExpectedResult = new[] { false, false, false })]

        [TestCase(1, 1, ExpectedResult = new[] { true, true, true })]
        [TestCase(1, 2, ExpectedResult = new[] { true, false, true })]
        [TestCase(1, 3, ExpectedResult = new[] { true, false, false })]

        [TestCase(3, 0, ExpectedResult = new[] { true, true, true })]
        public bool[] TestDynamicSlots(int numberOfTasks, int numberOfWorkingMachines)
        {
            var list = SetupMockForDynmaicSlotsTest(numberOfWorkingMachines);
            foreach (var p in list.Take(numberOfTasks))
            {
                Task.PluginUrn = p.Urn;
                p.Assign(Task);
            }
            foreach (var p in list)
            {
                Console.WriteLine($"{p.Urn} Busy : {p.Busy}, Has Task : {p.HasTask}");
            }
            return list.Select(p => p.Busy).ToArray();
        }
    }
}
