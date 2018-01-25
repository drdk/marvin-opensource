using System.Collections.Generic;
using DR.FFMpegClient;
using DR.Marvin.Model;

namespace DR.Marvin.Plugins.FFMpeg
{
    public class FFMpegConfiguration
    {
        public StateFormat Format { get; set; }
        public IList<AudioDestinationFormat> AudioDestinationsList { get; set; }
    }
}
