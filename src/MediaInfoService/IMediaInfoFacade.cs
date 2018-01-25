using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediaInfoDotNet;

namespace DR.Marvin.MediaInfoService
{
    public interface IMediaInfoFacade
    {
        MediaInfoResult Read(string path);
    }
}
