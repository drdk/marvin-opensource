using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DR.Marvin.Model;
using DR.Marvin.Plugins.Common;

namespace DR.Marvin.Plugins.Dummy
{
    /// <summary>
    /// Testing preprocessor plugin solves every flag task, in 10 sec 
    /// </summary>
    public class DummyLogoPP : PluginBase
    {
        public static readonly string Type = nameof(DummyLogoPP).ToLower();
        public override string PluginType => Type;
        public static string UrnPrefix => $"{UrnHelper.UrnBase}{PluginBaseUrn}{Type}:";
        public DummyLogoPP(string urn, ITimeProvider timeProvider, ILogging logging) : base(urn, Type, timeProvider, logging)
        {
            
        }

        public override bool CheckAndEstimate(ExecutionTask task)
        {
            InternalTaskCheck(task);
            if(task.To.Files != null && task.To.Files.Count != 0)
                throw new PluginException("Does not support output filenames.");
            if(task.To.Format != task.From.Format)
                throw new PluginException("Does not support trancoding.");
            task.Estimation = TimeSpan.FromSeconds(10);
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

            to.Files = from.Files;

            if (to.Attachments?.Count == 0)
                to.Attachments = from.Attachments;
            if (string.IsNullOrEmpty(to.Path))
                to.Path = from.Path;
            to.Flags |= from.Flags;
            
         
            CurrentTask.EndTime = TimeProvider.GetUtcNow();
            CurrentTask.State = ExecutionState.Done;
            Debug.WriteLine($"{Urn} > {CurrentTask.Urn} done @ {CurrentTask.EndTime:s}.");
        }

        public override bool Cancel(ExecutionTask task)
        {
            ValidateAndUpdateTask(task);
            CurrentTask.State = ExecutionState.Canceled;
            return true;
        }
        public override bool CanCancel => true;
    }
}
