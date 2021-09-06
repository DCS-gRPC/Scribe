using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using RurouniJones.DCScribe.Shared.Interfaces;
using RurouniJones.DCScribe.Shared.Models;

namespace RurouniJones.DCScribe.Postgres
{
    public class DatabaseClient : IDatabaseClient
    {
        public string Host { get; set; }
        public int Port { get; set; }
        public string Name { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }

        private readonly ILogger<DatabaseClient> _logger;
        public DatabaseClient(ILogger<DatabaseClient> logger)
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