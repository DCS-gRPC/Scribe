using System.Collections.Generic;
using RurouniJones.DCScribe.Core.Models;

namespace RurouniJones.DCScribe.Core.Clients
{
    public interface IDatabaseClient
    {
        string Host { get; set; }
        int Port { get; set; }
        string Name { get; set; }
        string Username { get; set; }
        string Password { get; set; }

        void ClearTableAsync();

        void WriteAsync(List<Unit> units);
    }
}