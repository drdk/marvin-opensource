using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DR.Marvin.Plugins.Wfs
{
    public class WfsConfiguration
    {
        public string MachineGroup { get; set; }
        public IList<WorkflowConfiguration> Presets { get; set; }
    }
}
