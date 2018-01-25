using AutoMapper;
using DR.Marvin.Model;
// ReSharper disable ClassNeverInstantiated.Global

namespace DR.Marvin.Repositories.AutomapperProfiles
{
    public class HealthCounterMappingProfile : Profile
    {
        public HealthCounterMappingProfile()

        {
            // depends on UtcTimeConverter from JobRepoMappingProfile.

            CreateMap<healthCounter, HealthCounter>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.id))
                .ForMember(dest => dest.Count, opt => opt.MapFrom(src => src.count))
                .ForMember(dest => dest.Message, opt => opt.MapFrom(src => src.message))
                .ForMember(dest => dest.TimeStamp, opt => opt.MapFrom(src => src.timestamp))
                ;
        }
    }
}
// ReSharper restore ClassNeverInstantiated.Global

