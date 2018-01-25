namespace DR.Marvin.Model
{
    /// <summary>
    /// State of a plan or a task.
    /// </summary>
    public enum ExecutionState
    {
        #pragma warning disable 1591
        Queued = 0,
        Running,
        Done,
        Failed,
        Canceled,
        Paused
        #pragma warning restore 1591
    }
}
