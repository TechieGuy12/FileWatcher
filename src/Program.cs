using System.CommandLine;
using System.CommandLine.Invocation;
using System.Diagnostics.CodeAnalysis;
using TE.FileWatcher.Configuration;
using TE.FileWatcher.Logging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.WindowsServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.CommandLine.IO;
using System.Text;
using System;

namespace TE.FileWatcher
{
    /// <summary>
    /// The main program class.
    /// </summary>
    internal class Program
    {
        // Success return code
        private const int SUCCESS = 0;

        // Error return code
        private const int ERROR = -1;

        /// <summary>
        /// The main function.
        /// </summary>
        /// <param name="args">
        /// Arguments passed into the application.
        /// </param>
        /// <returns>
        /// Returns 0 on success, otherwise non-zero.
        /// </returns>
        [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
        static int Main(string[] args)
        {
            if (WindowsServiceHelpers.IsWindowsService())
            {
                return ServiceMain(args);
            }
            else
            {
                return InitWatcher(args);
            }
        }

        /// <summary>
        /// Sets up the RootCommand, which parses the command line and runs the filewatcher.
        /// This is both used directly from Main, but also indirectly from the service, if running as a Windows Service.
        /// </summary>
        /// <param name="args"></param>
        /// <param name="stoppingToken"></param>
        /// <param name="console"></param>
        /// <returns></returns>
        [RequiresUnreferencedCode("Calls TE.FileWatcher.Configuration.IConfigurationFile.Read()")]
        public static int InitWatcher(string[] args, CancellationToken? stoppingToken = null, WindowsBackgroundService.LoggingConsole? console = null)
        {
            RootCommand rootCommand = new()
            {
                new Option<string>(
                    aliases: new string[] { "--folder", "-f" },
                    description: "The folder containing the configuration XML file."),

                new Option<string>(
                    aliases: new string[] { "--configFile", "-cf" },
                    description: "The name of the configuration XML file."),
            };
            if (stoppingToken != null)
            {
                rootCommand.AddOption(new Option<CancellationToken?>(
                    alias: "--stoppingToken",
                    getDefaultValue: () => stoppingToken,
                    description: "CancellationToken")
                    { IsHidden = true }
                );
            }
            rootCommand.Description = "Monitors files and folders for changes.";
            rootCommand.Handler = CommandHandler.Create<string, string, CancellationToken?>(Run);

            rootCommand.TreatUnmatchedTokensAsErrors = true;
            return rootCommand.Invoke(args, console);
        }

        /// <summary>
        /// Runs the file/folder watcher.
        /// </summary>
        /// <param name="folder">
        /// The folder where the config and notifications files are stored.
        /// </param>
        /// <param name="configFileName">
        /// The name of the configuration file.
        /// </param>
        /// <returns>
        /// Returns 0 if no error occurred, otherwise non-zero.
        /// </returns>
        [RequiresUnreferencedCode("Calls TE.FileWatcher.Configuration.IConfigurationFile.Read()")]
        internal static int Run(string? folder, string? configFile, CancellationToken? stoppingToken = null)
        {            
            IConfigurationFile config = new XmlFile(folder, configFile);

            // Load the watches information from the config XML file
            Watches? watches = config.Read();
            if (watches == null)
            {
                return ERROR;
            }

            // Set the logger
            if (!SetLogger(watches))
            {
                return ERROR;
            }

            // Run the watcher tasks
            if (StartWatchers(watches, stoppingToken))
            {
                return SUCCESS;
            }
            else
            {
                return ERROR;
            }
        }

        /// <summary>
        /// Runs the file/folder watcher as a Windows Service.
        /// </summary>
        /// <param name="folder">
        /// The folder where the config and notifications files are stored.
        /// </param>
        /// <param name="configFileName">
        /// The name of the configuration file.
        /// </param>
        /// <returns>
        /// Returns 0 if no error occurred, otherwise non-zero.
        /// </returns>
        [RequiresUnreferencedCode("Calls TE.FileWatcher.Configuration.IConfigurationFile.Read()")]
        internal static int ServiceMain(string[] args)
        {
            HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
            builder.Services.AddWindowsService(options =>
            {
                options.ServiceName = "FileWatcher Service";
            });

            builder.Services.AddHostedService<WindowsBackgroundService>();

            // See: https://github.com/dotnet/runtime/issues/47303
            builder.Logging.AddConfiguration(builder.Configuration.GetSection("Logging"));

            IHost host = builder.Build();
            host.Run();
            return SUCCESS;
        }

        /// <summary>
        /// Sets the logger.
        /// </summary>
        /// <param name="watches">
        /// The watches object that contains the log path.
        /// </param>
        /// <returns>
        /// True if the Logger was set, otherwise false.
        /// </returns>
        private static bool SetLogger(Watches watches)
        {
            if (watches == null)
            {
                Console.WriteLine("The watches object was not initialized.");
                return false;
            }

            try
            {
                Logger.SetLogger(watches.Logging);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"The log file could not be set. Reason: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Runs the watcher tasks as defined in the configuration XML file.
        /// </summary>
        /// <param name="watches">
        /// The watches.
        /// </param>
        /// <returns>
        /// True if the tasks were started and run successfully, otherwise false.
        /// </returns>
        private static bool StartWatchers(Watches watches, CancellationToken? stoppingToken = null)
        {
            if (watches == null)
            {
                Console.WriteLine("The watches object was not initialized.");
                return false;
            }

            watches.Start();
            if (stoppingToken != null)
            {
                stoppingToken.Value.WaitHandle.WaitOne();
            }
            else
            {
                new AutoResetEvent(false).WaitOne(); // Will never return
            }

            Logger.WriteLine("All watchers have closed.");
            return true;
        }

    }

    public sealed class WindowsBackgroundService : BackgroundService
    {
        private readonly ILogger<WindowsBackgroundService> _eventLogger;

        public WindowsBackgroundService(
            ILogger<WindowsBackgroundService> logger) =>
                (_eventLogger) = (logger);

        [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                _eventLogger.LogInformation($"CommandLineArgs: {Environment.CommandLine}");

                LoggingConsole loggingConsole = new LoggingConsole(_eventLogger);
                var errorLogWriter = new ErrorLogWriter(_eventLogger);
                Console.SetError(errorLogWriter);
                var result = await Task.Run( () => Program.InitWatcher(Environment.GetCommandLineArgs(), stoppingToken, loggingConsole));
                if (result != 0)
                {
                    errorLogWriter.WriteLine();
                    _eventLogger.LogError("Process: {ProcessPath}\r\n{Message}", Environment.ProcessPath, $"Service stopped with error: {result}");
                    Environment.Exit(result);
                }
            }
            catch (TaskCanceledException)
            {
                // When the stopping token is canceled, for example, a call made from services.msc,
                // we shouldn't exit with a non-zero exit code. In other words, this is expected...
            }
            catch (Exception ex)
            {
                _eventLogger.LogError(ex, "Process: {ProcessPath}\r\n{Message}", Environment.ProcessPath, ex.Message);

                // Terminates this process and returns an exit code to the operating system.
                // This is required to avoid the 'BackgroundServiceExceptionBehavior', which
                // performs one of two scenarios:
                // 1. When set to "Ignore": will do nothing at all, errors cause zombie services.
                // 2. When set to "StopHost": will cleanly stop the host, and log errors.
                //
                // In order for the Windows Service Management system to leverage configured
                // recovery options, we need to terminate the process with a non-zero exit code.
                Environment.Exit(1);
            }
        }
        public class LoggingConsole : IConsole
        {
            public readonly ILogger<WindowsBackgroundService> _eventLogger;

            public LoggingConsole(ILogger<WindowsBackgroundService> eventLogger) 
            {
                this._eventLogger = eventLogger;
            }

            public IStandardStreamWriter Out => new LoggingStandardWriter(this);

            public bool IsOutputRedirected => true;

            public IStandardStreamWriter Error => new LoggingErrorWriter(this);

            public bool IsErrorRedirected => true;

            public bool IsInputRedirected => false;

            private class LoggingStandardWriter : IStandardStreamWriter
            {
                private readonly LoggingConsole _console;

                public LoggingStandardWriter(LoggingConsole console) 
                {
                    _console = console;
                }

                public void Write(string value)
                {
                    if (!string.IsNullOrEmpty(value?.Trim()))
                        _console._eventLogger.LogDebug("Process: {ProcessPath}\r\n{value}", Environment.ProcessPath, value);
                }
            }
            private class LoggingErrorWriter : IStandardStreamWriter
            {
                private readonly LoggingConsole _console;

                public LoggingErrorWriter(LoggingConsole console)
                {
                    _console = console;
                }
                public void Write(string value)
                {
                    if (!string.IsNullOrEmpty(value?.Trim()))
                        _console._eventLogger.LogError("Process: {ProcessPath}\r\n{value}", Environment.ProcessPath, value);
                }
            }
        }
        public class ErrorLogWriter : TextWriter
        {
            public override Encoding Encoding => Encoding.UTF8;

            public readonly ILogger<WindowsBackgroundService> _eventLogger;
            StringBuilder _message = new StringBuilder();

            public ErrorLogWriter(ILogger<WindowsBackgroundService> eventLogger)
            {
                this._eventLogger = eventLogger;
            }

            public override void Write(string? value)
            {
                if (!string.IsNullOrEmpty(value))
                    _message.Append(value);
            }

            public override void Write(char ch)
            {
                _message.Append(ch);
            }

            public override void WriteLine(string? value)
            {
                if (!string.IsNullOrEmpty(value?.Trim()))
                {
                    _message.Append(value);
                }
                WriteLine();
            }
            public override void WriteLine()
            {
                if (_message.Length > 0)
                {
                    _eventLogger.LogError("Process: {ProcessPath}\r\n{value}", Environment.ProcessPath, _message);
                    _message.Clear();
                }
            }

        }
    }
}
