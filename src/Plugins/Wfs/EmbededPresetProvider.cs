using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;

namespace DR.Marvin.Plugins.Wfs
{
    public class EmbededPresetProvider: CommonPresetProvider
    {
        private const string ResourceName = "DR.Marvin.Plugins.Wfs.WorkflowsConfiguration.json";

        public EmbededPresetProvider()
        {
            var thisAssembly = Assembly.GetExecutingAssembly();
            var stream = thisAssembly.GetManifestResourceStream(ResourceName);
            if (stream == null)
                throw new Exception($"Unable to read resource {ResourceName}.");

            using (var jsonTextReader = new JsonTextReader(new StreamReader(stream)))
            {
                var serializer = new JsonSerializer();
                var res = serializer.Deserialize<WfsConfiguration>(jsonTextReader);
                _list = res.Presets;
                MachineGroup = res.MachineGroup;
            }
        }

        private readonly IList<WorkflowConfiguration> _list;
        public override IList<WorkflowConfiguration> AsList()
        {
            return _list;
        }

        public override string MachineGroup { get; }
    }
}
