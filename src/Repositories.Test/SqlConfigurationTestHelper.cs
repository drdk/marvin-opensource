#define USELOCALDB // :arrow_left: comment this line out to use od01udv instead
using System.Configuration;
using System.Reflection;

namespace DR.Marvin.Repositories.Test
{
    internal static class SqlConfigurationTestHelper
    {
        /// <summary>
        /// Ugly hacky work around to fix team city inability to read test project app.configs. :frog:
        /// </summary>
        static SqlConfigurationTestHelper()
        {
            //provider hacking (missing app.config work around)
            var instance = System.Data.Entity.SqlServer.SqlProviderServices.Instance;

            // :skull: :warning:  Hacking around System.Configuration security because why not?
            typeof(ConfigurationElementCollection)
                .GetField("bReadOnly", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(ConfigurationManager.ConnectionStrings, false);

#if USELOCALDB
            var host = "localhost";
#else
            var host = "od01udv";
#endif

            if (ConfigurationManager.ConnectionStrings["MarvinEntities"] == null)
                ConfigurationManager.ConnectionStrings.Add(
                    new ConnectionStringSettings(
                        "MarvinEntities",
                        $"metadata=res://*/MarvinEntities.csdl|res://*/MarvinEntities.ssdl|res://*/MarvinEntities.msl;provider=System.Data.SqlClient;provider connection string='data source={host};initial catalog=MarvinLocal;persist security info=True;MultipleActiveResultSets=True;integrated security=false;user id=nunit;password=test;App=EntityFramework'",
                        "System.Data.EntityClient"));
        }

        public static void Configure() { }
    }
}
