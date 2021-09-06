using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using RurouniJones.DCScribe.Core.Models;

namespace RurouniJones.DCScribe.Core.Clients
{
    public class DatabaseClient : IDatabaseClient
    {
        public string Host { get; set; }
        public int Port { get; set; }
        public string Name { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }

        private readonly ILogger<Scribe> _logger;
        public DatabaseClient(ILogger<Scribe> logger)
        {
            _logger = logger;
        }

        public void ClearTableAsync()
        {

        }


        public void WriteAsync(List<Unit> units)
        {
        }
    }
}