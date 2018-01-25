using System;
using System.Collections.Generic;
using System.Linq;
using DR.Marvin.Model;

namespace DR.Marvin.Executor
{
    public sealed class Executor : IExecutor
    {
        private readonly IPlanner _planner;
        private readonly ITimeProvider _timeProvider;
        private readonly IList<IPlugin> _plugins;
        private readonly IJobRepository _jobRepository;
        private readonly ICallbackService _callbackService;
        private readonly ISemaphoreRepository _semaphoreRepository;
        private readonly ICommandRepository _commandRepository;
        private readonly ILogging _logging;
        private bool _firstPulse = true;
        private bool _wasMaster = false;

        public Executor(IJobRepository jobRepository, ISemaphoreRepository semaphoreRepository, ICommandRepository commandRepository, IPlanner planner, ITimeProvider timeProvider, IEnumerable<IPlugin> plugins, ICallbackService callbackService, ILogging logging )
        {
            _jobRepository = jobRepository;
            _semaphoreRepository = semaphoreRepository;
            _commandRepository = commandRepository;
            _callbackService = callbackService;
            _planner = planner;
            _timeProvider = timeProvider;
            _plugins = plugins.ToList();
            _logging = logging;
            if (_plugins.Select(p=>p.Urn).Distinct().Count() != _plugins.Count())
                throw new ArgumentException("Invalid plugin configuration, not unique urns detected", nameof(plugins));
        }

