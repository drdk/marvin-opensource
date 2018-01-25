using System;
using System.Net;
using System.Web.Http;
using AutoMapper;
using DR.Marvin.MediaInfoService;
using DR.Marvin.Model;
using DR.Marvin.WindowsService.Model;
using Swashbuckle.Swagger.Annotations;

namespace DR.Marvin.WindowsService.Controllers
{
    /// <summary>
    /// API class for the Central Transcoding Platform 
    /// </summary>
    public class JobController : ApiController
    {
        private readonly IJobRepository _jobRepository;
        private readonly ICommandRepository _commandRepository;
        private readonly ITimeProvider _timeProvider;
        private readonly IMediaInfoFacade _mediaInfoFacade;
        private readonly ILogging _logging;

        /// <summary>
        /// Constructor
        /// </summary>
        public JobController(IJobRepository jobRepository, ICommandRepository commandRepository, ITimeProvider timeProvider, IMediaInfoFacade mediaInfoFacade, ILogging logging)
        {
            _jobRepository = jobRepository;
            _commandRepository = commandRepository;
            _timeProvider = timeProvider;
            _mediaInfoFacade = mediaInfoFacade;
            _logging = logging;
        }

        /// <summary>
        /// Place order to DR's central transcoder platform
        /// </summary>
        /// <param name="order">Order for transcoding task</param>
        /// <returns>Unique Urn on order or error message if order is invalid</returns>
        /// <remarks>This is the content of the *remarks* xml field.</remarks>
        /// <exception cref="OrderException">Throws if order does not validate.</exception>
        [SwaggerResponse(HttpStatusCode.BadRequest,"Invalid order, error message.",typeof(Error))]
        [HttpPost]
        public JobStatus Order(Order order)
        {
            try
            {
                order.Validate(_mediaInfoFacade, _timeProvider);
            }
            catch
            {
                _logging.LogWarning("Failed to validate order", order);
                throw;
            }
            var job = Mapper.Map<Job>(order);
            if (order.Format == StateFormat.custom)
            {
                _logging.LogWarning($"Custom format received : {order.CustomFormat}", job.Urn);
            }
            _jobRepository.Add(job);
            _logging.LogInfo($"Received valid order.", job);
            return Mapper.Map<JobStatus>(job);
        }

        /// <summary>
        /// Add an Marvin job command to the queue if possible. Commands are executed asynchronously. 
        /// </summary>
        /// <param name="command">command message</param>
        /// <returns>Current job status.</returns>
        [HttpPost]
        public JobStatus Command(Command command)
        {
            // validate command
            if (string.IsNullOrEmpty(command.Urn) || !command.Urn.ValidateUrn("job:"))
                throw new ArgumentException("invalid urn", nameof(command));

            var job = _jobRepository.Get(command.Urn);

            if (job == null)
                throw new ArgumentException("Job not found", nameof(command));

            // ReSharper disable once SwitchStatementMissingSomeCases
            switch (job.Plan?.GetState())
            {
                case ExecutionState.Done:
                case ExecutionState.Failed:
                case ExecutionState.Canceled:
                    throw new Exception($"Can't issue command job already {job.Plan.GetState()}");
            }

            _commandRepository.Add(command);
            _logging.LogInfo("Received cancel event.", command);
            return Mapper.Map<JobStatus>(job);
        }
    }
}