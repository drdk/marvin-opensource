using AutoMapper;

namespace DR.Marvin.Model.AutomapperProfiles
{
    /// <summary>
    /// AutoMapper profile for cloning of buisness models, need for simulator repos.
    /// </summary>
    public class ModelProfiles : Profile
    {
        /// <summary>
        /// Contructor
        /// </summary>
        public ModelProfiles()
        {
            CreateMap<Job, Job>();
            CreateMap<Essence, Essence>();
            CreateMap<EssenceFile, EssenceFile>()
                .ConstructUsing(src => new EssenceFile(src.Value, src.Kind));
            CreateMap<ExecutionPlan, ExecutionPlan>();
            CreateMap<ExecutionTask, ExecutionTask>();
            CreateMap<Command, Command>();
            CreateMap<Semaphore, Semaphore>();
        }
    }
}
