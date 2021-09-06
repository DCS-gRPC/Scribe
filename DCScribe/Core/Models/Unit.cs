namespace RurouniJones.DCScribe.Core.Models
{
    public class Unit
    {
        public uint Id { get; init; }
        public string Name { get; init; }
        public string Player { get; init; }
        public string Pilot { get; init; }
        public string GroupName { get; init; }
        public int Coalition { get; init; }
        public string Type { get; init; }
        public Location Location { get; init; }
        public bool Deleted { get; set; }
    }
}
