using System;
using System.Collections.Generic;
using System.Linq;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("DR.Marvin.Repositories")]
namespace DR.Marvin.Model
{
    /// <summary>
    /// The plan for a job, an order collection of tasks and their current state. 
    /// </summary>
    public class ExecutionPlan
    {
        private const string UrnPrefix = "urn:dr:marvin:plan:";
        
        /// <summary>
        /// Uniform Resource Name
        /// </summary>
        public string Urn { get; internal set; }

        /// <summary>
        /// Ordered list of tasks in the plan. If the list is empty , the plan is considdered canceled.
        /// </summary>
        public IList<ExecutionTask> Tasks { get; set; }

        /// <summary>
        /// The current active task. Sort of an IEnumrator.Current() method, but with to much logic.
        /// </summary>
        /// <returns>Current task, depends on the ActiveTaskIndex  property</returns>
        public ExecutionTask GetCurrentTask()
        {
            if (GetState() == ExecutionState.Done)
                return null;

            if (!ActiveTaskIndex.HasValue)
                MoveToNextTask();

            if (GetState() == ExecutionState.Done)
                return null;

            if (GetState() == ExecutionState.Canceled)
                return null;

            if (!ActiveTaskIndex.HasValue)
                throw new ExecutionPlanException($"Fatal error in {nameof(MoveToNextTask)} method.");

            return Tasks[ActiveTaskIndex.Value];
        }

        /// <summary>
        /// Increment ActiveTaskIndex, if the current task is Done. Sort of an IEnumrator.MoveNext() method.
        /// </summary>
        public void MoveToNextTask()
        {
            if (GetState() == ExecutionState.Done)
                throw new ExecutionPlanException("Plan already completly done.");

            if (!ActiveTaskIndex.HasValue)
                ActiveTaskIndex = 0;
            else
            {
                if (GetCurrentTask().State == ExecutionState.Done)
                {
                    ActiveTaskIndex++;
                    if (ActiveTaskIndex.Value == Tasks.Count)
                    {
                        ActiveTaskIndex = null;
                        return;
                    }
                    GetCurrentTask().From = Tasks[ActiveTaskIndex.Value-1].To; // Previous task's to essence.
                }
            }
        }

        /// <summary>
        /// Index of current active task. May be null if the plan has not started (first call to MoveToNextTask() or GetCurrentEssence()).
        /// </summary>
        public int? ActiveTaskIndex { get; internal set; }

        /// <summary>
        /// Returns the the current state of the jobs essence. Starts as source essence, ends as the destination essence.
        /// </summary>
        /// <returns>The last Done tasks "To"-essence. The plan hasn't started the the first tasks "From"-essence is returned.</returns>
        public Essence GetCurrentEssence()
        {
            if (!ActiveTaskIndex.HasValue)
                return GetState() == ExecutionState.Done ? Tasks.Last().To : Tasks.First().From;

            var id = ActiveTaskIndex.Value - 1;
            return id >= 0 ? Tasks[id].To : Tasks.First().From;
        }

        /// <summary>
        /// Calculate the overall state of the entire plan, depends on the state of the Tasks. 
        /// </summary>
        /// <returns>The plans overall state.</returns>
        public virtual ExecutionState GetState()
        {
            if (Tasks == null || Tasks.Count == 0)
                return ExecutionState.Canceled;
            if (Tasks.Any(t => t.State == ExecutionState.Failed))
                return ExecutionState.Failed;
            if (Tasks.Any(t => t.State == ExecutionState.Canceled))
                return ExecutionState.Canceled;
            if (Tasks.All(t => t.State == ExecutionState.Queued))
                return ExecutionState.Queued;
            if (Tasks.All(t => t.State == ExecutionState.Done) && !(ActiveTaskIndex.HasValue))
                return ExecutionState.Done;
            if (Tasks.Any(t => t.State == ExecutionState.Running) || ActiveTaskIndex.HasValue)
                return ExecutionState.Running;
            throw new ExecutionPlanException("Invalid state: ExecutionStates not exhausted.");
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public ExecutionPlan()
        {
            Urn = UrnPrefix + Guid.NewGuid();
        }

    }
    /// <summary>
    /// Exception from the plans internal method, this is most likely a fatal exception coused by some sort of logic error of data corption.
    /// </summary>
    [Serializable]
    public class ExecutionPlanException : Exception
    {
        /// <inheritdoc />
        public ExecutionPlanException(string message) : base (message) { }
    }
}
