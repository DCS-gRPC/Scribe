using System.Collections.Generic;
using System.IO;
using System.Linq;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace RurouniJones.DCScribe.Encyclopedia
{
    public class Repository
    {
        private static readonly IDeserializer Deserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

        private static readonly HashSet<Unit> Aircraft = Deserializer.Deserialize<HashSet<Unit>>(
            File.ReadAllText("Data/Encyclopedia/Air.yaml"));
        private static readonly HashSet<Unit> Vehicles = Deserializer.Deserialize<HashSet<Unit>>(
            File.ReadAllText("Data/Encyclopedia/Land.yaml"));
        private static readonly HashSet<Unit> Watercraft = Deserializer.Deserialize<HashSet<Unit>>(
            File.ReadAllText("Data/Encyclopedia/Sea.yaml"));

        private static readonly HashSet<Unit> Units = BuildUnitHashset();

        private static HashSet<Unit> BuildUnitHashset()
        {
            var set = new HashSet<Unit>(Aircraft);
            set.UnionWith(Vehicles);
            set.UnionWith(Watercraft);
            return set;
        }

        public static Unit GetUnitByDcsCode(string code)
        {
            return Units.FirstOrDefault(x => x.DcsCodes.Contains(code));
        }
    }
}