using AutoMapper;
using MediaInfoDotNet;

namespace DR.Marvin.MediaInfoService
{
    public class MediaInfoFacade : IMediaInfoFacade
    {
        public MediaInfoResult Read(string path)
        {
            var res = new MediaFile(path);
            return Mapper.Map<MediaInfoResult>(res);
        }
    }
}
