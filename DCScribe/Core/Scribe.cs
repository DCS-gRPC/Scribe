using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace RurouniJones.DCScribe.Core
{
    public class Scribe
    {
        public GameServer GameServer { get; set;}

        private readonly ILogger<Scribe> _logger;
        public Scribe(ILogger<Scribe> logger)
        {
            _logger = logger;
        }

        public async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Scribe {ShortName} running at {time}", GameServer.ShortName, DateTimeOffset.Now);
                await Task.Delay(1000, stoppingToken);
            }
            _logger.LogInformation("Stopping {ShortName} Scribe", GameServer.ShortName);
        }
    }
}
