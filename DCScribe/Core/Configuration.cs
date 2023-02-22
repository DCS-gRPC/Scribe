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
        public List<TaskTimer> TaskTimers { get;set; }
        public bool UnitStream { get; set; }
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

    public sealed class TaskTimer
    {
        public string TaskName { get;set; }
        public int Timer { get;set; }
        
    }
}
