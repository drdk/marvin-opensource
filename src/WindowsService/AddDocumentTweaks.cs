using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http.Description;
using Swashbuckle.Swagger;

namespace DR.Marvin.WindowsService
{
    //Disabled to prevent weird installer bug.
    /*
    class AddDocumentTweaks : IDocumentFilter
    {
        public void Apply(SwaggerDocument swaggerDoc, SchemaRegistry schemaRegistry, IApiExplorer apiExplorer)
        {
            var xmlOperation = swaggerDoc.paths["/api/SystemStatus/GetXml"].get;
            xmlOperation.produces = new List<string> { "text/xml"};
        }
    }
    */
}
