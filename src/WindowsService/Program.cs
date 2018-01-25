using System;
using System.Reflection;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;

namespace DR.Marvin.WindowsService
{
    static class Program
    {
        private const string MarvinLogo = @"
    _/      _/                                  _/           
   _/_/  _/_/    _/_/_/  _/  _/_/  _/      _/      _/_/_/    
  _/  _/  _/  _/    _/  _/_/      _/      _/  _/  _/    _/   
 _/      _/  _/    _/  _/          _/  _/    _/  _/    _/    
_/      _/    _/_/_/  _/            _/      _/  _/    _/     

[-I-n-i-t-i-a-l-i-z-a-t-i-o-n-------------------------------]

";

        private static void PrintLogo()
        {
            foreach (var character in MarvinLogo.ToCharArray())
            {
                switch (character)
                {
                    case '_':
                    case '/':
                        break;
                    case '\n':
                        Console.Beep(400, 160);
                        break;
                    case '-':
                        Console.ForegroundColor = ConsoleColor.DarkYellow;
                        Thread.Sleep(10);
                        break;
                    case '[':
                    case ']':
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.Beep(600, 200);
                        break;
                    case ' ':
                    default:
                        break;
                }
                Console.Write(character);
            }
        }

        private static bool stopping = false;
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
       {
            serviceToRun = new Service();
            if (Environment.UserInteractive)  // Console mode
            {
                waitHandle = new EventWaitHandle(false, EventResetMode.ManualReset);
                Console.CursorVisible = false;
                Console.WindowWidth = 100;
                Console.WindowHeight = 42;
                Console.ForegroundColor = ConsoleColor.Cyan;
                PrintLogo();
               
                var assembly = Assembly.GetExecutingAssembly();
                var soundStream = assembly.GetManifestResourceStream("DR.Marvin.WindowsService.Assets.Audio.Original.Marvin_ONLINE.wav");
                var marvinPlayer = new System.Media.SoundPlayer {Stream = soundStream};
                marvinPlayer.Play();
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine($"{VersionHelper.GitRepo}\nBuild time: {VersionHelper.BuildTime}\n");
                Console.WriteLine("Starting Marvin. Service in Console Mode.");
                Console.WriteLine("Press Ctrl+B to launch swagger ui in browser.");
                Console.WriteLine("Press Ctrl+X to exit");
                //Console.CancelKeyPress += OnCancelKeyPress;
                serviceToRun.ManualStart();
                var tokenSource = new CancellationTokenSource();
                Task.Factory.StartNew(result =>
                {
                    while (!stopping)
                    {
                        while (!Console.KeyAvailable)
                        {
                            Thread.Sleep(100);
                        }
                        var input = Console.ReadKey(true);
                        if (input.Key == ConsoleKey.B && input.Modifiers.HasFlag(ConsoleModifiers.Control))
                        {
                            System.Diagnostics.Process.Start($"http://localhost:{Properties.Settings.Default.Port}");
                        }
                        if (input.Key == ConsoleKey.X && input.Modifiers.HasFlag(ConsoleModifiers.Control))
                            waitHandle.Set();

                    }
                },tokenSource, tokenSource.Token);
                WaitHandle.WaitAll(new WaitHandle[] {waitHandle});
                stopping = true;
                
                soundStream = assembly.GetManifestResourceStream("DR.Marvin.WindowsService.Assets.Audio.Original.marvin_OFFLINE2.wav");
                marvinPlayer = new System.Media.SoundPlayer { Stream = soundStream };
                marvinPlayer.PlaySync();
                serviceToRun.ManualStop();
            }
            else // Windows service mode
            {
                var servicesToRun = new ServiceBase[]
                {
                    serviceToRun
                };
                ServiceBase.Run(servicesToRun);
            }
        }

        private static EventWaitHandle waitHandle;
        private static Service serviceToRun;

        private static void OnCancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            waitHandle.Set();
        }
    }
}
