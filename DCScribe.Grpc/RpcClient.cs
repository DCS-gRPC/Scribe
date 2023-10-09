using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Extensions.Logging;
using RurouniJones.Dcs.Grpc.V0.Mission;
using RurouniJones.Dcs.Grpc.V0.World;
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

        public async Task StreamUnitsAsync(uint pollRate, CancellationToken stoppingToken)
        {
            using var channel = GrpcChannel.ForAddress($"http://{HostName}:{Port}");
            var client = new MissionService.MissionServiceClient(channel);
            try
            {
                var units = client.StreamUnits(new StreamUnitsRequest
                {
                    PollRate = pollRate,
                    MaxBackoff = 30
                }, null, null, stoppingToken);
                await foreach (var update in units.ResponseStream.ReadAllAsync(stoppingToken))
                {
                    switch (update.UpdateCase)
                    {
                        case StreamUnitsResponse.UpdateOneofCase.None:
                            //No-op
                            break;
                        case StreamUnitsResponse.UpdateOneofCase.Unit:
                            var sourceUnit = update.Unit;
                            UpdateQueue.Enqueue(new Shared.Models.Unit
                            {
                                Coalition = (int)sourceUnit.Coalition,
                                Id = sourceUnit.Id,
                                Name = sourceUnit.Name,
                                Position = new Position(sourceUnit.Position.Lat, sourceUnit.Position.Lon),
                                Altitude = sourceUnit.Position.Alt,
                                Callsign = sourceUnit.Callsign,
                                Type = sourceUnit.Type,
                                Player = sourceUnit.PlayerName,
                                GroupName = sourceUnit.Group.Name,
                                Speed = sourceUnit.Velocity.Speed,
                                Heading = sourceUnit.Orientation.Heading,
                                Symbology = new MilStd2525d((int) sourceUnit.Coalition, Repository.GetUnitByDcsCode(sourceUnit.Type)?.MilStd2525d)
                            });
                            _logger.LogDebug("Enqueue unit update {unit}", sourceUnit);
                            break;
                        case StreamUnitsResponse.UpdateOneofCase.Gone:
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

        public async Task<List<Airbase>> GetAirbasesAsync()
        {
            using var channel = GrpcChannel.ForAddress($"http://{HostName}:{Port}");
            var client = new WorldService.WorldServiceClient(channel);

            var airbases = new List<Airbase>();
            try
            {
                var response = await client.GetAirbasesAsync(new GetAirbasesRequest());

                foreach (var airbase in response.Airbases)
                {
                    airbases.Add(new Airbase
                    {
                        Name = airbase.Name,
                        Callsign = airbase.Callsign,
                        Position = new Position(airbase.Position.Lat, airbase.Position.Lon),
                        Altitude = airbase.Position.Alt,
                        Category = (Airbase.AirbaseCategory) (int) airbase.Category - 1,
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

        public async Task<List<MarkPanel>> GetMarkPanelsAsync()
        {
            using var channel = GrpcChannel.ForAddress($"http://{HostName}:{Port}");
            var client = new WorldService.WorldServiceClient(channel);

            var markPanels = new List<MarkPanel>();
            try
            {
                var response = await client.GetMarkPanelsAsync(new GetMarkPanelsRequest());

                foreach (var markpanel in response.MarkPanels)
                {
                    markPanels.Add(new MarkPanel
                    {
                        Id = markpanel.Id,
                        Time = markpanel.Time,
                        Position = new Position(markpanel.Position.Lat, markpanel.Position.Lon),
                        Text = markpanel.Text,
                        Coalition = markpanel.HasCoalition ? (int) markpanel.Coalition : -1
                    });
                }

                return markPanels;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "gRPC Exception");
                return markPanels;
            }
        }

    }
}
