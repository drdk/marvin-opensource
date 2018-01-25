using System.Linq;
using AutoMapper;
using DR.Marvin.Model;
using DR.Marvin.Plugins.Common;
using DR.Marvin.WindowsService.AutomapperHelper;
using DR.Marvin.WindowsService.Model;
using StructureMap;

namespace DR.Marvin.WindowsService.AutomapperProfiles
{
    /// <summary>
    /// Automapper configuration profile for System Status view models
    /// </summary>
    public class StatusProfile : Profile
    {
        //TODO: Nicer way to do this? :question:
        private static ITimeProvider TimeProvider => ObjectFactory.GetInstance<ITimeProvider>();
        /// <summary>
        /// Constructor
        /// </summary>
        public StatusProfile()
        {
            CreateMap<Job, JobStatus>()
                .ForMember(dest => dest.JobUrn, opt => opt.MapFrom(src => src.Urn))
                .ForMember(dest => dest.State, opt => opt.ResolveUsing(src => src.Plan?.GetState() ?? ExecutionState.Queued))
                .ForMember(dest => dest.PercentDone, opt => opt.MapFrom(src => MappingHelper.CalculatePercentageDone(src, TimeProvider)))
                .ForMember(dest => dest.EstimatedDone, opt => opt.MapFrom(src => MappingHelper.CalculateEstimatedDone(src)))
                .ForMember(dest => dest.Started, opt => opt.ResolveUsing(src => src.Plan?.Tasks.FirstOrDefault()?.StartTime))
                .ForMember(dest => dest.Issued, opt => opt.MapFrom(src => src.Issued))
                .ForMember(dest => dest.EndTime, opt => opt.MapFrom(src => src.EndTime))
                ;

            CreateMap<Job, DashboardJob>()
                .ForMember(dest => dest.CurrentPluginUrn, opt => opt.ResolveUsing(src => src.Plan?.GetCurrentTask()?.PluginUrn))
                .ForMember(dest => dest.EstimatedDone, opt => opt.MapFrom(src => MappingHelper.CalculateEstimatedDone(src)))
                .ForMember(dest => dest.PercentDone, opt => opt.MapFrom(src => MappingHelper.CalculatePercentageDone(src, TimeProvider)))
                .ForMember(dest => dest.TaskPercentDone, opt => opt.MapFrom(src => MappingHelper.CalculateTaskPercentDone(src, TimeProvider)))
                .ForMember(dest => dest.State, opt => opt.ResolveUsing(src=> (src.Plan?.GetState() ?? ExecutionState.Queued).ToString()))
                .ForMember(dest => dest.Started, opt => opt.ResolveUsing(src => src.Plan?.Tasks.FirstOrDefault()?.StartTime))
                .ForMember(dest => dest.SourceFormat, opt => opt.MapFrom(src =>
                $"{src.Source.Format.ToString()}{(string.IsNullOrEmpty(src.Source.CustomFormat) ? string.Empty : $" ({src.Source.CustomFormat})")}"
                ))
                .ForMember(dest => dest.DestionationFormat, opt => opt.MapFrom(src => src.Destination.Format.ToString()))
                .ForMember(dest => dest.Duration, opt => opt.MapFrom(src => src.Source.Duration))
                ;

            CreateMap<PluginBase, DashbaordPlugin>()
                .ForMember(dest => dest.Urn, opt => opt.MapFrom(src => src.Urn))
                .ForMember(dest => dest.PluginType, opt => opt.MapFrom(src => src.PluginType))
                ;
        }
    }
}