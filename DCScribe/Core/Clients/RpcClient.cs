using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RurouniJones.DCScribe.Core.Models;

namespace RurouniJones.DCScribe.Core.Clients
{
    public class RpcClient : IRpcClient
    {
        public ConcurrentQueue<Unit> UpdateQueue { get; set; }

        public string HostName { get; set; }
        public int Port { get; set; }

        private readonly ILogger<Scribe> _logger;
        public RpcClient(ILogger<Scribe> logger)
        {
            _logger = logger;
        }

        public async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("RpcClient running at {time}", DateTimeOffset.Now);
                await Task.Delay(1000, stoppingToken);
            }
        }
    }
}
