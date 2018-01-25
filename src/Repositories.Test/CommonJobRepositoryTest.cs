using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DR.Marvin.Model;
using DR.Marvin.Simulator;
using NUnit.Framework;

namespace DR.Marvin.Repositories.Test
{
    public abstract class CommonJobRepositoryTest
    {
        protected readonly VirtualTimeProvider TimeProvider = new VirtualTimeProvider(new DateTime(2001, 1, 1));
        protected IJobRepository JobRepository;

        [OneTimeSetUp]
        public abstract void OneTimeSetUp();

        [SetUp]
        public abstract void SetUp();

        [Test]
        public void InitTest()
        {
            Assert.That(JobRepository.ActiveJobs(), Is.Empty);
            Assert.That(JobRepository.DoneJobs(), Is.Empty);
            Assert.That(JobRepository.WaitingJobs(), Is.Empty);
        }

        [Test]
        public void KeyNotFoundTest()
        {
            Assert.Throws<KeyNotFoundException>(() => JobRepository.Get("urn:dr:marvin:job:00000000-0000-0000-0000-000000000000"));
        }

        [Test]
        public void AddWaitingTest()
        {
            JobRepository.Add(NewJob());
            Assert.That(JobRepository.WaitingJobs().Count(), Is.EqualTo(1));
            Assert.That(JobRepository.DoneJobs(), Is.Empty);
            Assert.That(JobRepository.ActiveJobs(), Is.Empty);
        }

        [Test]
        public void AddCancelTest()
        {
            var job = ActiveJob();
            job.Plan.GetCurrentTask().State = ExecutionState.Canceled;
            JobRepository.Add(job);
            Assert.That(JobRepository.WaitingJobs(), Is.Empty);
            Assert.That(JobRepository.DoneJobs(), Is.Empty);
            Assert.That(JobRepository.ActiveJobs(), Is.Empty);
            Assert.That(JobRepository.CanceledJobs().Count(), Is.EqualTo(1));
        }

        [Test]
        public void UpdateModifiesLastModifiedDateTest()
        {
            var job = ActiveJob();
            JobRepository.Add(job); //Sets the initial lastmodified timestamp
            job = JobRepository.Get(job.Urn);
            var createdTime = job.LastModified;
            TimeProvider.Step(TimeSpan.FromMinutes(1));
            JobRepository.Update(job);
            var updatedJob = JobRepository.Get(job.Urn);
            Assert.That(updatedJob.LastModified, Is.EqualTo(createdTime.AddMinutes(1)));
        }

