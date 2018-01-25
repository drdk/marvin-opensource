using System.Data.Entity;
using System.Linq;

namespace DR.Marvin.Repositories.Helpers
{

    public static class Extensions
    {
        public static IQueryable<job> IncludeFullJob(this IQueryable<job> source)
        {
            return source
                .Include("executionplan.executiontask.execution_essence.essence.essencefile")
                .Include("executionplan.executiontask.execution_essence.essence.attachment")
                .Include("job_essence.essence.essencefile")
                .Include("job_essence.essence.attachment");
        }
    }
}
