using System;
using DR.Marvin.Model;

namespace DR.Marvin.Plugins.Wfs
{
    public class WorkflowConfiguration
    {
        public StateFormat Format { get; set; }
        public AspectRatio AspectRatio { get; set; }
        public Resolution Resolution { get; set; }
        public bool BurnInLogo { get; set; }
        public Guid Workflow { get; set; }
    }
}
