using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace DR.Marvin.Logging
{
    internal static class JsonHelper
    {
        public static void SerializeJsonData<T>(Action<string> logMethod, string message, T data)
        {
            using (var sw = new StringWriter())
            {
                var property = data.GetType().GetProperty("Urn");
                if (property != null)
                {
                    var urn = (string)property.GetValue(data);
                    message += " " + urn;
                }
                sw.WriteLine(message);
                sw.WriteLine("data:");
                var js = new JsonSerializer
                {
                    Formatting = Formatting.Indented,
                    NullValueHandling = NullValueHandling.Ignore,
                    Converters = { new StringEnumConverter() }
                };
                js.Serialize(sw, data);
                logMethod(sw.ToString());
            }
        }
    }
}
