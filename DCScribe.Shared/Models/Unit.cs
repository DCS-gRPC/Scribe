using RurouniJones.DCScribe.Shared.Interfaces;

namespace RurouniJones.DCScribe.Shared.Models
{
    public class Unit
    {
        public uint Id { get; init; }
        public string Name { get; init; }
        public string Player { get; init; }
        public string Callsign { get; init; }
        public string GroupName { get; init; }
        public int Coalition { get; init; }
        public string Type { get; init; }
        public Position Position { get; init; }
        public double Altitude { get; init; }
        public double Heading { get; init; }
        public double Speed { get; init; }
        public bool Deleted { get; init; }
        public ISymbology Symbology { get; init; }
    }
}
