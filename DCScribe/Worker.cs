using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RurouniJones.DCScribe.Core;

namespace RurouniJones.DCScribe
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly List<Scribe> _scribes = new();

        public Worker(ILogger<Worker> logger, IOptions<Configuration> configuration, ScribeFactory scribeFactory)
        {
            _logger = logger;

            foreach (var gameServer in configuration.Value.GameServers)
            {
                _logger.LogInformation("Instantiating {shortName} Scribe", gameServer.ShortName);
                var scribe = scribeFactory.CreateScribe();
                scribe.GameServer = gameServer;
                _scribes.Add(scribe);
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var scribeTasks = new List<Task>();
            _scribes.ForEach(s => scribeTasks.Add(s.ExecuteAsync(stoppingToken)));
            await Task.WhenAll(scribeTasks);
        }
    }
}
