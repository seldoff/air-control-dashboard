using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using App.Metrics;
using App.Metrics.Counter;
using App.Metrics.Gauge;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AirControlDashboard
{
    public class AirControlService : IHostedService
    {
        private readonly IOptions<AirControlOptions> _options;
        private readonly IMetricsRoot _metricsRoot;
        private readonly ILogger<AirControlService> _logger;
        private Timer? _timer;
        private readonly Regex _linePattern = new(@"\[.+?\]\W*(.+?): (.+)", RegexOptions.Compiled);

        public AirControlService(IOptions<AirControlOptions> options, IMetricsRoot metricsRoot, ILogger<AirControlService> logger)
        {
            _options = options;
            _metricsRoot = metricsRoot;
            _logger = logger;
        }

        private void Tick(object? state)
        {
            _logger.LogInformation("Retrieving air info");
            _metricsRoot.Measure.Counter.Increment(new CounterOptions{Name = "Tick"});

            try
            {
                var arguments = new StringBuilder();
                if (_options.Value.IpAddress != null)
                {
                    arguments.Append($" --ipaddr {_options.Value.IpAddress}");
                }

                if (_options.Value.Protocol != null)
                {
                    arguments.Append($" --protocol {_options.Value.Protocol}");
                }

                var process = Process.Start(new ProcessStartInfo("airctrl", arguments.ToString())
                {
                    RedirectStandardError = true, RedirectStandardOutput = true
                });
                process!.WaitForExit();
                var output = process.StandardOutput.ReadToEnd();
                if (process.ExitCode != 0)
                {
                    _logger.LogError($"Error code {process.ExitCode}: ${output}");
                    return;
                }

                _logger.LogInformation("Successfully called airctrl");

                var readings = output.Split(Environment.NewLine)
                    .Select(l =>
                    {
                        var match = _linePattern.Match(l);
                        if (match.Success && match.Groups.Count == 3 && match.Groups[1].Success &&
                            match.Groups[2].Success)
                        {
                            return (Name: match.Groups[1].Value, Value: match.Groups[2].Value);
                        }

                        return default;
                    })
                    // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                    .Where(t => t.Name != null)
                    .ToDictionary(t => t.Name, t => t.Value);

                foreach (var valueName in _options.Value.Values)
                {
                    if (readings.TryGetValue(valueName, out var reading))
                    {
                        if (double.TryParse(reading, out var dReading))
                        {
                            _logger.LogInformation($"{valueName}: {reading}");
                            _metricsRoot.Measure.Gauge.SetValue(new GaugeOptions{Name = valueName}, dReading);
                        }
                        else
                        {
                            _logger.LogError($"{valueName}: {reading} is not number");
                            _metricsRoot.Measure.Gauge.SetValue(new GaugeOptions{Name = valueName}, -1);
                        }
                    }
                    else
                    {
                        _logger.LogError($"No reading for {valueName}");
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error when retrieving air info");
            }
            finally
            {
                _logger.LogInformation("Finished retrieving air info");
            }
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _timer = new Timer(Tick, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(_options.Value.IntervalSeconds));
            _logger.LogInformation($"Started with options {JsonSerializer.Serialize(_options.Value)}");
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _timer?.Dispose();
            _logger.LogInformation("Stopped");
            return Task.CompletedTask;
        }
    }
}
