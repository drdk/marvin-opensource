using System;

namespace DR.Marvin.Model
{
    /// <summary>
    /// The Executor module, is the single entry-point for planner and plugins. 
    /// </summary>
    public interface IExecutor : IDisposable
    {
        /// <summary>
        /// The Pulse method is excepted to be called by a given interval or if the hosting application 
        /// thinks a given change to the JobRepository will trigger a change in the Planner module. It also assigns 
        /// Tasks to the Plugins, and monitor progress of the tasks and active jobs. 
        /// </summary>
        void Pulse();

        /// <summary>
        /// Show the status of every active plugin. 
        /// </summary>
        /// <returns>Status collection of PluginStatues</returns>
        ExecutorStatus GetStatus();
        /// <summary>
        /// Checks if this instance got the semaphore and is primary. 
        /// </summary>
        /// <returns>true if primary.</returns>
        bool GetIsPrimary();
    }
}
