﻿using System;
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

            public static class Air
            {
                public static class Military
                {
                    public enum EntityType
                    {
                        FixedWing = 1,
                        RotaryWing = 2,
                        UnMannedAircraft = 3,
                        VerticalTakeOffUUnMannedAircraft = 4,
                        LighterThanAir = 5,
                        AirShip = 6,
                        TetheredLighterThanAir = 7
                    }

                    public static class FixedWing
                    {
                        public enum EntitySubType
                        {
                            MedicalEvacuation = 1,
                            AttackStrike = 2,
                            Bomber = 3,
                            Fighter = 4,
                            FighterBomber = 5,
                            Cargo = 7,
                            ElectronicCombat = 8,
                            Tanker = 9,
                            Patrol = 10,
                            Reconnaissance = 11,
                            Trainer = 12,
                            Utility = 13,
                            VSTOL = 14,
                            AirborneCommandPost = 15,
                            AirborneEarlyWarning = 16,
                            AntiSurfaceWarfare = 17,
                            AntiSubmarineWarfare = 18,
                            Communications = 19,
                            CombatSearchAndRescue = 20,
                            ElectronicSupport = 21,
                            Government = 22,
                            MineCounterMeasures = 23,
                            PersonnelRecovery = 24,
                            SearchAndRescue = 25,
                            SpecialOperationsForces = 26,
                            UltraLight = 27,
                            PhotographicReconnaissance = 28,
                            VeryImportantPerson = 29,
                            SuppressionOfEnemyAirDefense = 30,
                            Passenger = 31,
                            Escort = 32,
                            ElectronicAttack = 33,
                        }
                    }
                }

                public static class Civilian
                {
                    public enum EntityType
                    {
                        FixedWing = 1,
                        RotaryWing = 2,
                        UnMannedAircraft = 3,
                        VerticalTakeOffUUnMannedAircraft = 4,
                        LighterThanAir = 5,
                        AirShip = 6,
                        TetheredLighterThanAir = 7
                    }
                }
            }
        }

        public int Version { get; set; } = 10;
        public Enums.Context Context { get; set; } = Enums.Context.Reality;
        public Enums.StandardIdentity StandardIdentity { get; set; } = Enums.StandardIdentity.Pending;
        public Enums.SymbolSet SymbolSet { get; set; } = Enums.SymbolSet.LandUnits;
        public Enums.Status Status { get; set; } = Enums.Status.Present;
        // ReSharper disable once InconsistentNaming
        public int HQTFDummy { get; set; }
        public int Amplifier { get; set; }
        public Enums.Entity Entity { get; set; } = Enums.Entity.Military;
        public int EntityType { get; set; }
        public int EntitySubType { get; set; }
        public int SectorOneModifier { get; set; }
        public int SectorTwoModifier { get; set; }

        public MilStd2525d(int dcsCoalition, string code)
        {
            StandardIdentity = dcsCoalition switch // 2-3 is the Standard Identity which we calculate ourselves
            {
                0 => Enums.StandardIdentity.Neutral,
                1 => Enums.StandardIdentity.HostileFaker,
                2 => Enums.StandardIdentity.Friend,
                _ => StandardIdentity
            };

            if (code == null) return;

            Version = int.Parse(code.Substring(0, 2)); // 0-1
            // 2-3 See above
            SymbolSet = Enum.Parse<Enums.SymbolSet>(code.Substring(4, 2)); // 4-5
            Status = Enum.Parse<Enums.Status>(code.Substring(6, 1)); // 6
            HQTFDummy = int.Parse(code.Substring(7, 1)); // 7
            Amplifier = int.Parse(code.Substring(8, 2)); // 8-9
            Entity = Enum.Parse<Enums.Entity>(code.Substring(10, 2)); // 10-11
            EntityType = int.Parse(code.Substring(12, 2)); // 12-13
            EntitySubType = int.Parse(code.Substring(14, 2)); // 14-15
            SectorOneModifier = int.Parse(code.Substring(16, 2)); // 16-17
            SectorTwoModifier = int.Parse(code.Substring(18, 2)); // 18-19
        }

        public override string ToString()
        {
            return new StringBuilder(Version)
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