        public void Pulse()
        {
            lock (_jobRepository)
            {
                #region semaphore
                string currentOwner;
                var firstCallAfterSemaphore = _firstPulse || !_wasMaster;
                if (!_semaphoreRepository.Get(nameof(Executor), Utilities.GetCallerId(), out currentOwner))
                {
                    if (_firstPulse)
                    {
                        _logging.LogInfo($"Did not get semaphore, current owner : {currentOwner}. Will stand by as slave.");
                        _firstPulse = false;
                    }
                    else if (_wasMaster)
                    {
                        _logging.LogWarning($"Lost semaphore to : {currentOwner}. Will stand by as slave.");
                        _wasMaster = false;
                    }
                    return;
                }
                if (_firstPulse)
                {
                    _logging.LogInfo($"Got semaphore, as : {currentOwner}. Will work as master.");
                    _firstPulse = false;
                }
                else if (!_wasMaster)
                {
                    _logging.LogInfo($"Slave woken, {currentOwner} is now owner. Will work as master.");
                    foreach (var plugin in _plugins)
                    {
                        plugin.Reset();
                    }
                }
                _wasMaster = true;
                #endregion

                #region command
                foreach (var command in _commandRepository.GetAll())
                {
                    // ReSharper disable once SwitchStatementMissingSomeCases
                    switch (command.Type)
                    {
                        case CommandType.Cancel:
                            CancelJob(command.Urn, command.Username);
                            break;
                        default:
                            _logging.LogWarning($"Command state {command.Type} is not implemented.", command.Urn);
                            break;
                    }
                    _commandRepository.Remove(command);
                }
                #endregion

                #region planner
                // TODO: modify existing plans ? :hamburger: :+1:
                if (!firstCallAfterSemaphore) // skip first pulse to reassign in-progress tasks to plugins.
                    _planner.Calculate();
                #endregion

                #region jobs, task and plugins
                foreach (var job in _jobRepository.ActiveJobs().ToList())
                {
                    //TODO: Add support for cancel
                    
                    var plan = job.Plan;
                    startOfJobLoop:
                    var executionTask = plan.GetCurrentTask();
                    var targetPlugin = _plugins.First(p => p.Urn == executionTask.PluginUrn);
                    switch (executionTask.State)
                    {
                        case ExecutionState.Queued:
                            if (targetPlugin.Busy)
                                // TODO: log planning warning
                                break;
                            _logging.LogDebug($"Task {executionTask.Urn} assigned to {targetPlugin.Urn}.", job.Urn);
                            targetPlugin.Assign(executionTask);
                            goto case ExecutionState.Running;
                        case ExecutionState.Running:
                            targetPlugin.Pulse(executionTask);
                            if (executionTask.State == ExecutionState.Done)
                                goto case ExecutionState.Done;
                            if (executionTask.State == ExecutionState.Failed)
                                goto case ExecutionState.Failed;
                            plan.Tasks[plan.ActiveTaskIndex.Value] = executionTask;
                            break;
                        case ExecutionState.Done:
                            _logging.LogDebug($"Task {executionTask.Urn} done, released from {targetPlugin.Urn}.", job.Urn);
                            targetPlugin.Release(executionTask);
                            plan.Tasks[plan.ActiveTaskIndex.Value] = executionTask;
                            plan.MoveToNextTask();
                            if (plan.ActiveTaskIndex.HasValue) // has more tasks
                            {
                                _jobRepository.Update(job); // save and...
                                goto startOfJobLoop; //start next task at once
                            }
                            break;
                        case ExecutionState.Failed:
                            if (targetPlugin.CanRetry && executionTask.NumberOfRetries < targetPlugin.RetryMax)
                                targetPlugin.Retry(executionTask);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                    
                    if (plan.GetState() == ExecutionState.Done)
                    {
                        job.Destination = plan.GetCurrentEssence();
                        job.EndTime = _timeProvider.GetUtcNow();
                        _logging.LogInfo("Job done", job.Urn);
                    }

                    if(plan.GetState() == ExecutionState.Failed)
                        _logging.LogWarning("Job failed",job.Urn);

                    
                    if ((plan.GetState() == ExecutionState.Failed || plan.GetState() == ExecutionState.Done) && !string.IsNullOrEmpty(job.CallbackUrl))
                    {
                        MakeCallback(job);
                    }
                    _jobRepository.Update(job);
                }
            }
            #endregion

        }

        private void MakeCallback(Job job)
        {
            try
            {
                _callbackService.MakeCallback(job);
            }
            catch (Exception e)
            {
                _logging.LogException(e, $"Callback to  {job.CallbackUrl} failed", job.Urn);
            }
        }

        public ExecutorStatus GetStatus()
        {
            lock (_jobRepository)
            {
                return new ExecutorStatus {
                    PluginStatuses = _plugins.Select(p => p.GetStatus()).ToList(),
                    TimeStamp = _timeProvider.GetUtcNow(),
                    IsPrimary = _wasMaster
                };
            }
        }

        public void Dispose()
        {
            lock (_jobRepository)
            {
                _semaphoreRepository.Release(nameof(Executor), Utilities.GetCallerId());
            }
        }

        private void CancelJob(string urn, string username)
        {
            var job = _jobRepository.Get(urn);

            if (job == null)
            {
                _logging.LogWarning("Invalid command, job not found ", urn);
                return;
            }

            //If job is queued set empty plan
            if (job.Plan == null)
            {
                job.Plan = new ExecutionPlan();
            }
            else
            {
                var targetPlugin = _plugins.FirstOrDefault(p => p.Urn == job.Plan.GetCurrentTask()?.PluginUrn);
                if (targetPlugin == null)
                {
                    _logging.LogWarning($"Unable to cancel job. Job state was {job.Plan.GetState()}.", urn);
                    return;
                }
                if (targetPlugin.CanCancel)
                {
                    var task = job.Plan.GetCurrentTask();
                    targetPlugin.Cancel(task);
                    targetPlugin.Release(task);
                }
                else
                {
                    _logging.LogWarning(
                        $"Unable to cancel job, current plugin ({targetPlugin.PluginType}) doesn't support the cancel command.",
                        urn);
                    return;
                }
            }
            job.EndTime = _timeProvider.GetUtcNow();
            _jobRepository.Update(job);
            _logging.LogInfo($"Job Canceled by {username}", job.Urn);
            MakeCallback(job);
        }

        public bool GetIsPrimary()
        {
            return _wasMaster;
        }
    }
}
