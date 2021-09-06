using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RurouniJones.DCScribe.Shared.Interfaces;
using RurouniJones.DCScribe.Shared.Models;

namespace RurouniJones.DCScribe.Core
{
    public class Scribe
    {
        /*
         * Configuration for the GameServer including DB and RPC information
         */
        public GameServer GameServer { get; set; }

        /*
         * The RPC client that connects to the server and receives the unit updates
         * to put into the update queue
         */
        private readonly IRpcClient _rpcClient;

        /*
         * The client that handles database actions.
         */
        private readonly IDatabaseClient _databaseClient;

        private readonly ILogger<Scribe> _logger;
        public Scribe(ILogger<Scribe> logger, IRpcClient rpcClient, IDatabaseClient databaseClient)
        {
            _logger = logger;
            _rpcClient = rpcClient;
            _databaseClient = databaseClient;
        }

        public async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _rpcClient.HostName = GameServer.Rpc.Host;
            _rpcClient.Port = GameServer.Rpc.Port;

            while (!stoppingToken.IsCancellationRequested)
            {
                var scribeTokenSource = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                var scribeToken = scribeTokenSource.Token;
                
                // Clear the database and units as we start from scratch each time around
                _databaseClient.ClearTableAsync();
                var units = new ConcurrentDictionary<uint, Unit>();

                /*
                 * A queue containing all the unit updates to be processed. We populate
                 * this queue in a separate thread to make sure that slowdowns in unit
                 * processing do not impact the rate at which we can receive unit updates
                 *
                 * We clear the queue each time we connect
                 */
                var queue = new ConcurrentQueue<Unit>();
                _rpcClient.UpdateQueue = queue;

                var tasks = new List<Task>
                {
                    _rpcClient.ExecuteAsync(scribeToken), // Get the events and put them into the queue
                    ProcessQueue(queue, units, scribeToken), // Process the queue events into the units dictionary
                    WriteToDatabaseAsync(units, scribeToken) // Periodically write the units to the database
                };

                await Task.WhenAny(tasks); // If one task finishes (usually when the RPC client gets disconnected on
                                           // mission restart
                scribeTokenSource.Cancel(); // Then cancel all of the other tasks
                await Task.WhenAll(tasks); // Then we wait for all of them to finish before starting the loop again
            }
        }

        private static async Task ProcessQueue(ConcurrentQueue<Unit> queue, ConcurrentDictionary<uint, Unit> units,
            CancellationToken scribeToken)
        {
            while (!scribeToken.IsCancellationRequested)
            {
                queue.TryDequeue(out var unit);
                if (unit == null)
                {
                    await Task.Delay(5, scribeToken);
                    continue;
                }

                if (unit.Deleted)
                {
                    units[unit.Id].Deleted = true;
                }
                else
                {
                    units[unit.Id] = unit;
                }
            }
        }

        private async Task WriteToDatabaseAsync(ConcurrentDictionary<uint, Unit> units, CancellationToken scribeToken)
        {
            while (!scribeToken.IsCancellationRequested)
            {
                _logger.LogInformation("Writing {count} units to database at {time}",units.Count, DateTimeOffset.Now);
                _databaseClient.WriteAsync(units.Values.ToList());
                await Task.Delay(1000, scribeToken);
            }
        }
    }
}
