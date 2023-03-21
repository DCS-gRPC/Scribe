using System.Collections.Generic;

namespace RurouniJones.DCScribe.Core
{
    public sealed class Configuration
    {
        public List<GameServer> GameServers { get; init; }
    }

    public sealed class GameServer
    {
        public string Name { get; set; }
        public string ShortName { get; set; }
        public Database Database { get; set; }
        public Rpc Rpc { get; set; }
        public Tasks Tasks { get;set; }
    }

    public sealed class Database
    {
        public string Host { get; set; }
        public int Port { get; set; }
        public string Name { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
    }

    public sealed class Rpc
    {
        public string Host { get; set; }
        public int Port { get; set; }
    }

    public sealed class Tasks
    {
        public RecordUnitPositions RecordUnitPositions { get;set; }
        public RecordEvents RecordEvents { get; set; }
        public ProcessAirbaseUpdates ProcessAirbaseUpdates { get;set; }
    }

    public sealed class RecordEvents
    {
        public bool Enabled { get; set; }
        public RecordMarkPanels RecordMarkPanels { get; set; }
    }

    public sealed class RecordUnitPositions
    {
        public bool Enabled { get;set; }
        public uint PollRate { get;set; }
        public int Timer { get;set; }      
    }

    public sealed class ProcessAirbaseUpdates
    {
        public bool Enabled { get;set; }
        public int Timer { get;set; }        
    }

    public sealed class RecordMarkPanels
    {
        public bool Enabled { get; set; }
        public int Timer { get; set; }
    }
}
