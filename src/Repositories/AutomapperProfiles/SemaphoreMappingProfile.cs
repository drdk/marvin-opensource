using AutoMapper;
using DR.Marvin.Model;

namespace DR.Marvin.Repositories.AutomapperProfiles
{
    public class SemaphoreMappingProfile : Profile
    {
        public SemaphoreMappingProfile()
        {
            CreateMap<semaphore, Semaphore>();
        }
    }
}
