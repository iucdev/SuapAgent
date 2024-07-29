using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using qoldau.suap.miniagent.localDb;
using Serilog;
using suap.miniagent;

namespace qoldau.suap.miniagent {
    public sealed class PlcDataCollectorBackgroundService : BackgroundService {

        public PlcDataCollectorBackgroundService(SqlLiteDbManager db, IOptions<Config> config, ILogger logger) {
            _config = config.Value;
            _db = db;
            _logger = logger;
        }

        private readonly SqlLiteDbManager _db;
        private readonly ILogger _logger;
        private readonly Config _config;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
            try {
                var agentService = new AgentService();

                while (!stoppingToken.IsCancellationRequested) {
                    
                    await agentService.CollectAndSendData(_db, _config, _logger);

                    _logger.Information($"Sleep. Start after -> {_config.SleepIntervalInMs} ms");
                    await Task.Delay(_config.SleepIntervalInMs, stoppingToken);
                }
                Environment.Exit(0);
            } catch (OperationCanceledException) {
                Environment.Exit(0);
            } catch (Exception ex) {
                _logger.Error(ex, "{Message}", ex.Message);
                Environment.Exit(1);
            }
        }

        public override Task StopAsync(CancellationToken cancellationToken) {
            return base.StopAsync(cancellationToken);
        }

    }
}
