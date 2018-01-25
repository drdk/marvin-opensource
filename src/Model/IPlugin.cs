using System;

namespace DR.Marvin.Model
{
    /// <summary>
    /// Common interface for plugins. Plugins in Marvin represent a given integration to a transcoder, preprocessor or similar.
    /// Multiple plugins for a given platform should be registered to support a given number of slots. Eg. if an WFS plugin is talking to 
    /// a platform with 3 nodes. 3 plugin should be instantiated, and a given instance should only use the we one node it represent. 
    /// </summary>
    public interface IPlugin
    {
        /// <summary>
        /// Returns lower case string of instance class name, used as part of the plugin urn, and for planners to match plugin time.
        /// Each plugin class should also implement at static string field containing the same value. See Plugins.Dummy for example.
        /// </summary>
        string PluginType { get; }

        /// <summary>
        /// Unique key, specific ExecutionTask is tied to a specific Plugin URN.
        /// </summary>
        string Urn { get; }
        
        /// <summary>
        /// Checks if the plugin can solve the given task. If it can solve the task, 
        /// the task is modified to include a for the time estimate for task if the plugin was available right now.
        /// </summary>
        /// <param name="task">The task to check and estimate :hourglass:.</param>
        /// <returns>True if the plug in can complete the given task.</returns>
        bool CheckAndEstimate(ExecutionTask task);
        
        /// <summary>
        /// Assings the plugin to the given task. 
        /// </summary>
        /// <exception cref="PluginException">If the plugin is already busy or if the CheckAndEstimate fails the task.</exception>
        /// <param name="task">Target task.</param>
        void Assign(ExecutionTask task);

        /// <summary>
        /// Re-assings the plugin to the given task. 
        /// </summary>
        /// <exception cref="PluginException">If the plugin is already busy or if the task has not been checked and estimated.</exception>
        /// <param name="task"></param>
        void Reassign(ExecutionTask task);

        /// <summary>
        /// Frees the plugin of a given task.
        /// </summary>
        /// <exception cref="PluginException">If another task is assigned to the plugin or if the task is still running.</exception>
        /// <param name="task">Task to release.</param>
        void Release(ExecutionTask task);
        
        /// <summary>
        /// Get plugin status. Include current task and progress
        /// </summary>
        /// <returns>Plugin status collection.</returns>
        PluginStatus GetStatus();
        
        /// <summary>
        /// Is the plugin available for new a task at the moment.
        /// </summary>
        bool Busy { get; }

        /// <summary>
        /// Is the operation asynchronously.
        /// False, if the plugin always completes any task synchronously, in a single Pulse.
        /// Otherwise true, if the operation is complete asynchronously, over multiple pulses.
        /// </summary>
        bool AsyncOperation { get; }

        /// <summary>
        /// URN of the current task if any, else null.
        /// </summary>
        string CurrentTaskUrn { get; }

        /// <summary>
        /// "Do work" method, update the state on the task. May change the ExecutionState on the task.
        /// </summary>
        /// <exception cref="PluginException">If not task is not assigned to the plugin.</exception>
        /// <param name="task">Task to update with new state.</param>
        void Pulse(ExecutionTask task);

        /// <summary>
        /// Optional method to retries a failed task. 
        /// A task is retried by setting the ExecutionState back to Running.
        /// If retry attempts exceeds RetryMax the job continues with the task in ExecutionState Failed
        /// </summary>
        /// <exception cref="PluginException">If not task is not assigned to the plugin or is not failed.</exception>
        /// <exception cref="NotImplementedException">If the plugin does not support retry.</exception>
        /// <param name="task">Target task to retry.</param>
        void Retry(ExecutionTask task);

        /// <summary>
        /// Returns true if Retry method is implemented. 
        /// </summary>
        bool CanRetry { get; }

        /// <summary>
        /// The max number of retry attempts. 
        /// </summary>
        int RetryMax { get; }

        /// <summary>
        /// Optional method to pauses a running task. Depends on optional Resume method.
        /// </summary>
        /// <exception cref="PluginException">If not task is not assigned to the plugin or is not running.</exception>
        /// <exception cref="NotImplementedException">If the plugin does not support pause or resume.</exception>
        /// <param name="task">Target task to pause.</param>
        void Pause(ExecutionTask task);

        /// <summary>
        /// Returns true if Pause and Resume methodes are implemented. 
        /// </summary>
        bool CanPause { get; }

        /// <summary>
        /// Optional method to resumes a running task. Depends on optional Pause method.
        /// </summary>
        /// <exception cref="PluginException">If not task is not assigned to the plugin or is not paused.</exception>
        /// <exception cref="NotImplementedException">If the plugin does not support pause or resume.</exception>
        /// <param name="task">Target task to resume.</param>
        void Resume(ExecutionTask task);

        /// <summary>
        /// Returns true if Pause and Resume methodes are implemented. Same value as CanPause.
        /// </summary>
        bool CanResume { get; }

        /// <summary>
        /// Optinal method to cancel a running task.
        /// </summary>
        /// <exception cref="PluginException">If not task is not assigned to the plugin or is not running.</exception>
        /// <exception cref="NotImplementedException">If the plugin does not support cancellation of tasks.</exception>
        /// <param name="task">Target task to cancel.</param>
        bool Cancel(ExecutionTask task);

        /// <summary>
        /// Returns true if the Cancel method is implemented.
        /// </summary>
        bool CanCancel { get; }

        /// <summary>
        /// Sets current task to null. Used by executor if the an slave is woken and takes the role as master.
        /// </summary>
        void Reset();
    }
}