        [Test]
        public void GetAndUpdateTest()
        {
            var originJob = ActiveJob();
            JobRepository.Add(originJob);
            Assert.That(JobRepository.ActiveJobs().Count(), Is.EqualTo(1));
            Assert.That(JobRepository.DoneJobs(), Is.Empty);
            Assert.That(JobRepository.WaitingJobs(), Is.Empty);
            var jobFromRepo = JobRepository.Get(originJob.Urn);
            jobFromRepo.Plan.GetCurrentTask().State = ExecutionState.Done;
            jobFromRepo.Plan.MoveToNextTask();
            JobRepository.Update(jobFromRepo);
            Assert.That(JobRepository.DoneJobs().Count(), Is.EqualTo(1));
            Assert.That(JobRepository.ActiveJobs(), Is.Empty);
            Assert.That(JobRepository.WaitingJobs(), Is.Empty);
            jobFromRepo = JobRepository.Get(originJob.Urn);
            // Check that the object has been cloned in the repo. 
            Assert.That(jobFromRepo.Plan, Is.Not.SameAs(originJob.Plan));
            Assert.That(jobFromRepo.Plan.Tasks[0], Is.Not.SameAs(originJob.Plan.Tasks[0]));
            Assert.That(jobFromRepo.Plan.Tasks[0].From, Is.Not.SameAs(originJob.Plan.Tasks[0].From));
            Assert.That(jobFromRepo.Plan.Tasks[0].From.Files[0], Is.Not.SameAs(originJob.Plan.Tasks[0].From.Files[0]));
            Assert.That(jobFromRepo.Plan.Tasks[0].Arguments, Is.Not.SameAs(originJob.Plan.Tasks[0].Arguments));
            Assert.That(jobFromRepo.Plan.Tasks[0].Urn, Is.EqualTo(originJob.Plan.Tasks[0].Urn));
            Assert.That(jobFromRepo.Plan.Tasks[0].NumberOfRetries, Is.EqualTo(originJob.Plan.Tasks[0].NumberOfRetries));
            Assert.That(jobFromRepo.Plan.Tasks[0].From.Files[0].Value, Is.EqualTo(originJob.Plan.Tasks[0].From.Files[0].Value));
            Assert.That(jobFromRepo.Plan.Tasks[0].Arguments["foo"], Is.EqualTo(originJob.Plan.Tasks[0].Arguments["foo"]));
            Assert.That(originJob.Plan.GetState(), Is.EqualTo(ExecutionState.Running));
            Assert.That(jobFromRepo.Plan.GetState(), Is.EqualTo(ExecutionState.Done));
            Assert.That(jobFromRepo.Plan.Tasks[0].From.AspectRatio, Is.EqualTo(originJob.Plan.Tasks[0].From.AspectRatio));
            Assert.That(jobFromRepo.Plan.Tasks[0].From.Resolution, Is.EqualTo(originJob.Plan.Tasks[0].From.Resolution));
            Assert.That(jobFromRepo.Plan.Tasks[0].From.Duration, Is.EqualTo(originJob.Plan.Tasks[0].From.Duration));
            Assert.That(jobFromRepo.Source.CustomFormat, Is.EqualTo(originJob.Source.CustomFormat));
            
        }


