using System;
using DR.Marvin.Model;

namespace DR.Marvin.Simulator
{
    class DummyCallbackService : ICallbackService
    {
        public void MakeCallback(Job job)
        {
            Console.WriteLine($"Dummy callback to url: {job.CallbackUrl}");
        }
    }
}
