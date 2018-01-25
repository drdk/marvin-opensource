using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Reflection;
using System.Web.Http;
using DR.Common.Monitoring.Contract;
using DR.Common.RESTClient;
using DR.Marvin.Executor;
using DR.Marvin.Logging;
using DR.Marvin.Model;
using DR.Marvin.Planner;
using DR.Marvin.Repositories;
using DR.Marvin.WindowsService.Attributes;
using DR.Marvin.Plugins.Wfs;
using DR.Marvin.Plugins.FFMpeg;
using DR.Marvin.WindowsService.Model;
using DR.WfsService.Contract;
using DR.Marvin.MediaInfoService;
using DR.Marvin.Plugins.FileRenamer;
using JetBrains.Annotations;
using Microsoft.Owin;
using Microsoft.Owin.FileSystems;
using Microsoft.Owin.StaticFiles;
using Newtonsoft.Json;
using Owin;
using StructureMap;
using Swashbuckle.Application;
using WebApiContrib.IoC.StructureMap;

namespace DR.Marvin.WindowsService
{
    /// <summary>
    /// initialzation and configuration class
    /// </summary>
    [UsedImplicitly]
    public class Startup
    {
        private static IContainer SetupStructureMap()
        {
            ObjectFactory.Initialize(x =>
            {
                x.For<IWfsService>().Use<WfsService.WfsService>()
                    .Ctor<string>(/*"uri"*/).Is(Properties.Settings.Default.WfsUri)
                    .Setter(ws=>ws.RetryCount).Is(int.Parse(Properties.Settings.Default.WfsRetryCount))
                    .Setter(ws=>ws.RetrySleepMs).Is(int.Parse(Properties.Settings.Default.WfsRetrySleepMs))
                    ;
                x.For<IPresetProvider>().Use<EmbededPresetProvider>();
                x.For<IAudioPresetProvider>().Use<EmbededAudioPresetProvider>();
                x.For<IFFMpegService>().Use<FFMpegService>()
                    .Ctor<string>(/*"uri"*/).Is(Properties.Settings.Default.FFMpegServiceUri)
                    .Ctor<int>(/* threadsPerMachine*/).Is(Properties.Settings.Default.FFMpegNumberOfThreadsPerNode)
                    ;

                x.For<IJobRepository>().Singleton().Use<SqlJobRepository>();
                x.For<ISemaphoreRepository>().Singleton().Use<SqlSemaphoreRepository>();
                x.For<IHealthCounterRepository>().Singleton().Use<SqlHealthCounterRepository>()
                    .Ctor<TimeSpan>().Is(Properties.Settings.Default.HealthCounterMaxAge);

                x.For<IJsonClient>().Use(new JsonClient(true));
                x.For<ICallbackService>().Singleton().Use<CallbackService>();
                x.For<ILogging>().Singleton().Use<Log4NetLogging>();
                x.For<IPlanner>().Singleton().Use<SimplePlanner>();

                #region Plugins
                for (var i = 1; i <= Properties.Settings.Default.WfsNumberOfNodes ; i++)
                {
                    x.For<IPlugin>().Singleton().Add<Wfs>()
                    .Ctor<string>(/*"urn"*/).Is($"{Wfs.UrnPrefix}{i:D2}");
                }
                for (var i = 1; i <= Properties.Settings.Default.FFMpegNumberOfNodes; i++)
                {
                    x.For<IPlugin>().Singleton().Add<FFMpeg>()
                    .Ctor<string>("urn").Is($"{FFMpeg.UrnPrefix}{i:D2}");
                }
                x.For<IPlugin>().Singleton().Add<FileRenamer>()
                .Ctor<string>(/*"urn"*/).Is($"{FileRenamer.UrnPrefix}01");

                x.For<IEnumerable<IPlugin>>().Singleton().Use(y => y.GetAllInstances<IPlugin>());
                #endregion

                x.For<IExecutor>().Singleton().Use<Executor.Executor>();
                x.For<ITimeProvider>().Singleton().Use<RealTimeProvider>();

                x.For<IMediaInfoFacade>().Use<MediaInfoFacade>();

                if (Properties.Settings.Default.WfsNumberOfNodes > 0)
                    x.For<IHealthCheck>().Add<WfsHealthCheck>();
                if (Properties.Settings.Default.FFMpegNumberOfNodes > 0)
                    x.For<IHealthCheck>().Add<FFMpegHealthCheck>();

                x.For<IHealthCheck>().Add<SqlRepositoryHealthCheck>();
                x.For<ICommandRepository>().Add<SqlCommandRepository>();
                x.For<IHealthCheck>().Add<JobsHealthCheck>()
                .Ctor<int>("minutes").Is(Properties.Settings.Default.JobsHealthCheckMinutes)
                .Ctor<int>("minimumFailCount").Is(Properties.Settings.Default.JobsHealthCheckMinimumFailCount)
                .Ctor<double>("failureRatio").Is(Properties.Settings.Default.JobsHealthCheckFailureRatio)
                ;
                x.For<IHealthCheck>().Add<ExecutorHealthCheck>();
                x.For<IHealthCheck>().Add<PulseHealthCheck>();
                x.For<IHealthCheck>().Add<SemaphoreHealthCheck>();
                x.For<IEnumerable<IHealthCheck>>().Use(y => y.GetAllInstances<IHealthCheck>());
                x.For<ISystemStatus>().Singleton().Add<Common.Monitoring.SystemStatus>();
            });
            return ObjectFactory.Container;
        }
        
