using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DR.Marvin.Model;
using DR.Marvin.Planner;
using DR.Marvin.Plugins.Dummy;
using DR.Marvin.Logging;

namespace DR.Marvin.Simulator
{
    class Program
    {
        private static Job CreateNewJob(DateTime issued, DateTime dueDate)
        {
            return new Job
            {
                Id = Guid.NewGuid(),
                Source = new Essence
                {
                    Files = new List<EssenceFile> { "foo" } ,
                    Flags = StateFlags.None,
                    Format = StateFormat.dv5p,
                    Path = "C:\\Temp\\"
                },
                Destination = new Essence
                {
                    Flags = StateFlags.HardSubtitles | StateFlags.Logo,
                    Format = StateFormat.h264_od_standard,
                    Path = "C:\\Output\\"
                },
                DueDate = dueDate,
                Issued = issued
            };
        }
        static void Main(string[] args)
        {
            AutoMapperHelper.EnsureInitialization();
            VirtualTimeProvider tp = new VirtualTimeProvider(new DateTime(2001, 01, 01));
            var logging = new Log4NetLogging();
            var jr = new InMemoryJobRepository(tp);
            var sr = new InMemorySemaphoreRepository(tp);
            var cr = new InMemoryCommandRepository();

            jr.Add(CreateNewJob(tp.GetUtcNow(), tp.GetUtcNow().AddDays(1)));
            jr.Add(CreateNewJob(tp.GetUtcNow(), tp.GetUtcNow().AddDays(1)));
            jr.Add(CreateNewJob(tp.GetUtcNow(), tp.GetUtcNow().AddDays(2)));
            jr.Add(CreateNewJob(tp.GetUtcNow(), tp.GetUtcNow()));
            jr.Add(CreateNewJob(tp.GetUtcNow(), tp.GetUtcNow()));
            jr.Add(CreateNewJob(tp.GetUtcNow(), tp.GetUtcNow()));
            var plugins = new List<IPlugin>
            {
                new DummyLogoPP("urn:dr:marvin:plugin:dummylogopp:1",tp, logging),
                new DummyLogoPP("urn:dr:marvin:plugin:dummylogopp:2",tp, logging),
                new DummyTCOnly("urn:dr:marvin:plugin:dummytconly:1",tp, logging)
            };

            var jobCountBefore = jr.WaitingJobs().Count();
            foreach (var waitingJob in jr.WaitingJobs())
            {
                Console.WriteLine($"{waitingJob.Urn} : {waitingJob.Source.Format} >> {waitingJob.Destination.Format}");
            }
            var exe = new Executor.Executor(jr, sr, cr, new DummyPPPlanner(plugins, jr, logging), tp, plugins, new DummyCallbackService(), logging);
            while (jr.WaitingJobs().Any() || jr.ActiveJobs().Any())
            {
                Tick(tp.GetUtcNow());
                exe.Pulse();
                tp.Step(TimeSpan.FromSeconds(5));
            }
            Debug.Assert(jr.DoneJobs().Count() == 6);
            foreach (var doneJob in jr.DoneJobs())
            {
                Console.WriteLine($"{doneJob.Urn} :\n\t Due {doneJob.DueDate:u},\n\t done {doneJob.EndTime:u}");
            }
            Console.WriteLine("Press any key to exit.");
            logging.LogDebug("Simulator done");
            Console.ReadKey();
        }

        static void Tick(DateTime now)
        {
            Debug.WriteLine($"===== Tick 1 {now}");
        }
    }
}
