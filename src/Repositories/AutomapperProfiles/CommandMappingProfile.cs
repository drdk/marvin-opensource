using AutoMapper;
using DR.Marvin.Model;

namespace DR.Marvin.Repositories.AutomapperProfiles
{
    public class CommandMappingProfile : Profile
    {
        public CommandMappingProfile()
        {
            CreateMap<command, Command>()
                .ForMember(src => src.Type, opt => opt.MapFrom(dest => (CommandType) dest.type))
                .ForMember(src => src.Urn, opt => opt.MapFrom(dest => dest.urn))
                .ForMember(src => src.Username, opt => opt.MapFrom(dest => dest.username));

            CreateMap<Command, command>()
                .ForMember(src => src.type, opt => opt.MapFrom(dest => (int) dest.Type))
                .ForMember(src => src.urn, opt => opt.MapFrom(dest => dest.Urn))
                .ForMember(src => src.username, opt => opt.MapFrom(dest => dest.Username));
        }
    }
}
