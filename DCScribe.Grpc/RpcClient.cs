using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
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

        public ConcurrentQueue<Shared.Models.MarkPanel> MarkEventQueue { get;set; }

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

        public async Task StreamEventsAsync(CancellationToken stoppingToken)
        {
            using var channel = GrpcChannel.ForAddress($"http://{HostName}:{Port}");
            var client = new MissionService.MissionServiceClient(channel);
            try
            {
                var events = client.StreamEvents(new StreamEventsRequest(), null, null, stoppingToken);
                await foreach (var e in events.ResponseStream.ReadAllAsync(stoppingToken))
                {
                    switch (e.EventCase)
                    {
                        case StreamEventsResponse.EventOneofCase.MarkAdd:
                            var newMark = e.MarkAdd;
                            _logger.LogDebug("Adding a new mark - {mark}", newMark);
                            var newVis = newMark.VisibilityCase;
                            _logger.LogDebug("Mark visibiliy case - {case}", newVis);
                            bool isUnit = (newMark.Initiator.InitiatorCase == Dcs.Grpc.V0.Common.Initiator.InitiatorOneofCase.Unit);
                            var newGrp = -1; // not sure if 0 is a 'safe' value so went with -1 
                            var newCoa = -1; // -1 is treated as 'all' for markpanel visibility so is a 'catchall' default
                            switch (newVis)
                            {
                                case StreamEventsResponse.Types.MarkAddEvent.VisibilityOneofCase.Coalition:
                                    newCoa = (int) newMark.Coalition;
                                    break;
                                case StreamEventsResponse.Types.MarkAddEvent.VisibilityOneofCase.GroupId:
                                    newGrp = (int) newMark.GroupId;
                                    break;
                                case StreamEventsResponse.Types.MarkAddEvent.VisibilityOneofCase.None:
                                    // No-Op? Spectator slots could have special marks/drawings?
                                    break;
                                default:
                                    //No-Op because we didn't match.
                                    _logger.LogWarning("Unexpected VisibilityCase of {case}", newVis);
                                    break;
                            }
                            _logger.LogDebug("About to call enqueue for {mark}", newMark);
                            var pos = await GetMarkPanelPosition(newMark.Id);
                            if (pos is null) { break; }
                            _logger.LogDebug("With coa={coa}, grp={grp}, Id={i}, lat={lat} and lon={lon}", newCoa,newGrp,newMark.Id,pos.Latitude,pos.Longitude);
                            MarkEventQueue.Enqueue(new Shared.Models.MarkPanel
                            {
                                Coalition = newCoa,
                                GroupId = newGrp,
                                Id = newMark.Id,
                                Text = (newMark.Text is null) ? String.Empty : newMark.Text,
                                Position = pos,
                                Initiator = (isUnit && newMark.Initiator.Unit.HasPlayerName) ? newMark.Initiator.Unit.PlayerName : String.Empty
                            });
                            _logger.LogDebug("Enqueue MarkAdd event {mark}", newMark);
                            break;
                        case StreamEventsResponse.EventOneofCase.MarkChange:
                            var targetMark = e.MarkChange;
                            _logger.LogDebug("Changing a mark - {mark}", targetMark);
                            var tgtVis = targetMark.VisibilityCase;
                            _logger.LogDebug("Mark visibiliy case - {case}", tgtVis);
                            bool tgtIsUnit = (targetMark.Initiator.InitiatorCase == Dcs.Grpc.V0.Common.Initiator.InitiatorOneofCase.Unit);
                            var tgtGrp = -1; // not sure if 0 is a 'safe' value so went with -1 
                            var tgtCoa = -1; // -1 is treated as 'all' for markpanel visibility so is a 'catch all' default
                            switch (tgtVis)
                            {
                                case StreamEventsResponse.Types.MarkChangeEvent.VisibilityOneofCase.Coalition:
                                    tgtCoa = (int) targetMark.Coalition;
                                    break;
                                case StreamEventsResponse.Types.MarkChangeEvent.VisibilityOneofCase.GroupId:
                                    tgtGrp = (int) targetMark.GroupId;
                                    break;
                                case StreamEventsResponse.Types.MarkChangeEvent.VisibilityOneofCase.None:
                                    // No-Op? Spectator slots could have special marks/drawings?
                                    break;
                                default:
                                    //No-Op because we didn't match.
                                    _logger.LogWarning("Unexpected VisibilityCase of {case}", tgtVis);
                                    break;
                            }
                            _logger.LogDebug("About to call enqueue for {mark}", targetMark);
                            var tgtpos = await GetMarkPanelPosition(targetMark.Id);
                            if (tgtpos is null) { break; }
                            _logger.LogDebug("With coa={coa}, grp={grp}, Id={i}, lat={lat} and lon={lon}", tgtCoa,tgtGrp,targetMark.Id,tgtpos.Latitude,tgtpos.Longitude);
                            MarkEventQueue.Enqueue(new Shared.Models.MarkPanel
                            {
                                Coalition = tgtCoa,
                                GroupId = tgtGrp,
                                Id = targetMark.Id,
                                Text = (targetMark.Text is null) ? String.Empty : targetMark.Text,
                                Position = tgtpos,
                                Initiator = (tgtIsUnit && targetMark.Initiator.Unit.HasPlayerName) ? targetMark.Initiator.Unit.PlayerName : "unknown"
                            });
                            _logger.LogDebug("Enqueue MarkChange event {mark}", targetMark);
                            break;
                        case StreamEventsResponse.EventOneofCase.MarkRemove:
                            var delMark = e.MarkRemove;
                            MarkEventQueue.Enqueue(new Shared.Models.MarkPanel
                            {
                                Id = delMark.Id,
                                Text = (delMark.Text is null) ? String.Empty : delMark.Text,
                                Deleted = true
                            });
                            _logger.LogDebug("Enqueue MarkRemove event {mark}", delMark);
                            break;
                        //
                        // TODO: add cases for other/all event types
                        //
                        default:
                            //_logger.LogWarning("Unexpected EventCase of {case}", e.EventCase); // enable when all cases are handled.
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
                        Category = (Airbase.AirbaseCategory) (int) airbase.Category,
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
                        Text = markpanel.HasText ? markpanel.Text : String.Empty,
                        Coalition = markpanel.HasCoalition ? (int) markpanel.Coalition : -1,
                        GroupId = markpanel.HasGroupId ? (int) markpanel.GroupId : -1,
                        Initiator = (markpanel.Initiator is null) ? "unknown" : markpanel.Initiator.PlayerName
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

        public async Task<Position> GetMarkPanelPosition(uint mpid)
        {
            // Get the mark position via markpanels because the Mark event payload has changed
            var newMarkPanels = await GetMarkPanelsAsync();            
            var nmp = newMarkPanels.FirstOrDefault(i => i.Id == mpid);
            if (nmp is null) {
                _logger.LogDebug("Could not find a markpanel with id matching {markid}", mpid);
                return null;
            }
            return new Position(nmp.Position.Latitude, nmp.Position.Longitude);
        } 

    }
}
