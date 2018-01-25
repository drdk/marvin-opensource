using System;
using DR.Marvin.Model;
using DR.Marvin.WindowsService.Model;
using Swashbuckle.Swagger;
#pragma warning disable 1591

namespace DR.Marvin.WindowsService
{
    /// <summary>
    /// Adds examples for swagger-ui
    /// </summary>
    public class AddSchemaExamples : ISchemaFilter
    {
        public void Apply(Schema schema, SchemaRegistry schemaRegistry, Type type)
        {
            
            if (type == typeof(Order))
            {
                schema.example = new Order
                {
                    FilePath = "\\\\ondnas01\\MediaCache\\Test\\cliptest1.mov",
                    BurnInLogo = false,
                    LogoPath = null,
                    BurnInSubtitles = false,
                    SubtitlesPath = null,
                    DestinationFormat = StateFormat.h264_od_standard,
                    ForceAspectRatio = AspectRatio.ratio_16x9,
                    DestinationPath = "\\\\ondnas01\\MediaCache\\Test\\marvin"
                };
            }
            else if (type == typeof(Command))
            {
                schema.example = new Command
                {
                    Type = CommandType.Cancel,
                    Username = "NET\\<login>",
                    Urn = "<job-urn>"
                };
            }
           
        }
    }
}
