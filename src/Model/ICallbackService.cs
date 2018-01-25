namespace DR.Marvin.Model
{
    /// <summary>
    /// Callback service used when the transcoding job is done or failed
    /// </summary>
    public interface ICallbackService
    {
        /// <summary>
        /// Make callback to the url provided with the order for the transcoding job.
        /// </summary>
        /// <param name="job">Job that is done or failed</param>
        void MakeCallback(Job job);
    }
}