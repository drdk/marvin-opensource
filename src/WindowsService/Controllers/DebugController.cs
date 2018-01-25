using System.Web.Http;
using System.Web.Http.Description;
using DR.Marvin.Model;
using WebApi.OutputCache.V2;
#pragma warning disable 1591

namespace DR.Marvin.WindowsService.Controllers
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public class DebugController : ApiController
    {
        private readonly IJobRepository _jobRepository;
        public DebugController(IJobRepository jobRepository)
        {
            _jobRepository = jobRepository;
        }

        [HttpGet]
        [CacheOutput(ServerTimeSpan = 30, ClientTimeSpan = 30)]
        public Job GetJob(string urn)
        {
            return _jobRepository.Get(urn);
        }
    }
}
