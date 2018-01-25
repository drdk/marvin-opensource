using System;
using DR.Marvin.Model;

namespace DR.Marvin.Simulator
{
    public class VirtualTimeProvider : ITimeProvider
    {

        private readonly DateTime _starTime;

        private DateTime _currentTime; 

        public VirtualTimeProvider(DateTime startTime)
        {
            _currentTime = _starTime = startTime.ToUniversalTime();
        }
        public DateTime GetUtcNow()
        {
            return _currentTime;
        }

        public void Step(TimeSpan stepSize)
        {
            if(stepSize <= TimeSpan.Zero)
                throw new Exception("Negative or zero step size not allowed.");
            _currentTime += stepSize;
        }

        /// <summary>
        /// Turn back time to initial time (defined at construction).
        /// </summary>
        public void Reset()
        {
            _currentTime = _starTime;
        }
    }
}
