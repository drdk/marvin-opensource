using System.Linq;
using AutoMapper;
using MediaInfoDotNet;
using MediaInfoDotNet.Models;

namespace DR.Marvin.MediaInfoService
{
    /// <summary>
    /// Automapper configuration profile for media info mapping.
    /// </summary>
    public class MediaInfoResultProfile : Profile
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public MediaInfoResultProfile()
        {
            CreateMap<VideoStream, Video>()
                .ForMember(dest => dest.DisplayAspectRatioRawValue, opt => opt.MapFrom(src => src.DisplayAspectRatio))
                //fallback to format if codecid is missing. 
                .ForMember(dest => dest.CodecId, opt => opt.MapFrom(src => string.IsNullOrEmpty(src.CodecId) ? src.Format.ToLower() : src.CodecId))
                ;

            CreateMap<AudioStream, Audio>()
                .ForMember(dest => dest.Format, opt => opt.MapFrom(src => src.Format.ToLower().Replace(" ", "_")))
                .ForMember(dest => dest.Channel, opt => opt.MapFrom(src => src.Channels))
                ;

            CreateMap<MediaFile, MediaInfoResult>()
                .ForMember(dest => dest.Duration, opt => opt.MapFrom(src=>src.General.Duration))
                .ForMember(dest => dest.Video, opt => opt.ResolveUsing(src => src.Video?.FirstOrDefault()))
                .ForMember(dest => dest.Audio, opt => opt.ResolveUsing(src => src.Audio?.FirstOrDefault()))
                ;
        }
    }
}
