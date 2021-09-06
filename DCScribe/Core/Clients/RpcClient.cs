using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Extensions.Logging;
using RurouniJones.DCScribe.Grpc;
using Unit = RurouniJones.DCScribe.Core.Models.Unit;

namespace RurouniJones.DCScribe.Core.Clients
{
    public class RpcClient : IRpcClient
    {
        public ConcurrentQueue<Unit> UpdateQueue { get; set; }

        public string HostName { get; set; }
        public int Port { get; set; }

        private readonly ILogger<Scribe> _logger;
        public RpcClient(ILogger<Scribe> logger)
        {
            _logger = logger;
        }

        public async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var channel = GrpcChannel.ForAddress($"http://{HostName}:{Port}");
            var client = new Mission.MissionClient(channel);
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
                        UpdateQueue.Enqueue(new Unit
                        {
                            Coalition = (int) sourceUnit.Coalition,
                            Id = sourceUnit.Id,
                            Name = sourceUnit.Name,
                            Location = new  Models.Location(sourceUnit.Position.Lat, sourceUnit.Position.Lon, sourceUnit.Position.Alt),
                            Pilot = sourceUnit.Callsign,
                            Type = sourceUnit.Type,
                            Player = sourceUnit.PlayerName,
                            GroupName = sourceUnit.GroupName,
                            Deleted = false
                        });
                        _logger.LogWarning("Enqueue unit update {unit}", sourceUnit);
                        break;
                    case UnitUpdate.UpdateOneofCase.Gone:
                        var deletedUnit = update.Gone;
                        UpdateQueue.Enqueue(new Unit
                        {
                            Id = deletedUnit.Id,
                            Name = deletedUnit.Name,
                            Deleted = true
                        });
                        _logger.LogWarning("Enqueue unit deletion {unit}", deletedUnit);
                        break;
                    default:
                        _logger.LogWarning("Unexpected UnitUpdate case of {case}", update.UpdateCase);
                        break;
                }
            }
        }
    }
}
