using System.Collections.Generic;
using RurouniJones.DCScribe.Shared.Models;

namespace RurouniJones.DCScribe.Shared.Interfaces
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