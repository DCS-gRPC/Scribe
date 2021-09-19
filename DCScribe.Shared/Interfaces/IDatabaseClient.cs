using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
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

        Task ClearTableAsync();

        Task TruncateUnitsAsync();

        Task TruncateAirbasesAsync();

        Task UpdateUnitsAsync(List<Unit> units, CancellationToken scribeToken);

        Task DeleteUnitsAsync(List<uint> units, CancellationToken scribeToken);

        Task WriteAirbasesAsync(List<Airbase> airbases, CancellationToken scribeToken);
    }
}