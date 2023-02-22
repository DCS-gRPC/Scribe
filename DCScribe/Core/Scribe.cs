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

            _databaseClient.Host = GameServer.Database.Host;
            _databaseClient.Port = GameServer.Database.Port;
            _databaseClient.Name = GameServer.Database.Name;
            _databaseClient.Username = GameServer.Database.Username;
            _databaseClient.Password = GameServer.Database.Password;

            _logger.LogInformation("{server} Scribe Processing starting", GameServer.ShortName);
            while (!stoppingToken.IsCancellationRequested)
            {
                var scribeTokenSource = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                var scribeToken = scribeTokenSource.Token;

                // Clear the database as we start from scratch each time around
                await _databaseClient.ClearTableAsync();                

                var tasks = new[]
                {
                    ProcessAirbaseUpdates(scribeToken), // Process Airbase updates by calling an APi on a timer
                    ProcessMarkPanelUpdates(scribeToken) // Process MarkPanel updates by calling an API on a long timer
                };
                if (GameServer.UnitStream == true) {
                        /*
                        * A queue containing all the unit updates to be processed. We populate
                        * this queue in a separate thread to make sure that slowdowns in unit
                        * processing do not impact the rate at which we can receive unit updates
                        *
                        * We clear the queue each time we connect
                        */
                        var queue = new ConcurrentQueue<Unit>();
                        _rpcClient.UpdateQueue = queue;
                        tasks.Append(_rpcClient.StreamUnitsAsync(scribeToken)); // Get the events and put them into the queue
                        tasks.Append(ProcessUnitQueue(queue, scribeToken)); // Process the queue events into the units dictionary
                }
                await Task.WhenAny(tasks); // If one task finishes (usually when the RPC client gets
                                           // disconnected on mission restart
                _logger.LogInformation("{server} Scribe Processing stopping", GameServer.ShortName);
                scribeTokenSource.Cancel(); // Then cancel all of the other tasks
                // Then we wait for all of them to finish before starting the loop again.
                try
                {
                    await Task.WhenAll(tasks);
                }
                catch (Exception)
                {
                    // No-op. Exceptions have already been logged in the task
                }

                _logger.LogInformation("{server} Scribe Processing stopped", GameServer.ShortName);

                // Wait before trying again unless the entire service is shutting down.
                await Task.Delay((int)TimeSpan.FromSeconds(10).TotalMilliseconds, stoppingToken);
                _logger.LogInformation("{server} Scribe Processing restarting", GameServer.ShortName);
            }
        }

        private async Task ProcessUnitQueue(ConcurrentQueue<Unit> queue, CancellationToken scribeToken)
        {
            var unitsToUpdate = new ConcurrentDictionary<uint, Unit>();
            var unitsToDelete = new List<uint>();
            var startTime = DateTime.UtcNow;

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
                    unitsToDelete.Add(unit.Id);
                }
                else
                {
                    unitsToUpdate[unit.Id] = unit;
                }

                if (!((DateTime.UtcNow - startTime).TotalMilliseconds > 2000)) continue;
                // Every X seconds we will write the accumulated data to the database
                try
                {
                    if (unitsToUpdate.Count > 0)
                    {
                        var updates = new Unit[unitsToUpdate.Count];
                        unitsToUpdate.Values.CopyTo(updates, 0);
                        await UpdateUnitsAsync(updates.ToList(), scribeToken);
                    }

                    if (unitsToDelete.Count > 0)
                    {
                        var deletions = new uint[unitsToDelete.Count];
                        unitsToDelete.CopyTo(deletions, 0);
                        await DeleteUnitsAsync(deletions.ToList(), scribeToken);
                    }
                    // Then clear the updates and start again
                    unitsToUpdate = new ConcurrentDictionary<uint, Unit>();
                    unitsToDelete = new List<uint>();
                    startTime = DateTime.UtcNow;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "{server} Error processing queue", GameServer.ShortName);
                }
            }
        }

        private async Task UpdateUnitsAsync(List<Unit> units, CancellationToken scribeToken)
        {
            _logger.LogInformation("{server} Writing {count} unit(s) to database", GameServer.ShortName, units.Count);
            await _databaseClient.UpdateUnitsAsync(units, scribeToken);
        }

        private async Task DeleteUnitsAsync(List<uint> ids, CancellationToken scribeToken)
        {
            _logger.LogInformation("{server} Deleting {count} unit(s) from database",GameServer.ShortName, ids.Count);
            await _databaseClient.DeleteUnitsAsync(ids, scribeToken);
        }

        private async Task ProcessAirbaseUpdates(CancellationToken scribeToken)
        {
            while (!scribeToken.IsCancellationRequested)
            {
                try
                {
                    var airbases = await _rpcClient.GetAirbasesAsync();
                    if (airbases.Count == 0) continue;
                    _logger.LogInformation("{server} Writing {count} airbase(s) to database ", GameServer.ShortName, airbases.Count);
                    await _databaseClient.TruncateAirbasesAsync();
                    await _databaseClient.WriteAirbasesAsync(airbases, scribeToken);
                    await Task.Delay(TimeSpan.FromSeconds(GetTaskTimer("ProcessAirbaseUpdates")), scribeToken);
                } catch (Exception)
                {
                    // No-op. Exceptions have already been logged in the task
                }
            }
        }

        private async Task ProcessMarkPanelUpdates(CancellationToken scribeToken)
        {
            while (!scribeToken.IsCancellationRequested)
            {
                try
                {
                    var markPanels = await _rpcClient.GetMarkPanelsAsync();
                    if (markPanels.Count == 0) continue;
                    _logger.LogInformation("{server} Writing {count} markpanel(s) to database ", GameServer.ShortName, markPanels.Count);
                    await _databaseClient.TruncateMarkPanelsAsync();
                    await _databaseClient.WriteMarkPanelsAsync(markPanels, scribeToken);
                    await Task.Delay(TimeSpan.FromSeconds(GetTaskTimer("ProcessMarkPanelUpdates")), scribeToken);
                } catch (Exception)
                {
                    // No-op. Exceptions have already been logged in the task
                }
            }
        }

        private int GetTaskTimer(string taskName)
        {
            TaskTimer clsTask = GameServer.TaskTimers.FirstOrDefault(x => x.TaskName == taskName);
            return (clsTask == null) ? 60 : clsTask.Timer;
        }
    }
}
