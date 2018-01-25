using System;
using System.Collections.Generic;
using System.Linq;
using DR.Marvin.Model;

namespace DR.Marvin.Plugins.Wfs
{
    public abstract class CommonPresetProvider : IPresetProvider
    {
        public abstract IList<WorkflowConfiguration> AsList();

        public IDictionary<Tuple<StateFormat, AspectRatio, Resolution, bool>, Guid> AsDictionary()
        {
            var list = AsList();
            if (list.Count == 0)
                throw new PresetProviderException("WorkflowConfiguration empty.");
            if (list.Any(cfg => cfg.AspectRatio == AspectRatio.unknown || cfg.Format == StateFormat.unknown))
                throw new PresetProviderException("Invalid enum found in configuration.");
            if (list.Any(cfg => cfg.Workflow == Guid.Empty))
                throw new PresetProviderException("Invalid workflow guid found in configuration.");
            if (list.Any(t => list.Count(cfg =>
                cfg.BurnInLogo == t.BurnInLogo &&
                cfg.AspectRatio == t.AspectRatio &&
                cfg.Resolution == t.Resolution &&
                cfg.Format == t.Format) != 1))
            {
                throw new PresetProviderException("Duplicated configuration found.");
            }
            #if DEBUG
            var dubs = list.Where(t => list.Count(cfg => cfg.Workflow == t.Workflow) != 1);
            foreach (var workflowConfiguration in dubs)
            {
                Console.WriteLine(workflowConfiguration.Workflow);
            }
            #endif
            if (list.Any(t => list.Count(cfg => cfg.Workflow == t.Workflow) != 1))
            {
                throw new PresetProviderException("Duplicated workflow found.");
            }
            return list.ToDictionary(
                entry => new Tuple<StateFormat, AspectRatio, Resolution, bool>(entry.Format, entry.AspectRatio, entry.Resolution, entry.BurnInLogo),
                entry => entry.Workflow
                );
        }

        public abstract string MachineGroup { get; }
    }

    /// <inheritdoc />
    [Serializable]
    public class PresetProviderException : Exception
    {
        /// <inheritdoc />
        public PresetProviderException(string message) : base (message) { }
    }
}