        [Test]
        public void GetAndUpdateTwoTasksTest()
        {
            var originJob = ActiveJobTwoTasks();
            JobRepository.Add(originJob);
            Assert.That(JobRepository.ActiveJobs().Count(), Is.EqualTo(1));
            Assert.That(JobRepository.DoneJobs(), Is.Empty);
            Assert.That(JobRepository.WaitingJobs(), Is.Empty);
            var jobFromRepo = JobRepository.Get(originJob.Urn);
            jobFromRepo.Plan.GetCurrentTask().State = ExecutionState.Done;
            jobFromRepo.Plan.MoveToNextTask();
            JobRepository.Update(jobFromRepo);
            jobFromRepo = JobRepository.Get(originJob.Urn);
            jobFromRepo.Plan.GetCurrentTask().State = ExecutionState.Done;
            jobFromRepo.Plan.MoveToNextTask();
            JobRepository.Update(jobFromRepo);
            Assert.That(JobRepository.DoneJobs().Count(), Is.EqualTo(1));
            Assert.That(JobRepository.ActiveJobs(), Is.Empty);
            Assert.That(JobRepository.WaitingJobs(), Is.Empty);
            jobFromRepo = JobRepository.Get(originJob.Urn);
            // Check that the object has been cloned in the repo. 
            Assert.That(jobFromRepo.Plan, Is.Not.EqualTo(originJob.Plan));
            Assert.That(jobFromRepo.Plan.Tasks[0], Is.Not.EqualTo(originJob.Plan.Tasks[0]));
            Assert.That(jobFromRepo.Plan.Tasks[0].From, Is.Not.EqualTo(originJob.Plan.Tasks[0].From));
            Assert.That(jobFromRepo.Plan.Tasks[0].Urn, Is.EqualTo(originJob.Plan.Tasks[0].Urn));
            Assert.That(originJob.Plan.GetState(), Is.EqualTo(ExecutionState.Running));
            Assert.That(jobFromRepo.Plan.GetState(), Is.EqualTo(ExecutionState.Done));
        }
        [Test]
        public void PlanWithMultipleExecutionTasksPersistsTheCorrectOrder()
        {
            var job = ActiveJobWithXtasks(4);
            job.Plan.Tasks[0].Id = Guid.Parse("C2843612-ECC4-42D0-BDF8-861B51E96544");
            job.Plan.Tasks[1].Id = Guid.Parse("C2843612-ECC4-42D0-BDF8-861B51E96543");
            job.Plan.Tasks[2].Id = Guid.Parse("C2843612-ECC4-42D0-BDF8-861B51E96542");
            job.Plan.Tasks[3].Id = Guid.Parse("C2843612-ECC4-42D0-BDF8-861B51E96541");
            JobRepository.Add(job);
            var jobFromRepo = JobRepository.Get(job.Urn);
            //Check if it adds correctly
            Assert.That(jobFromRepo.Plan.Tasks[0].Id, Is.EqualTo(job.Plan.Tasks[0].Id));
            Assert.That(jobFromRepo.Plan.Tasks[1].Id, Is.EqualTo(job.Plan.Tasks[1].Id));
            Assert.That(jobFromRepo.Plan.Tasks[2].Id, Is.EqualTo(job.Plan.Tasks[2].Id));
            Assert.That(jobFromRepo.Plan.Tasks[3].Id, Is.EqualTo(job.Plan.Tasks[3].Id));
            //Shuffle order and update
            var temp0 = jobFromRepo.Plan.Tasks[0];
            var temp1 = jobFromRepo.Plan.Tasks[1];
            var temp2 = jobFromRepo.Plan.Tasks[2];
            var temp3 = jobFromRepo.Plan.Tasks[3];
            jobFromRepo.Plan.Tasks[0] = temp3;
            jobFromRepo.Plan.Tasks[1] = temp0;
            jobFromRepo.Plan.Tasks[2] = temp1;
            jobFromRepo.Plan.Tasks[3] = temp2;
            JobRepository.Update(jobFromRepo);
            var updatedJobFromRepo = JobRepository.Get(jobFromRepo.Urn);
            //verify order
            Assert.That(updatedJobFromRepo.Plan.Tasks[0].Id, Is.EqualTo(job.Plan.Tasks[3].Id));
            Assert.That(updatedJobFromRepo.Plan.Tasks[1].Id, Is.EqualTo(job.Plan.Tasks[0].Id));
            Assert.That(updatedJobFromRepo.Plan.Tasks[2].Id, Is.EqualTo(job.Plan.Tasks[1].Id));
            Assert.That(updatedJobFromRepo.Plan.Tasks[3].Id, Is.EqualTo(job.Plan.Tasks[2].Id));
        }

        [Test]
        public void FilenameSortOrderPersisted()
        {
            var job = NewJob();
            JobRepository.Add(job);
            var repoJob = JobRepository.Get(job.Urn);
            Assert.That(repoJob.Destination.Files[0].Value, Is.EqualTo(job.Destination.Files[0].Value));
            Assert.That(repoJob.Destination.Files[1].Value, Is.EqualTo(job.Destination.Files[1].Value));
            Assert.That(repoJob.Destination.Files[2].Value, Is.EqualTo(job.Destination.Files[2].Value));
            var temp0 = repoJob.Destination.Files[0];
            var temp1 = repoJob.Destination.Files[1];
            var temp2 = repoJob.Destination.Files[2];
            repoJob.Destination.Files[0] = temp1;
            repoJob.Destination.Files[1] = temp2;
            repoJob.Destination.Files[2] = temp0;
            JobRepository.Update(repoJob);
            var updatedRepo = JobRepository.Get(job.Urn);
            Assert.That(updatedRepo.Destination.Files[0].Value, Is.EqualTo(job.Destination.Files[1].Value));
            Assert.That(updatedRepo.Destination.Files[1].Value, Is.EqualTo(job.Destination.Files[2].Value));
            Assert.That(updatedRepo.Destination.Files[2].Value, Is.EqualTo(job.Destination.Files[0].Value));
        }

        [Test]
        public void EmptyPlanTestReturnsCanceledStatus()
        {
            var job = NewJob();
            job.Plan = new ExecutionPlan();
            JobRepository.Add(job);
            var repoJob = JobRepository.Get(job.Urn);
            Assert.That(repoJob.Plan.GetState(), Is.EqualTo(ExecutionState.Canceled));
        }

