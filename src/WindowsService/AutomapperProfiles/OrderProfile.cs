using System;
using AutoMapper;
using DR.Marvin.Model;
using DR.Marvin.WindowsService.AutomapperHelper;
using DR.Marvin.WindowsService.Model;

namespace DR.Marvin.WindowsService.AutomapperProfiles
{
    /// <summary>
    /// Automapper configuration for order view model.
    /// </summary>
    public class OrderProfile : Profile
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public OrderProfile()
        {
            CreateMap<Order, Job>()
                .BeforeMap((src,dest) =>
                {
                    if(!src.Validated)
                        throw new Exception("Unvalidated order");
                })
                .ForMember(a => a.Destination, b => b.MapFrom(c => MappingHelper.GetDestination(c)))
                .ForMember(a => a.Source, b => b.MapFrom(c => MappingHelper.GetSource(c)))
                .ForMember(a => a.DueDate, b => b.MapFrom(c => c.DueDate.Value))
                .ForMember(a => a.Priority, b => b.MapFrom(c => c.Priority.Value))
                .ForMember(a => a.Issued, b => b.MapFrom(c => c.Issued))
                .ForMember(a => a.Plan, b => b.Ignore())
                .ForMember(a => a.EndTime, b => b.Ignore())
                .ForMember(a => a.LastModified, b => b.Ignore())
                .ForMember(a => a.Id, b => b.Ignore())
                ;
        }
    }
}