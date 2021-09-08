using System.Text;
using RurouniJones.DCScribe.Shared.Interfaces;

namespace RurouniJones.DCScribe.Shared.Models
{
    public class MilStd2525d : ISymbology
    {
        public class Enums
        {
            public enum Context {
                Reality = 0,
                Exercise = 1,
                Simulation = 2
            }

            public enum StandardIdentity
            {
                Pending = 0,
                Unknown = 1,
                AssumedFriend = 2,
                Friend = 3,
                Neutral = 4,
                SuspectJoker = 5,
                HostileFaker = 6
            }

            public enum SymbolSet
            {
                Air = 1,
                AirMissile = 2,
                Space = 5,
                SpaceMissile = 6,
                LandUnits = 10,
                LandCivilian = 11,
                LandEquipment = 15,
                LandInstallation = 20,
                ControlMeasure = 25,
                SeaSurface = 30,
                SeaSubSurface = 35,
                MineWarfare = 36,
                Activities = 40,
                MeteorologicalAtmospheric = 45,
                MeteorologicalOceanographic = 46,
                MeteorologicalSpace = 47,
                SignalsIntelligenceSpace = 50,
                SignalsIntelligenceAir = 51,
                SignalsIntelligenceLand = 52,
                SignalsIntelligenceSurface = 53,
                SignalsIntelligenceSubSurface = 54,
                Cyberspace = 60
            }

            public enum Status {
                Present = 0,
                PlannedAnticipatedSuspect = 1,
                PresentFullyCapable = 2,
                PresentDamaged = 3,
                PresentDestroyed = 4,
                PresentFullToCapacity = 5
            }

            // HQTFDummy skipped
            // Amplifiers skipped

            public enum Entity
            {
                Military = 11,
                Civilian = 12,
                Weapon = 13,
                ManualTrack = 14
            }
        }

        public Enums.Context Context { get; set; } = Enums.Context.Reality;
        public Enums.StandardIdentity StandardIdentity { get; set; } = Enums.StandardIdentity.Pending;
        public Enums.SymbolSet SymbolSet { get; set; } = Enums.SymbolSet.LandUnits;
        public Enums.Status Status { get; set; } = Enums.Status.Present;
        // ReSharper disable once InconsistentNaming
        public int HQTFDummy { get; set; } = 0;
        public int Amplifier => 0;
        public Enums.Entity Entity { get; set; } = Enums.Entity.Military;
        public int EntityType { get; set; } = 0;
        public int EntitySubType { get; set; } = 0;
        public int SectorOneModifier { get; set; } = 0;
        public int SectorTwoModifier { get; set; } = 0;

        public MilStd2525d(int dcsCoalition)
        {
            StandardIdentity = dcsCoalition switch
            {
                0 => Enums.StandardIdentity.Neutral,
                1 => Enums.StandardIdentity.HostileFaker,
                2 => Enums.StandardIdentity.Friend,
                _ => StandardIdentity
            };
        }

        public override string ToString()
        {
            return new StringBuilder(10)
                .Append(StandardIdentity.ToString("00"))
                .Append(SymbolSet.ToString("00"))
                .Append(Status.ToString("0"))
                .Append(HQTFDummy.ToString("0"))
                .Append(Amplifier.ToString("00"))
                .Append(Entity.ToString("00"))
                .Append(EntityType.ToString("00"))
                .Append(EntitySubType.ToString("00"))
                .Append(SectorOneModifier.ToString("00"))
                .Append(SectorTwoModifier.ToString("00"))
                .ToString();
        }
    }
}
