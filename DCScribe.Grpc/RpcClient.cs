﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Extensions.Logging;
using RurouniJones.DCScribe.Encyclopedia;
using RurouniJones.DCScribe.Shared.Interfaces;
using RurouniJones.DCScribe.Shared.Models;

namespace RurouniJones.DCScribe.Grpc
{
    public class RpcClient : IRpcClient
    {
        public ConcurrentQueue<Shared.Models.Unit> UpdateQueue { get; set; }

        public string HostName { get; set; }
        public int Port { get; set; }

        private readonly ILogger<RpcClient> _logger;
        public RpcClient(ILogger<RpcClient> logger)
        {
            _logger = logger;
        }

        public async Task StreamUnitsAsync(CancellationToken stoppingToken)
        {
            using var channel = GrpcChannel.ForAddress($"http://{HostName}:{Port}");
            var client = new Mission.MissionClient(channel);
            try
            {
                var units = client.StreamUnits(new StreamUnitsRequest
                {
                    PollRate = 1,
                    MaxBackoff = 30
                }, null, null, stoppingToken);
                await foreach (var update in units.ResponseStream.ReadAllAsync(stoppingToken))
                {
                    switch (update.UpdateCase)
                    {
                        case UnitUpdate.UpdateOneofCase.None:
                            //No-op
                            break;
                        case UnitUpdate.UpdateOneofCase.Unit:
                            var sourceUnit = update.Unit;
                            UpdateQueue.Enqueue(new Shared.Models.Unit
                            {
                                Coalition = (int)sourceUnit.Coalition,
                                Id = sourceUnit.Id,
                                Name = sourceUnit.Name,
                                Position = new Shared.Models.Position(sourceUnit.Position.Lat, sourceUnit.Position.Lon),
                                Altitude = sourceUnit.Position.Alt,
                                Callsign = sourceUnit.Callsign,
                                Type = sourceUnit.Type,
                                Player = sourceUnit.PlayerName,
                                GroupName = sourceUnit.GroupName,
                                Speed = sourceUnit.Speed,
                                Heading = sourceUnit.Heading,
                                Symbology = new MilStd2525d((int) sourceUnit.Coalition, Repository.GetUnitByDcsCode(sourceUnit.Type)?.MilStd2525D)
                            });
                            _logger.LogDebug("Enqueue unit update {unit}", sourceUnit);
                            break;
                        case UnitUpdate.UpdateOneofCase.Gone:
                            var deletedUnit = update.Gone;
                            UpdateQueue.Enqueue(new Shared.Models.Unit
                            {
                                Id = deletedUnit.Id,
                                Name = deletedUnit.Name,
                                Deleted = true
                            });
                            _logger.LogDebug("Enqueue unit deletion {unit}", deletedUnit);
                            break;
                        default:
                            _logger.LogWarning("Unexpected UnitUpdate case of {case}", update.UpdateCase);
                            break;
                    }
                }
            }
            catch (RpcException ex)
            {
                if (ex.Status.StatusCode == StatusCode.Cancelled)
                {
                    _logger.LogInformation("Shutting down gRPC connection due to {reason}", ex.Status.Detail);
                }
                else
                {
                    _logger.LogWarning(ex, "gRPC Exception");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "gRPC Exception");
            }
        }

        public async Task<List<Shared.Models.Airbase>> GetAirbasesAsync()
        {
            using var channel = GrpcChannel.ForAddress($"http://{HostName}:{Port}");
            var client = new World.WorldClient(channel);

            var airbases = new List<Shared.Models.Airbase>();
            try
            {
                var response = await client.GetAirbasesAsync(new GetAirbasesRequest());

                foreach (var airbase in response.Airbases)
                {
                    airbases.Add(new Shared.Models.Airbase
                    {
                        Name = airbase.Name,
                        Callsign = airbase.Callsign,
                        Position = new Shared.Models.Position(airbase.Position.Lat, airbase.Position.Lon),
                        Altitude = airbase.Position.Alt,
                        Category = (Shared.Models.Airbase.AirbaseCategory) (int) airbase.Category,
                        Type = airbase.DisplayName, // "Invisible FARP", "CG Ticonderoga", "Krymsk" etc.
                        Coalition =  (int) airbase.Coalition,
                        Symbology = new MilStd2525d((int) airbase.Coalition, null) // TODO Think about how to do this. Probably based off Category
                    });
                }

                return airbases;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "gRPC Exception");
                return airbases;
            }
        }
    }
}
