using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Serilog;
using suap.miniagent;

namespace qoldau.suap.miniagent {
    public sealed class WindowsBackgroundService : BackgroundService {

        public WindowsBackgroundService(AgentService agentService, ILogger<WindowsBackgroundService> logger) {
            _agentService = agentService;
            _logger = logger;
        }

        private AgentService _agentService { get; }
        private ILogger<WindowsBackgroundService> _logger { get; }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
            try {
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


                while (!stoppingToken.IsCancellationRequested) {
                    
                    await _agentService.CollectAndSendData(config, logger);
                    
                    logger.Information($"Sleep. Start after -> {config.SleepIntervalInMs} ms");
                    await Task.Delay(config.SleepIntervalInMs, stoppingToken);
                }
                Environment.Exit(0);
            } catch (OperationCanceledException) {
                Environment.Exit(0);
            } catch (Exception ex) {
                _logger.LogError(ex, "{Message}", ex.Message);
                Environment.Exit(1);
            }
        }

        public override Task StopAsync(CancellationToken cancellationToken) {
            return base.StopAsync(cancellationToken);
        }

    }
}
