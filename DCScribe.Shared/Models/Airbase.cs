using RurouniJones.DCScribe.Shared.Interfaces;

namespace RurouniJones.DCScribe.Shared.Models
{
    public class Airbase
    {
        public enum AirbaseCategory {
            Aerodrome = 0,
            Helipad = 1,
            Ship = 2
        }

        public string Name { get; init; }
        public string Callsign { get; init; }
        public Position Position { get; init; }
        public double Altitude { get; init; }
        public AirbaseCategory Category { get; init; }
        public string Type { get; init; }
        public int Coalition { get; init; }
        public ISymbology Symbology { get; init; }
    }
}
