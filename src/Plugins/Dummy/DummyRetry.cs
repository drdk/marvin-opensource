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
    public class DummyRetry : PluginBase
    {
        public static readonly string Type = nameof(DummyTCOnly).ToLower();
        public static string UrnPrefix => $"{UrnHelper.UrnBase}{PluginBaseUrn}{Type}:";
        public override string PluginType => Type;
        public DummyRetry(string urn, ITimeProvider timeProvider, ILogging logging) : base(urn, Type, timeProvider, logging)
        {
            
        }

        public override bool CheckAndEstimate(ExecutionTask task)
        {
            InternalTaskCheck(task);

            if (!task.From.CustomFormat.StartsWith("FailNumber"))
                return false;

            task.Estimation = TimeSpan.FromSeconds(15);
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

            int FailNumber = int.Parse(CurrentTask.From.CustomFormat.Substring(CurrentTask.From.CustomFormat.Length - 1));
            //CurrentTask.EndTime = TimeProvider.GetUtcNow();
            if (CurrentTask.NumberOfRetries < FailNumber)
            {
                CurrentTask.State = ExecutionState.Failed;
                Release(CurrentTask);
            }
            else
            {
                CurrentTask.State = ExecutionState.Done;
                Debug.WriteLine($"{Urn} > {CurrentTask.Urn} done @ {CurrentTask.EndTime:s}.");
            }
        }

        public override bool CanRetry => true;
    }
}
