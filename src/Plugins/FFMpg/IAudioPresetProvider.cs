using System.Collections.Generic;
using DR.Marvin.Model;

namespace DR.Marvin.Plugins.FFMpeg
{
    public interface IAudioPresetProvider
    {
        IDictionary<StateFormat,IList<FFMpegClient.AudioDestinationFormat>> AsDictionary();
    }
}
