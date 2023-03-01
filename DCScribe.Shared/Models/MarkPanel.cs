using RurouniJones.DCScribe.Shared.Interfaces;

namespace RurouniJones.DCScribe.Shared.Models
{
    public class MarkPanel
    {
        public uint Id { get; init; }
        public double Time { get; init; }
        public Position Position { get; init; }
        public string Text { get; init; }
        public string Initiator { get; init; }
        public int Coalition { get; init; }
        public int GroupId { get; init; }
        public bool Deleted { get;init; }
    }
}