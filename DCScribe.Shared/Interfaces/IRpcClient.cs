using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RurouniJones.DCScribe.Shared.Models;

namespace RurouniJones.DCScribe.Shared.Interfaces
{
    public interface IRpcClient
    {
        /*
         * A queue containing all the unit updates to be processed. We populate
         * this queue in a separate thread to make sure that slowdowns in unit
         * processing do not impact the rate at which we can receive unit updates
        */
        public ConcurrentQueue<Unit> UpdateQueue { get; set; }

        /*
         * Hostname of the RPC server we are connecting to
         */
        public string HostName { get; set; }

        /*
         * Port number of the RPC server we are connecting to
         */
        public int Port { get; set; }

        Task StreamUnitsAsync(CancellationToken stoppingToken);

        Task<List<Airbase>> GetAirbasesAsync();
    }
}