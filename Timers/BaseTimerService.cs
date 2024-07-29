using Microsoft.Extensions.Hosting;
using Serilog;

namespace qoldau.suap.miniagent.Timers {
    public abstract class BaseTimerService : BackgroundService {
        public string TimerName { get; }

        private readonly BaseTimerServiceSettings _delaySettings;
        private readonly ILogger _logger;

        public BaseTimerService(string timerName, BaseTimerServiceSettings delaySettings, ILogger logger) {
            TimerName = timerName;
            _logger = logger;
            _delaySettings = delaySettings;
        }

        public abstract Task DoAction();

        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
            _logger.Debug($"{TimerName} is starting.");

            stoppingToken.Register(() =>
                _logger.Debug($" {TimerName} background task is stopping."));

            while (!stoppingToken.IsCancellationRequested) {
                _logger.Debug($"{TimerName} task doing background work.");

                try {
                    await DoAction();
                    await Task.Delay(_delaySettings.Delay, stoppingToken);
                } catch (TaskCanceledException exception) {
                    _logger.Warning(exception, $" {TimerName} TaskCanceledException Error", exception.Message);
                } catch (Exception exception) {
                    _logger.Warning(exception, $" {TimerName} Exception", exception.Message);
                }
            }

            _logger.Debug($"{TimerName} background task is stopping.");
        }
    }
    public record BaseTimerServiceSettings(TimeSpan Delay);
}
