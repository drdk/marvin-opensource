using System;
using System.IO;
using System.Linq;
using System.Text;
using DR.Marvin.Model;

namespace DR.Marvin.Plugins.Common
{
    public abstract class PluginBase : IPlugin
    {
        
        protected const string PluginBaseUrn = "plugin:";
        public abstract string PluginType { get; }
        public string Urn { get;  }
        protected ExecutionTask CurrentTask;
        protected readonly ITimeProvider TimeProvider;
        protected ILogging Logging;

        protected PluginBase(string urn, string pluginType, ITimeProvider timeProvider, ILogging logging)
        {
            if(timeProvider==null)
                throw new ArgumentNullException(nameof(timeProvider));
            if (string.IsNullOrEmpty(urn))
                throw new ArgumentNullException(nameof(urn),"Urn can't be null or empty");
            if(!urn.ValidateUrn($"{PluginBaseUrn}{pluginType}:"))
                throw new ArgumentException($"invalid urn value : {urn}",nameof(urn));
            if(logging==null)
                throw new ArgumentNullException(nameof(logging));
            Urn = urn;
            TimeProvider = timeProvider;
            Logging = logging;
        }

        protected void InternalTaskCheck(ExecutionTask task)
        {
            if(task == null)
                throw new ArgumentNullException(nameof(task),"Can not check null task");
            if (task.PluginUrn != Urn)
                throw new ArgumentException("Can not check task with mismatching plugin urn.");
        }
        public abstract bool CheckAndEstimate(ExecutionTask task);

        public virtual void Assign(ExecutionTask task)
        {
            if (CurrentTask != null)
                throw new PluginException("Can not assign task to busy plugin.");
            if(!CheckAndEstimate(task))
                throw new PluginException("Task cannot be solved by current plugin.");
            CurrentTask = task;
            task.StartTime = TimeProvider.GetUtcNow();
        }

        public virtual void Reassign(ExecutionTask task)
        {
            if (CurrentTask != null)
                throw new PluginException("Can not re-assign task to busy plugin.");
            if (!task.StartTime.HasValue)
                throw new PluginException("Can not re-assign task that has not been started before.");
            CurrentTask = task;
        }

        public void Release(ExecutionTask task)
        {
            if (task == null)
                throw new ArgumentNullException(nameof(task));
            if (CurrentTask != null)
            {
                if (CurrentTask.Urn != task.Urn)
                    throw new PluginException("taskUrn mismatch");
                if (CurrentTask.State == ExecutionState.Running)
                    throw new PluginException("Can not release running task.");
                CurrentTask = null;
            }

            if (task.State == ExecutionState.Done &&
                task.Arguments.ContainsKey("TemporaryEssence") && 
                task.Arguments["TemporaryEssence"] == "From")
            {
                try
                {
                    DeleteFilesInEssence(task.From);
                }
                catch (Exception e)
                {
                    Logging.LogException(e, "Unable to clean up temp files.", task.Urn);
                }
            }

            task.EndTime = TimeProvider.GetUtcNow();
        }

        public PluginStatus GetStatus()
        {
            return new PluginStatus(Urn,CurrentTask,Busy);
        }
      
        public virtual bool Busy => CurrentTask != null;
        public virtual bool AsyncOperation => true;
        public string CurrentTaskUrn => GetStatus().CurrentTask?.Urn;

        public void Pulse(ExecutionTask task)
        {
            ValidateAndUpdateTask(task);
            //TODO: move try catch and release here....
            DoWork();
        }

        private void DeleteFilesInEssence(Essence essence)
        {
            if (!Directory.Exists(essence.Path))
            {
                Logging.LogWarning(essence.Path + " already deleted.");
                return;
            }
            var sb = new StringBuilder("Deleting Tempory files and directory:\n");
            foreach (var file in essence.Files)
            {
                var absFineName = Path.Combine(essence.Path, file.Value);
                if (File.Exists(absFineName))
                {
                    File.Delete(absFineName);
                    sb.AppendLine(absFineName);
                }
            }

            if (!Directory.EnumerateFiles(essence.Path).Any()) //temp dir is empty, safe to delete
            {
                Directory.Delete(essence.Path);
                sb.AppendLine(essence.Path);
            }
            Logging.LogInfo(sb.ToString());
        }

        protected void ValidateAndUpdateTask(ExecutionTask task)
        {
            if (task == null)
                throw new ArgumentNullException(nameof(task), "can not be null");

            if (CurrentTask == null)
            {
                Reassign(task);
            }
            else
            {
                if (task.Urn != CurrentTaskUrn)
                    throw new PluginException("Task urn mismatch.");

                CurrentTask = task;
            }
        }

        public virtual void Retry(ExecutionTask task)
        {
            task.NumberOfRetries += 1;
            if (task.NumberOfRetries <= RetryMax)
            {                
                task.ForeignKey = null;
                task.State = ExecutionState.Running;
                task.StartTime = TimeProvider.GetUtcNow();
            }
        }

        public virtual bool CanRetry => false;
        public virtual int RetryMax => 3;
        public void Pause(ExecutionTask task) { throw new NotImplementedException(); }
        public virtual bool CanPause => false;
        public virtual void Resume(ExecutionTask task) { throw new NotImplementedException(); }
        public bool CanResume => CanPause; // can not resume without pause
        public virtual bool Cancel(ExecutionTask task) { throw new NotImplementedException(); }
        public virtual bool CanCancel => false;
        public void Reset()
        {
            CurrentTask = null;
        }

        protected abstract void DoWork();
    }
}
