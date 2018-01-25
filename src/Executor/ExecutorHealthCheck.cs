using DR.Common.Monitoring.Models;
using DR.Marvin.Model;

namespace DR.Marvin.Executor
{
    public class ExecutorHealthCheck : CommonHealthCheck
    {
        private readonly IExecutor _executor;
        public ExecutorHealthCheck(IExecutor executor)
        {
            _executor = executor;
        }

        public override string Name => "ExecutorStatus";
        protected override bool? RunTest(ref string message)
        {
            message = $"Node is {(_executor.GetIsPrimary() ? "primary" : "secondary")}";
            return null;
        }
}
}
