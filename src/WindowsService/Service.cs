using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Timers;
using DR.Marvin.Model;
using DR.Marvin.WindowsService.Properties;
using Microsoft.Owin.Hosting;
using StructureMap;

namespace DR.Marvin.WindowsService
{
    /// <summary>
    /// Marvin windows service class. Starts the web-api and hosts the executor. 
    /// </summary>
    public partial class Service : ServiceBase
    {

        private IDisposable _api;
        private ITimeProvider _timeProvider;
        private IExecutor _executor;
        private Timer _timer;
        private ILogging _logging;
        private IHealthCounterRepository _healthCounterRepository;
        
        /// <inheritdoc />
        public Service()
        {
            InitializeComponent();
        }

        /// <inheritdoc />
        protected override void OnStart(string[] args)
        {
            var port = Settings.Default.Port;
            var url = Environment.UserInteractive ? $"http://localhost:{port}/" : $"http://+:{port}/";
            var readableUrl = url.Replace("+", Environment.MachineName);
            Console.Write("Starting API at ");
            Console.BackgroundColor = ConsoleColor.DarkBlue;
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(readableUrl);
            Console.BackgroundColor = ConsoleColor.Black;
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine();
            Console.WriteLine();
            _api = WebApp.Start<Startup>(url);
            _logging = ObjectFactory.GetInstance<ILogging>();
            _executor = ObjectFactory.GetInstance<IExecutor>();
            _timeProvider = ObjectFactory.GetInstance<ITimeProvider>();
            _healthCounterRepository = ObjectFactory.GetInstance<IHealthCounterRepository>();
            _timer = new System.Timers.Timer()
            {
                Interval = TimeSpan.FromSeconds(10).TotalMilliseconds,
                Enabled = true,
                AutoReset = false,

            };
            _timer.Elapsed += this.Pulse;
            
            _logging.LogDebug($"Service OnStart done. API started at {readableUrl}");
        }

        private int _pulseErrorCount;// = 0;
        private DateTime _pluseLastError = DateTime.MinValue;
        private static readonly IList<KeyValuePair<int, TimeSpan>> ErrorSleepConfiguration = 
            new List<KeyValuePair<int, TimeSpan>>
            {
                // Must be defined in decending order. 
                new KeyValuePair<int, TimeSpan>(10, TimeSpan.FromMinutes(10)),
                new KeyValuePair<int, TimeSpan>(5, TimeSpan.FromMinutes(2)),
                new KeyValuePair<int, TimeSpan>(1, TimeSpan.FromSeconds(30)),
                new KeyValuePair<int, TimeSpan>(0, TimeSpan.Zero),
            };
        private void Pulse(object source, ElapsedEventArgs eea)
        {
            try
            {
                var potentialErrorSleep =
                    ErrorSleepConfiguration
                        .First(cfg => cfg.Key <= _pulseErrorCount).Value;
                if (_pluseLastError + potentialErrorSleep > _timeProvider.GetUtcNow())
                    return; // noop because of error, longer and longer sleep.
                try
                {
                    _executor.Pulse();
                    _pulseErrorCount = 0;
                }
                catch (Exception e)
                {
                    _pluseLastError = _timeProvider.GetUtcNow();
                    _logging.LogException(e, $"Executor failed to pulse. Successive error count : {++_pulseErrorCount}");
                    _healthCounterRepository.Increment(nameof(Executor.Executor), e.Message);
                }
#if DEBUG
                var status = _executor.GetStatus();
                foreach (var pluginStatus in status.PluginStatuses)
                {
                    Console.Write($"{pluginStatus.SourceUrn} : ");
                    if (pluginStatus.Busy && pluginStatus.CurrentTask == null)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("MISSING NODE");
                    }
                    else if (pluginStatus.Busy)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("BUSY");
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("FREE");
                    }
                    Console.ForegroundColor = ConsoleColor.Gray;
                }
                Console.WriteLine($"Last Executor pulse @ {_timeProvider.GetUtcNow().ToLocalTime()}");
#endif

            }
            catch
            {
                //ignore
            }
            finally
            {
                try
                {
                    _timer.Start();
                }
                catch (ObjectDisposedException)
                {
                    // ignore
                }
            }
        }

        /// <inheritdoc />
        protected override void OnStop()
        {
            _logging.LogDebug("Service stopped.");
            _timer?.Dispose();
            _executor?.Dispose();
            try
            {
                if (_api != null)
                {
                    //TODO: find out how to dispose webapp
                    // Structure map ioc is buggy and will throw an exception
                    // see: https://github.com/WebApiContrib/WebApiContrib.IoC.StructureMap/commit/d0306db2169303f3fdb56a7c7ac940207b5cf0c2
                    // Nuget has not been updated since 2012. switch to submodule, or ignore.
                    //_api.Dispose();
                }
            }
            catch (Exception e)
            {
                _logging.LogException(e,"Failed to dispose webapp");
            }
        }

        /// <summary>
        /// Used when debugging.
        /// </summary>
        public void ManualStart()
        {
            OnStart(new string[0]);
        }
        /// <summary>
        /// Used when debugging.
        /// </summary>
        public void ManualStop()
        {
            OnStop();
        }
    }
}
