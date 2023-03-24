﻿using System;
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

                var tasks = new List<Task>();
                
                if (GameServer.Tasks.ProcessAirbaseUpdates.Enabled) {
                    tasks.Add(ProcessAirbaseUpdates(scribeToken)); // Process Airbase updates by calling an APi on a timer
                }
                if (GameServer.Tasks.RecordUnitPositions.Enabled) {
                        /*
                        * A queue containing all the unit updates to be processed. We populate
                        * this queue in a separate thread to make sure that slowdowns in unit
                        * processing do not impact the rate at which we can receive unit updates
                        *
                        * We clear the queue each time we connect
                        */
                        var queue = new ConcurrentQueue<Unit>();
                        _rpcClient.UpdateQueue = queue;
                        var pollRate = GameServer.Tasks.RecordUnitPositions.PollRate;
                        tasks.Add(_rpcClient.StreamUnitsAsync(pollRate, scribeToken)); // Get the events and put them into the queue
                        tasks.Add(ProcessUnitQueue(queue, scribeToken)); // Process the queue events into the units dictionary
                }
                if (GameServer.Tasks.RecordEvents.Enabled) { // Is EventStreaming enabled in config?
                    if (GameServer.Tasks.RecordEvents.RecordMarkPanels.Enabled) { // Are MarkPanel events to be recorded?
                        var evQueue = new ConcurrentQueue<MarkPanel>();
                        _rpcClient.MarkEventQueue = evQueue;
                        tasks.Add(GetCurrentMarkPanelDataAsync(scribeToken)); // Get the initial state of markpanels in mission
                        tasks.Add(_rpcClient.StreamEventsAsync(scribeToken)); // Start streaming and handling events
                        tasks.Add(ProcessMarkPanelQueue(evQueue,scribeToken)); // Process the eventqueue for markpanels
                    }
                    //
                    // TODO: Add other event type handlers
                    //
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

                if (!((DateTime.UtcNow - startTime).TotalMilliseconds > (GameServer.Tasks.RecordUnitPositions.Timer*1000))) continue;
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
                    await Task.Delay(TimeSpan.FromSeconds(GameServer.Tasks.ProcessAirbaseUpdates.Timer), scribeToken);
                } catch (Exception)
                {
                    // No-op. Exceptions have already been logged in the task
                }
            }
        }

        private async Task GetCurrentMarkPanelDataAsync(CancellationToken scribeToken)
        {            
            while (!scribeToken.IsCancellationRequested)
            {
                try 
                {
                    var markPanels = await _rpcClient.GetMarkPanelsAsync();
                    if (markPanels.Count == 0) {
                        await Task.Delay(TimeSpan.FromSeconds(10), scribeToken);
                        continue;
                    }
                    _logger.LogInformation("{server} Writing {count} markpanel(s) to database ", GameServer.ShortName, markPanels.Count);
                    await _databaseClient.TruncateMarkPanelsAsync();
                    await _databaseClient.WriteMarkPanelsAsync(markPanels, scribeToken);
                    await Task.Delay(TimeSpan.FromSeconds(600), scribeToken);
                } catch (Exception)
                {
                    // No-op. Exceptions have already been logged in the task
                }
            }
        }

        private async Task ProcessMarkPanelQueue(ConcurrentQueue<MarkPanel> mpqueue, CancellationToken scribeToken)
        {
            var mpsToUpdate = new ConcurrentDictionary<uint, MarkPanel>();
            var mpsToDelete = new List<uint>();
            var startTime = DateTime.UtcNow;

            while (!scribeToken.IsCancellationRequested)
            {
                mpqueue.TryDequeue(out var mp);
                if (mp is null)
                {
                    if (mpsToUpdate.Count == 0 && mpsToDelete.Count == 0) {
                        _logger.LogDebug("{server} MarkPanel queue empty - waiting 10 seconds to retry....", GameServer.ShortName);
                        await Task.Delay(10000, scribeToken);
                        continue;
                    }
                } else {
                    if (mp.Deleted)
                    {
                        mpsToDelete.Add(mp.Id);
                    }
                    else
                    {
                        mpsToUpdate[mp.Id] = mp;
                    }
                }
                if (!((DateTime.UtcNow - startTime).TotalMilliseconds > 40)) { // minimum 40ms between DB writes
                    _logger.LogDebug("{server} WriteToDB timeout not exceeded - {msSinceLastUpdate} < 40", GameServer.ShortName, (DateTime.UtcNow - startTime).TotalMilliseconds);
                    continue;
                }
                // Every X seconds we will write the accumulated data to the database
                try
                {
                    if (mpsToUpdate.Count > 0)
                    {
                        //var mpupdates = new MarkPanel[mpsToUpdate.Count];
                        //mpsToUpdate.Values.CopyTo(mpupdates, 0);
                        var mpupdates = mpsToUpdate.Values.ToList<MarkPanel>();
                        await UpdateMarkPanelsAsync(mpupdates, scribeToken);
                    }

                    if (mpsToDelete.Count > 0)
                    {
                        //var mpdeletions = new uint[mpsToDelete.Count];
                        //mpsToDelete.CopyTo(mpdeletions, 0);
                        var mpdeletions = mpsToDelete;
                        await DeleteMarkPanelsAsync(mpdeletions, scribeToken);
                    }
                    // Then clear the updates and start again
                    mpsToUpdate = new ConcurrentDictionary<uint, MarkPanel>();
                    mpsToDelete = new List<uint>();
                    startTime = DateTime.UtcNow;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "{server} Error processing event queue", GameServer.ShortName);
                }
            }
        }

        private async Task UpdateMarkPanelsAsync(List<MarkPanel> markpanels, CancellationToken scribeToken)
        {
            _logger.LogInformation("{server} Writing {count} markpanel(s) to database", GameServer.ShortName, markpanels.Count);
            await _databaseClient.UpdateMarkPanelsAsync(markpanels, scribeToken);
        }

        private async Task DeleteMarkPanelsAsync(List<uint> ids, CancellationToken scribeToken)
        {
            _logger.LogInformation("{server} Deleting {count} markpanel(s) from database",GameServer.ShortName, ids.Count);
            await _databaseClient.DeleteMarkPanelsAsync(ids, scribeToken);
        }
    }
}
