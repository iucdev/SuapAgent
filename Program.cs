using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using qoldau.suap.miniagent;
using qoldau.suap.miniagent.localDb;
using qoldau.suap.miniagent.Timers;
using Serilog;
using suap.miniagent;

internal class Program {
    private static void Main(string[] args) {
        if(args.Length > 0) {
            //меняем рабочую папку
            Environment.CurrentDirectory = args[0];
        }

        var configJson = File.ReadAllText("appsettings.json");
        var config = JsonConvert.DeserializeObject<Config>(configJson);

        var loggerConfiguration = new LoggerConfiguration()
            .WriteTo.Console()
            .WriteTo.File(config.LogsPath, rollingInterval: RollingInterval.Day);
        if (config.ShowDebugLogs) {
            loggerConfiguration.MinimumLevel.Debug();
        } else {
            loggerConfiguration.MinimumLevel.Information();
        }
        using var logger = loggerConfiguration.CreateLogger();


        var builder = Host.CreateApplicationBuilder(args);
        builder.Logging.AddSerilog(logger);
        builder.Services.AddSingleton<ILogger>(logger);
        
        builder.Services.AddSingleton(new SqlLiteDbManager(config.LocalDbFolder));

        builder.Services.Configure<Config>(x => {
            x.SentToAlcotrackValuesCount = config.SentToAlcotrackValuesCount;
            x.AlcotrackApiUrl = config.AlcotrackApiUrl;
            x.BearerTokenFromAlcotrack = config.BearerTokenFromAlcotrack;
            x.LocalDbFolder = config.LocalDbFolder;
            x.LogsPath = config.LogsPath;
            x.SleepIntervalInMs = config.SleepIntervalInMs;
            x.ShowDebugLogs = config.ShowDebugLogs;
            x.Devices = config.Devices;
        });


        builder.Services.AddWindowsService(options => {
            options.ServiceName = "Qoldau Alcotrack Agent Service";
        });


        #region ukmScannersWithTcpConfig
        
        var ukmScannersWithTcpConfigs = config.Devices
            .Where(x => x.Type == DeviceType.Techvision)
            .Where(x => x.TcpConfig != null)
            .ToArray();

        for (var i = 0; i < ukmScannersWithTcpConfigs.Length; i++) {
            var ukmScannerWithTcpConfig = ukmScannersWithTcpConfigs[i];

            var ip = ukmScannerWithTcpConfig.TcpConfig.Ip;
            var port = ukmScannerWithTcpConfig.TcpConfig.Port;
            var techvisionIndicator = ukmScannerWithTcpConfig.TechvisionIndicators[0];
            var sleepDelay = TimeSpan.FromSeconds(1);
            switch (i) {
                case 0:
                    builder.Services.AddHostedService(x => 
                        new UkmScannerTcpClientTimer0(techvisionIndicator, ip, port, x.GetRequiredService<SqlLiteDbManager>(), sleepDelay,x.GetRequiredService<ILogger>()));
                    break;

                case 1:
                    builder.Services.AddHostedService(x =>
                        new UkmScannerTcpClientTimer1(techvisionIndicator, ip, port, x.GetRequiredService<SqlLiteDbManager>(), sleepDelay, x.GetRequiredService<ILogger>()));
                    break;

                case 2:
                    builder.Services.AddHostedService(x =>
                        new UkmScannerTcpClientTimer2(techvisionIndicator, ip, port, x.GetRequiredService<SqlLiteDbManager>(), sleepDelay, x.GetRequiredService<ILogger>()));
                    break;

                case 3:
                    builder.Services.AddHostedService(x =>
                        new UkmScannerTcpClientTimer3(techvisionIndicator, ip, port, x.GetRequiredService<SqlLiteDbManager>(), sleepDelay, x.GetRequiredService<ILogger>()));
                    break;

                case 4:
                    builder.Services.AddHostedService(x =>
                        new UkmScannerTcpClientTimer4(techvisionIndicator, ip, port, x.GetRequiredService<SqlLiteDbManager>(), sleepDelay, x.GetRequiredService<ILogger>()));
                    break;

                case 5:
                    builder.Services.AddHostedService(x =>
                        new UkmScannerTcpClientTimer5(techvisionIndicator, ip, port, x.GetRequiredService<SqlLiteDbManager>(), sleepDelay, x.GetRequiredService<ILogger>()));
                    break;

                case 6:
                    builder.Services.AddHostedService(x =>
                        new UkmScannerTcpClientTimer6(techvisionIndicator, ip, port, x.GetRequiredService<SqlLiteDbManager>(), sleepDelay, x.GetRequiredService<ILogger>()));
                    break;

                case 7:
                    builder.Services.AddHostedService(x =>
                        new UkmScannerTcpClientTimer7(techvisionIndicator, ip, port, x.GetRequiredService<SqlLiteDbManager>(), sleepDelay, x.GetRequiredService<ILogger>()));
                    break;

                case 8:
                    builder.Services.AddHostedService(x =>
                        new UkmScannerTcpClientTimer8(techvisionIndicator, ip, port, x.GetRequiredService<SqlLiteDbManager>(), sleepDelay, x.GetRequiredService<ILogger>()));
                    break;

                case 9:
                    builder.Services.AddHostedService(x =>
                        new UkmScannerTcpClientTimer9(techvisionIndicator, ip, port, x.GetRequiredService<SqlLiteDbManager>(), sleepDelay, x.GetRequiredService<ILogger>()));
                    break;

                case 10:
                    builder.Services.AddHostedService(x =>
                        new UkmScannerTcpClientTimer10(techvisionIndicator, ip, port, x.GetRequiredService<SqlLiteDbManager>(), sleepDelay, x.GetRequiredService<ILogger>()));
                    break;


                default: throw new NotImplementedException(i.ToString());
            }

        }

        #endregion


        builder.Services.AddHostedService<PlcDataCollectorBackgroundService>();

        var host = builder.Build();
        host.Run();
    }
}