        [Test]
        public void ChangeArrayLenghths()
        {
            var job = NewJob();
            JobRepository.Add(job);
            var repoJob = JobRepository.Get(job.Urn);
            Assert.That(repoJob.Destination.Files.Count, Is.EqualTo(3));
            Assert.That(repoJob.Destination.Attachments.Count, Is.EqualTo(1));
            repoJob.Destination.Files.Add("filename4");
            repoJob.Destination.Attachments.Add(new Attachment { Path = "c:\\Path", Arguments = new Dictionary<string, string> {{"foo2","bar2"}}, Type = AttachmentType.Subtitle });
            JobRepository.Update(repoJob);
            var afterUpdateRepoJob = JobRepository.Get(job.Urn);
            Assert.That(afterUpdateRepoJob.Destination.Files.Count, Is.EqualTo(4));
            Assert.That(afterUpdateRepoJob.Destination.Attachments.Count, Is.EqualTo(2));
            Assert.That(afterUpdateRepoJob.Destination.Files[3].Value, Is.EqualTo("filename4"));
            Assert.That(afterUpdateRepoJob.Destination.Files[3].Kind, Is.EqualTo(EssenceFileKind.Filename));
            Assert.That(afterUpdateRepoJob.Destination.Attachments.Count, Is.EqualTo(2));
            Assert.That(afterUpdateRepoJob.Destination.Attachments[1].Arguments["foo2"], Is.EqualTo("bar2"));
            Assert.That(afterUpdateRepoJob.Destination.Attachments[1].Type, Is.EqualTo(AttachmentType.Subtitle));
            afterUpdateRepoJob.Destination.Files.RemoveAt(3);
            afterUpdateRepoJob.Destination.Attachments.RemoveAt(1);
            JobRepository.Update(afterUpdateRepoJob);
            var afterDeleteRepoJob = JobRepository.Get(job.Urn);
            Assert.That(afterDeleteRepoJob.Destination.Files.Count, Is.EqualTo(3));
            Assert.That(afterDeleteRepoJob.Destination.Attachments.Count, Is.EqualTo(1));
            Assert.That(afterDeleteRepoJob.Destination.Files.All(f=>f.Value!="filename4"));
            Assert.That(afterDeleteRepoJob.Destination.Attachments.All(f => !f.Arguments.ContainsKey("foo2")));
        }

        private const int ThreadLoops = 2000;
        [Test,Explicit("Takes almost a minute to run on sql.")]
        public void ThreadSafetyTest()
        {
            var job = ActiveJob();
            JobRepository.Add(job);
            //Console.WriteLine(job.Plan.Tasks[0].From.Files[0]);
            var writeTask = Task.Run(() =>
            {
                for (var x = 0; x < ThreadLoops; x++)
                {
                    try
                    {
                        var dbJob = JobRepository.Get(job.Urn);
                        dbJob.Plan.Tasks[0].ForeignKey = $"{x}";
                        dbJob.Plan.Tasks[0].State = x%2 == 0 ? ExecutionState.Queued : ExecutionState.Running;
                        dbJob.Plan.Tasks[0].From.Files[0] = $"{x}.mov";
                        //Console.WriteLine("w > "+dbJob.Plan.Tasks[0].From.Files[0]);
                        JobRepository.Update(dbJob);
                        Thread.Sleep(0);
                    }
                    catch
                    {
                        Console.WriteLine($"Exception at write count : {x}");
                        throw;
                    }
                }
                Console.WriteLine($"Write loop done, count : {ThreadLoops} {DateTime.Now.ToLocalTime()}");
            });
            var readTask = Task.Run(() =>
            {
                for (var x = 0; x < ThreadLoops; x++)
                {
                    try
                    {
                        var dbJob = JobRepository.Get(job.Urn);
                        Assert.That(dbJob, Is.Not.Null);
                        //Console.WriteLine("r > " + dbJob.Plan.Tasks[0].From.Files[0]);
                        Thread.Sleep(0);
                    }
                    catch
                    {
                        Console.WriteLine($"Exception at read count : {x}");
                        throw;
                    }
                }
                Console.WriteLine($"Read loop done, count : {ThreadLoops} {DateTime.Now.ToLocalTime()}");
            });
            writeTask.Wait();
            readTask.Wait();
        }

