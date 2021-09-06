using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Extensions.Logging;
using RurouniJones.DCScribe.Shared.Interfaces;

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

        public async Task ExecuteAsync(CancellationToken stoppingToken)
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
                                Deleted = false
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
    }
}
