using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;

namespace DR.Marvin.Plugins.FFMpeg
{
    public class EmbededAudioPresetProvider: CommonAudioPresetProvider
    {
        private const string ResourceName = "DR.Marvin.Plugins.FFMpeg.AudioDestinationConfiguration.json";
        private readonly IList<FFMpegConfiguration> _list;

        public EmbededAudioPresetProvider()
        {
            var thisAssembly = Assembly.GetExecutingAssembly();
            var stream = thisAssembly.GetManifestResourceStream(ResourceName);
            if (stream == null)
                throw new Exception($"Unable to read resource {ResourceName}.");

            using (var jsonTextReader = new JsonTextReader(new StreamReader(stream)))
            {
                var serializer = new JsonSerializer();
                var res = serializer.Deserialize<AudioDestinationConfiguration>(jsonTextReader);
                _list = res.Presets;
            }
        }

        public override IList<FFMpegConfiguration> AsList()
        {
            return _list;
        }
    }
}
