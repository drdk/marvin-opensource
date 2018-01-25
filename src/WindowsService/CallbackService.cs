using DR.Common.RESTClient;
using DR.Marvin.Model;
using JetBrains.Annotations;

namespace DR.Marvin.WindowsService
{
    /// <summary>
    /// Standard json callback provider
    /// </summary>
    [UsedImplicitly]
    public class CallbackService : ICallbackService
    {
        private readonly IJsonClient _jsonClient;

        /// <summary>
        /// Constructor
        /// </summary>
        public CallbackService(IJsonClient jsonClient)
        {
            _jsonClient = jsonClient;
        }

        /// <summary>
        /// Make callback to the url provided with the order for the transcoding job.
        /// </summary>
        /// <param name="job">Job that are done or failed</param>
        public void MakeCallback(Job job)
        {
            var status = AutoMapper.Mapper.Map<Model.JobStatus>(job);
            _jsonClient.Post(job.CallbackUrl, status);
        }
    }
}
