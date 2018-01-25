using System;
using System.Collections.Generic;
using DR.Marvin.Model;

namespace DR.Marvin.Plugins.Wfs
{
    public interface IPresetProvider
    {
        IList<WorkflowConfiguration> AsList();
        IDictionary<Tuple<StateFormat, AspectRatio, Resolution, bool>, Guid> AsDictionary();
        string MachineGroup { get; }
    }
}
