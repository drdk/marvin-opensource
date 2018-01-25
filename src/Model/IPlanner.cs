namespace DR.Marvin.Model
{
    /// <summary>
    /// Planning module interface.
    /// A IPlanner implementation must be able to create new ExecutionPlans, 
    /// modify existing plans. 
    /// </summary>
    public interface IPlanner
    {
        /// <summary>
        /// Modify the Jobs, by adding or modifying ExecutionPlans.
        /// </summary>
        void Calculate();
    }
}