        /// <summary>
        /// This code configures Web API. The Startup class is specified as a type
        /// parameter in the WebApp.Start method.
        /// </summary>
        [UsedImplicitly]
        public void Configuration(IAppBuilder appBuilder)
        {
            Utilities.Port = Properties.Settings.Default.Port;
            // static content file server 
            appBuilder.UseStaticFiles(new StaticFileOptions
            {
                FileSystem = new PhysicalFileSystem(
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Content")),
                RequestPath = new PathString("/content")
            });

            // Configure Web API for self-host. 
            var config = new HttpConfiguration
            {
                DependencyResolver = new StructureMapResolver(SetupStructureMap())
            };

            // only allow json...
            config.Formatters.Clear(); 

            config.Formatters.Add(new JsonMediaTypeFormatter
            {
                Indent = true,SerializerSettings =  new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore,
                    Converters = new List<JsonConverter> { new Newtonsoft.Json.Converters.StringEnumConverter()}
                }
            });

            // Configure mapping from thrown exceptions to http errors...
            config.Filters.Add(new UnhandledExceptionFilterAttribute(ObjectFactory.GetInstance<ILogging>())
                .Register<ArgumentException>((ex, req) =>
                    req.CreateErrorResponse(HttpStatusCode.BadRequest, "Argument error : " + ex.Message, ex))
                .Register<ArgumentNullException>((ex, req) =>
                    req.CreateErrorResponse(HttpStatusCode.BadRequest, "Argument null error : " + ex.Message, ex))
                .Register<KeyNotFoundException>((ex, req) =>
                    req.CreateErrorResponse(HttpStatusCode.NotFound, "Key not found : " + ex.Message, ex))
                .Register<OrderException>((ex, req) =>
                    req.CreateErrorResponse(HttpStatusCode.BadRequest, ex.Message, ex))
                );

            // Setup api route
            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{action}/{id}",
                defaults: new { id = RouteParameter.Optional }
                );

            config.Routes.MapHttpRoute(
                name: "versionInfo",
                routeTemplate: "__version.js",
                defaults: new {controller = "SystemStatus", action = "GetVersionInfo"}
                );
            
            #region Swagger configuration
            // Get documentation
            var xmlDocFiles = 
                Directory.GetFiles($"{AppDomain.CurrentDomain.BaseDirectory}\\App_Data\\")
                .Where(filename => filename.EndsWith(".XML", StringComparison.InvariantCultureIgnoreCase));
            
            config
                .EnableSwagger(c =>
                {
                    c.DescribeAllEnumsAsStrings();
                    c.SingleApiVersion("v1", "DR Marvin").Description("Central transkoder *platform*." +
                        $@"<br/> 
* [Source]({VersionHelper.GitRepoUri}) 
  * Build time : {VersionHelper.BuildTime.ToLocalTime()}"+
(VersionHelper.BuildNumber.HasValue?$@"
  * Build Number : {VersionHelper.BuildNumber}
  * Branch : [{VersionHelper.Branch}]({VersionHelper.GitBranchUri}) 
  * Commit : [{VersionHelper.ShortCommitHash}]({VersionHelper.GitCommitUri})" 
:@"
  * Local build")+@"
* [Gource](http://www.youtube.com/watch?v=wg47AqKFEag)
* [Monitoring XML](/api/SystemStatus/GetXml)
");
                    foreach (var xmlDocFile in xmlDocFiles) c.IncludeXmlComments(xmlDocFile);
                    //c.PrettyPrint();
                    c.SchemaFilter<AddSchemaExamples>();
                    //c.DocumentFilter<AddDocumentTweaks>();
                })
                .EnableSwaggerUi(c =>
                {
                    var thisAssembly = Assembly.GetExecutingAssembly();
                    c.InjectStylesheet(thisAssembly, "DR.Marvin.WindowsService.Content.Marvin.css");
                    c.DisableValidator();
                    c.DocExpansion(DocExpansion.List);
                });

            config.Routes.MapHttpRoute(
                name:"custom_swagger_ui_shortcut",
                routeTemplate:"",
                defaults:null,
                constraints:null,
                handler:new RedirectHandler(SwaggerDocsConfig.DefaultRootUrlResolver, "/swagger"));
            #endregion
            
            // assign the appBuilder the new configuration
            appBuilder.UseWebApi(config);

            //Init automapper
            AutoMapperHelper.EnsureInitialization();
            var logging = ObjectFactory.GetInstance<ILogging>();
            logging.LogDebug($"Persistance store: { ObjectFactory.GetInstance<IJobRepository>().GetEnvironment()}\n");
            logging.LogDebug($"Build info:\nBuild time: {VersionHelper.BuildTime.ToLocalTime()} {(VersionHelper.BuildNumber.HasValue ? $"\nBuild Number : {VersionHelper.BuildNumber}\nBranch : {VersionHelper.Branch}\nCommit : {VersionHelper.ShortCommitHash}" : "")} ");
            logging.LogDebug("Configuration done.");
        }
    }
}
