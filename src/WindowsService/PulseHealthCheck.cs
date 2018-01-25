using System;
using System.Linq;
using DR.Common.Monitoring.Models;
using DR.Marvin.Model;
#pragma warning disable 1591

namespace DR.Marvin.WindowsService
{
    public class PulseHealthCheck : CommonHealthCheck
    {
        private readonly IHealthCounterRepository _healthCounterRepository;
        public PulseHealthCheck(IHealthCounterRepository healthCounterRepository)
        {
            if (healthCounterRepository == null)
                throw new ArgumentNullException(nameof(healthCounterRepository));
            _healthCounterRepository = healthCounterRepository;
        }
        public override string Name => "Pulse";
        protected override bool? RunTest(ref string message)
        {
            var entries = _healthCounterRepository.ProbeAndPrune().ToList();
            if (entries.Any())
            {
                message = $"The following pulse functions recently ({_healthCounterRepository.MaxAge}) encounter errors:";
                foreach (var healthCounter in entries)
                {
                    message +=
                        $"\n\"{healthCounter.Id}\" count : {healthCounter.Count} last timestamp : {healthCounter.TimeStamp.ToLocalTime()} last message : {healthCounter.Message}";
                }
                return false;
            }
            message = $"No pulse function have failed recently ({_healthCounterRepository.MaxAge}).";
            return true;
        }
    }
}