        #region Job factories

        protected static Job NewJob()
        {
            return new Job
            {
                Id = Guid.NewGuid(),
                Source = new Essence
                {
                    Files = new List<EssenceFile> { "foo" },
                    Flags = StateFlags.None,
                    Format = StateFormat.dv5p,
                    AspectRatio = AspectRatio.ratio_16x9,
                    Resolution = Resolution.hd,
                    Duration = 42,
                    Path = "C:\\Temp\\",
                    CustomFormat = "customformatvalue"
                },
                Destination = new Essence
                {
                    Flags = StateFlags.HardSubtitles | StateFlags.Logo,
                    Format = StateFormat.h264_od_standard,
                    AspectRatio = AspectRatio.ratio_16x9,
                    Resolution = Resolution.hd,
                    Duration = 42,
                    Path = "C:\\Output\\",
                    Attachments = new List<Attachment> { new Attachment { Path = "c:\\Path", Arguments = new Dictionary<string, string> { { "foo", "bar" } }, Type = AttachmentType.Logo } },
                    Files = new List<EssenceFile> { "filename", "filename2", "filename3" },

                },
                DueDate = new DateTime(2001, 1, 1),
                Issued = new DateTime(2001, 1, 1)
            };
        }

        protected static Job ActiveJob()
        {
            var res = NewJob();
            res.Plan = new ExecutionPlan
            {
                Tasks = new List<ExecutionTask>
                {
                    new ExecutionTask
                    {
                        Estimation = TimeSpan.FromHours(1),
                        From = res.Source,
                        To = res.Destination,
                        PluginUrn = "dr:marvin:plugin:foo",
                        State = ExecutionState.Running,
                        EndTime = DateTime.UtcNow.AddHours(1),
                        StartTime = DateTime.UtcNow,
                        Arguments = new Dictionary<string, string> { {"foo","bar"} },
                        NumberOfRetries = 1
                    }
                }
            };
            res.Plan.MoveToNextTask();
            return res;
        }

        protected static Job ActiveJobTwoTasks()
        {
            var res = NewJob();
            res.Plan = new ExecutionPlan
            {
                Tasks = new List<ExecutionTask>
                {
                     new ExecutionTask
                    {
                        Estimation = TimeSpan.FromHours(1),
                        From = res.Source,
                        To = res.Source,
                        PluginUrn = "dr:marvin:plugin:foo",
                        State = ExecutionState.Running,
                        EndTime = DateTime.UtcNow.AddHours(1),
                        StartTime = DateTime.UtcNow
                    },
                    new ExecutionTask
                    {
                        Estimation = TimeSpan.FromHours(1),
                        From = res.Source,
                        To = res.Destination,
                        PluginUrn = "dr:marvin:plugin:foo",
                        State = ExecutionState.Running,
                        EndTime = DateTime.UtcNow.AddHours(1),
                        StartTime = DateTime.UtcNow
                    }
                }
            };
            res.Plan.MoveToNextTask();
            return res;
        }
        protected static Job ActiveJobWithXtasks(int taskcount)
        {
            var res = NewJob();
            res.Plan = new ExecutionPlan
            {
                Tasks = new List<ExecutionTask>()

            };
            for (int i = 0; i < taskcount; i++)
            {
                res.Plan.Tasks.Add(
                    new ExecutionTask
                    {
                        Estimation = TimeSpan.FromHours(1),
                        From = res.Source,
                        To = res.Destination,
                        PluginUrn = "dr:marvin:plugin:foo",
                        State = ExecutionState.Running,
                        EndTime = DateTime.UtcNow.AddHours(1),
                        StartTime = DateTime.UtcNow
                    });
            }
            res.Plan.MoveToNextTask();
            return res;
        }
        #endregion
    }
}
