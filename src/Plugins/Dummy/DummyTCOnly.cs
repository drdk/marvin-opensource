using System;
using System.Diagnostics;
using System.Linq;
using DR.Marvin.Model;
using DR.Marvin.Plugins.Common;

namespace DR.Marvin.Plugins.Dummy
{
    /// <summary>
    /// Testing plugin can only transcode, in 50 sec 
    /// </summary>
    public class DummyTCOnly : PluginBase
    {
        public static readonly string Type = nameof(DummyTCOnly).ToLower();
        public static string UrnPrefix => $"{UrnHelper.UrnBase}{PluginBaseUrn}{Type}:";
        public override string PluginType => Type;
        public DummyTCOnly(string urn, ITimeProvider timeProvider, ILogging logging) : base(urn, Type, timeProvider, logging)
        {
            
        }

        public override bool CheckAndEstimate(ExecutionTask task)
        {
            InternalTaskCheck(task);

            if (task.To.Files != null && task.To.Files.Count != 0)
                return false;

            if (task.To.Flags != task.From.Flags)
                return false;

            task.Estimation = TimeSpan.FromSeconds(50);
            return true;
        }

        public override void Assign(ExecutionTask task)
        {
            base.Assign(task);
            Debug.WriteLine($"{Urn} > {CurrentTask.Urn} assinged.");
        }



        protected override void DoWork()
        {
            CurrentTask.State = ExecutionState.Running;
           
            if (CurrentTask.StartTime.Value + CurrentTask.Estimation > TimeProvider.GetUtcNow())
                return;

            var from = CurrentTask.From;
            var to = CurrentTask.To;
            
            to.Files = new[] { "_1", "_2", "_3" }.Select(postfix => (EssenceFile) (from.Files.First().Value + postfix)).ToList();

            if (to.Attachments?.Count == 0)
                to.Attachments = from.Attachments;
            if (string.IsNullOrEmpty(to.Path))
                to.Path = from.Path;
            to.Flags |= from.Flags;
            
         
            CurrentTask.EndTime = TimeProvider.GetUtcNow();
            CurrentTask.State = ExecutionState.Done;
            Debug.WriteLine($"{Urn} > {CurrentTask.Urn} done @ {CurrentTask.EndTime:s}.");
        }
    }
}
