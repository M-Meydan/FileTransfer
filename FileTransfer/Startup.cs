using FileTransfer.Commands;
using FileTransfer.Helpers;
using FileTransfer.Models;
using FileTransfer.Services;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MMQ;
using System.Collections.Concurrent;
using System.Reflection;
using System.Text;

namespace FileTransfer
{
    
    public class Startup { 

        IHost _host;
        ILogger<Startup> _logger;
        IAppConfig _appConfig;
        ICommandFactory _commandFactory;
        IMediator? _mediatr;
        CancellationTokenSource _cts;

        public Startup()
        {
            AppDomain.CurrentDomain.UnhandledException += UnhandledException;

            _host = BuildHost();
            _logger = _host.Services.GetService<ILogger<Startup>>();
            _appConfig = _host.Services.GetService<IAppConfig>();
            _commandFactory = _host.Services.GetService<ICommandFactory>();
            _mediatr = _host.Services.GetService<IMediator>();
            _cts = new CancellationTokenSource();
        }

        public async Task Init()
        {
            _logger.LogInformation("Enter source and destination folders for transferring files.");
            _logger.LogInformation("e.g. transfer ./SrcFolder ./DestFolder");

            string arguments;
            ICommand command;
            try
            {
                do
                {
                    arguments = Console.ReadLine();
                    command = _commandFactory.GetCommand(arguments);

                    if (command is ExitCommand)
                        _cts.Cancel();
                    else if (command is TransferCommand)
                        await _mediatr.Send(command, _cts.Token);

                } while (!_cts.IsCancellationRequested);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString()); 
            }

            ExitApp();

            //if (task.Status != TaskStatus.Canceled) fileDownloader.CancelDownloads();

            _logger.LogInformation($"Host shutting down!");
            await Task.Delay(2000); // give time to release resources
            Environment.Exit(1);
        }

        IHost BuildHost()
        {
            return new HostBuilder()
                    .ConfigureAppConfiguration(app => { app.AddJsonFile("appsettings.json"); })
                    .ConfigureServices((hostContext, services) => BuildServiceCollection(hostContext, services))
                    .UseConsoleLifetime()
                    .Build();
        }

        void BuildServiceCollection(HostBuilderContext hostContext, IServiceCollection services)
        {
            services.AddMediatR(Assembly.GetExecutingAssembly())
                    .AddLogging(configure =>
                    {
                        configure.AddFilter("Microsoft", LogLevel.Warning)
                                 .AddFilter("System", LogLevel.Error)
                                 .AddConsole(config => { config.TimestampFormat = "hhh\\:mm\\:ss\\.ff| "; });
                        //.AddDebug();
                    });

            services.AddSingleton<IAppConfig, AppConfig>();
            services.AddSingleton<ICommandFactory, CommandFactory>();
            services.AddSingleton<IFileTransferService, FileTransferService>();
           

            var service = services.BuildServiceProvider();
        }

        void UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            _logger.LogInformation(e.ExceptionObject.ToString());
            ExitApp();
        }

        void ExitApp()
        {
            _logger.LogInformation("Application stopping.");
        }
    }
}
