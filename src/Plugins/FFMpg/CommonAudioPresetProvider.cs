using System;
using System.Collections.Generic;
using System.Linq;
using DR.Marvin.Model;

namespace DR.Marvin.Plugins.FFMpeg
{
    public abstract class CommonAudioPresetProvider: IAudioPresetProvider
    {
        public abstract IList<FFMpegConfiguration> AsList();

        public IDictionary<StateFormat,IList<FFMpegClient.AudioDestinationFormat>> AsDictionary()
        {
            var list = AsList();
            if (list.Count == 0)
                throw new AudioPresetProviderException("AudioDestinationConfiguration empty.");
            if (list.Any(cfg => cfg.Format == StateFormat.unknown))
                throw new AudioPresetProviderException("Invalid enum found in configuration.");
            if (list.Any(cfg => !Enum.IsDefined(typeof(StateFormat), cfg.Format)))
                throw new AudioPresetProviderException("Invalid format found in configuration.");
            if (list.Any(t => list.Count(cfg => cfg.Format == t.Format) != 1))
            {
                throw new AudioPresetProviderException("Duplicated format found.");
            }
            return list.ToDictionary(
                entry => entry.Format,
                entry => entry.AudioDestinationsList
                );
        }
    }


    [Serializable]
    public class AudioPresetProviderException : Exception
    {
        /// <inheritdoc />
        public AudioPresetProviderException(string message) : base(message) { }
    }
}
