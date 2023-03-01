using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NetTopologySuite.Geometries;
using Npgsql;
using RurouniJones.DCScribe.Shared.Interfaces;
using RurouniJones.DCScribe.Shared.Models;

namespace RurouniJones.DCScribe.Postgres
{
    public class DatabaseClient : IDatabaseClient
    {
        public string Host { get; set; }
        public int Port { get; set; }
        public string Name { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }

        private readonly ILogger<DatabaseClient> _logger;
        public DatabaseClient(ILogger<DatabaseClient> logger)
        {
            _logger = logger;
        }

        public async Task ClearTableAsync()
        {
            await TruncateUnitsAsync();
            await TruncateAirbasesAsync();
            await TruncateMarkPanelsAsync();
        }

        public async Task TruncateUnitsAsync()
        {
            try
            {
                await using var conn = new NpgsqlConnection(GetConnectionString());
                await conn.OpenAsync();

                await using var truncateUnits = new NpgsqlCommand("TRUNCATE TABLE units", conn);
                await truncateUnits.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Database Exception");
            }

        }

        public async Task TruncateAirbasesAsync()
        {
            try
            {
                await using var conn = new NpgsqlConnection(GetConnectionString());
                await conn.OpenAsync();

                await using var truncateAirbases = new NpgsqlCommand("TRUNCATE TABLE airbases", conn);
                await truncateAirbases.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Database Exception");
            }
        }

        public async Task TruncateMarkPanelsAsync()
        {
            try
            {
                await using var conn = new NpgsqlConnection(GetConnectionString());
                await conn.OpenAsync();

                await using var truncateMarkPanels = new NpgsqlCommand("TRUNCATE TABLE markpanels", conn);
                await truncateMarkPanels.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Database Exception");
            }
        }

        public async Task UpdateUnitsAsync(List<Unit> units, CancellationToken scribeToken)
        {
            try
            {
                await using var conn = new NpgsqlConnection(GetConnectionString());
                await conn.OpenAsync(scribeToken);
                conn.TypeMapper.UseNetTopologySuite(geographyAsDefault: true);

                await using var command = new NpgsqlCommand(
                @"INSERT INTO units (id, ""position"", altitude, type, name, callsign, player, group_name,
                        coalition, heading, speed, updated_at, context, standard_identity, symbol_set, status,
                        hqtf_dummy, amplifier, entity, entity_type, entity_sub_type, sector_one_modifier,
                        sector_two_modifier)
                        SELECT * FROM unnest(@i, @l, @a, @t, @n, @pi, @pl, @g, @c, @h, @s, @u, @sc, @si, @ss, @st,
                        @sh, @sa, @se, @sf, @sg, @so, @sp)
                        ON CONFLICT ON CONSTRAINT units_pkey
                        DO UPDATE SET ""position"" = EXCLUDED.position, altitude = EXCLUDED.altitude, 
                        heading = EXCLUDED.heading, speed = EXCLUDED.speed, updated_at = EXCLUDED.updated_at", conn);

                /*
                 * The following fields come directly from DCS
                 */
                command.Parameters.Add(new NpgsqlParameter<int[]>("i", units.Select(e =>
                    (int) e.Id).ToArray()));
                command.Parameters.Add(new NpgsqlParameter<Point[]>("l", units.Select(e =>
                    new Point(new Coordinate(e.Position.Longitude, e.Position.Latitude))).ToArray()));
                command.Parameters.Add(new NpgsqlParameter<double[]>("a", units.Select(e =>
                    e.Altitude).ToArray()));
                command.Parameters.Add(new NpgsqlParameter<string[]>("t", units.Select(e =>
                    e.Type).ToArray()));
                command.Parameters.Add(new NpgsqlParameter<string[]>("n", units.Select(e =>
                    e.Name).ToArray()));
                command.Parameters.Add(new NpgsqlParameter<string[]>("pi", units.Select(e =>
                    e.Callsign).ToArray()));
                command.Parameters.Add(new NpgsqlParameter<string[]>("pl", units.Select(e =>
                    e.Player).ToArray()));
                command.Parameters.Add(new NpgsqlParameter<string[]>("g", units.Select(e =>
                    e.GroupName).ToArray()));
                command.Parameters.Add(new NpgsqlParameter<int[]>("c", units.Select(e =>
                    e.Coalition).ToArray()));
                command.Parameters.Add(new NpgsqlParameter<int[]>("h", units.Select(e =>
                    (int) Math.Round(e.Heading)).ToArray()));
                command.Parameters.Add(new NpgsqlParameter<int[]>("s", units.Select(e =>
                    (int) Math.Round(e.Speed)).ToArray()));
                command.Parameters.Add(new NpgsqlParameter<DateTime[]>("u", units.Select(e =>
                    DateTime.UtcNow).ToArray()));

                /*
                 * The following fields are calculated using MilStd-2525D based on the unit
                 */
                command.Parameters.Add(new NpgsqlParameter<int[]>("sc", units.Select(e =>
                    Convert.ToInt32(((MilStd2525d) e.Symbology).Context)).ToArray()));
                command.Parameters.Add(new NpgsqlParameter<int[]>("si", units.Select(e =>
                    Convert.ToInt32(((MilStd2525d) e.Symbology).StandardIdentity)).ToArray()));
                command.Parameters.Add(new NpgsqlParameter<int[]>("ss", units.Select(e =>
                    Convert.ToInt32(((MilStd2525d) e.Symbology).SymbolSet)).ToArray()));
                command.Parameters.Add(new NpgsqlParameter<int[]>("st", units.Select(e =>
                    Convert.ToInt32(((MilStd2525d) e.Symbology).Status)).ToArray()));
                command.Parameters.Add(new NpgsqlParameter<int[]>("sh", units.Select(e =>
                    Convert.ToInt32(((MilStd2525d) e.Symbology).HQTFDummy)).ToArray()));
                command.Parameters.Add(new NpgsqlParameter<int[]>("sa", units.Select(e =>
                    Convert.ToInt32(((MilStd2525d) e.Symbology).Amplifier)).ToArray()));
                command.Parameters.Add(new NpgsqlParameter<int[]>("se", units.Select(e =>
                    Convert.ToInt32(((MilStd2525d) e.Symbology).Entity)).ToArray()));
                command.Parameters.Add(new NpgsqlParameter<int[]>("sf", units.Select(e =>
                    Convert.ToInt32(((MilStd2525d) e.Symbology).EntityType)).ToArray()));
                command.Parameters.Add(new NpgsqlParameter<int[]>("sg", units.Select(e =>
                    Convert.ToInt32(((MilStd2525d) e.Symbology).EntitySubType)).ToArray()));
                command.Parameters.Add(new NpgsqlParameter<int[]>("so", units.Select(e =>
                    Convert.ToInt32(((MilStd2525d) e.Symbology).SectorOneModifier)).ToArray()));
                command.Parameters.Add(new NpgsqlParameter<int[]>("sp", units.Select(e =>
                    Convert.ToInt32(((MilStd2525d) e.Symbology).SectorTwoModifier)).ToArray()));
                await command.ExecuteNonQueryAsync(scribeToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Database Exception");
            }
        }

        public async Task DeleteUnitsAsync(List<uint> units, CancellationToken scribeToken)
        {
            try
            {
                await using var conn = new NpgsqlConnection(GetConnectionString());
                await conn.OpenAsync(scribeToken);

                await using var command = new NpgsqlCommand(
                    @"DELETE FROM units WHERE id IN (SELECT * FROM unnest(@a))", conn);

                command.Parameters.Add(new NpgsqlParameter<int[]>("a", units.Select(u =>
                    (int) u).ToArray()));

                await command.ExecuteNonQueryAsync(scribeToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Database Exception");
            }
        }

        public async Task WriteAirbasesAsync(List<Airbase> airbases, CancellationToken scribeToken)
        {
            try
            {
                await using var conn = new NpgsqlConnection(GetConnectionString());
                await conn.OpenAsync(scribeToken);
                conn.TypeMapper.UseNetTopologySuite(geographyAsDefault: true);

                await using var command = new NpgsqlCommand(
                @"INSERT INTO airbases (name, callsign, ""position"", altitude, ""category"", ""type"", coalition,
                        updated_at, context, standard_identity, symbol_set, status, hqtf_dummy, amplifier, entity,
                        entity_type, entity_sub_type, sector_one_modifier,
                        sector_two_modifier)
                        SELECT * FROM unnest(@n, @c, @p, @a, @ca, @t, @co, @u, @sc, @si, @ss, @st, @sh, @sa, @se, @sf,
                        @sg, @so, @sp)", conn);

                /*
                 * The following fields come directly from DCS
                 */
                command.Parameters.Add(new NpgsqlParameter<string[]>("n", airbases.Select(e =>
                    e.Name).ToArray()));
                command.Parameters.Add(new NpgsqlParameter<string[]>("c", airbases.Select(e =>
                    e.Callsign).ToArray()));
                command.Parameters.Add(new NpgsqlParameter<Point[]>("p", airbases.Select(e =>
                    new Point(new Coordinate(e.Position.Longitude, e.Position.Latitude))).ToArray()));
                command.Parameters.Add(new NpgsqlParameter<double[]>("a", airbases.Select(e =>
                    e.Altitude).ToArray()));
                command.Parameters.Add(new NpgsqlParameter<string[]>("ca", airbases.Select(e =>
                    e.Category.ToString()).ToArray()));
                command.Parameters.Add(new NpgsqlParameter<string[]>("t", airbases.Select(e =>
                    e.Type).ToArray()));
                command.Parameters.Add(new NpgsqlParameter<int[]>("co", airbases.Select(e =>
                    e.Coalition).ToArray()));
                command.Parameters.Add(new NpgsqlParameter<DateTime[]>("u", airbases.Select(e =>
                    DateTime.UtcNow).ToArray()));

                /*
                 * The following fields are calculated using MilStd-2525D based on the unit
                 */
                command.Parameters.Add(new NpgsqlParameter<int[]>("sc", airbases.Select(e =>
                    Convert.ToInt32(((MilStd2525d) e.Symbology).Context)).ToArray()));
                command.Parameters.Add(new NpgsqlParameter<int[]>("si", airbases.Select(e =>
                    Convert.ToInt32(((MilStd2525d) e.Symbology).StandardIdentity)).ToArray()));
                command.Parameters.Add(new NpgsqlParameter<int[]>("ss", airbases.Select(e =>
                    Convert.ToInt32(((MilStd2525d) e.Symbology).SymbolSet)).ToArray()));
                command.Parameters.Add(new NpgsqlParameter<int[]>("st", airbases.Select(e =>
                    Convert.ToInt32(((MilStd2525d) e.Symbology).Status)).ToArray()));
                command.Parameters.Add(new NpgsqlParameter<int[]>("sh", airbases.Select(e =>
                    Convert.ToInt32(((MilStd2525d) e.Symbology).HQTFDummy)).ToArray()));
                command.Parameters.Add(new NpgsqlParameter<int[]>("sa", airbases.Select(e =>
                    Convert.ToInt32(((MilStd2525d) e.Symbology).Amplifier)).ToArray()));
                command.Parameters.Add(new NpgsqlParameter<int[]>("se", airbases.Select(e =>
                    Convert.ToInt32(((MilStd2525d) e.Symbology).Entity)).ToArray()));
                command.Parameters.Add(new NpgsqlParameter<int[]>("sf", airbases.Select(e =>
                    Convert.ToInt32(((MilStd2525d) e.Symbology).EntityType)).ToArray()));
                command.Parameters.Add(new NpgsqlParameter<int[]>("sg", airbases.Select(e =>
                    Convert.ToInt32(((MilStd2525d) e.Symbology).EntitySubType)).ToArray()));
                command.Parameters.Add(new NpgsqlParameter<int[]>("so", airbases.Select(e =>
                    Convert.ToInt32(((MilStd2525d) e.Symbology).SectorOneModifier)).ToArray()));
                command.Parameters.Add(new NpgsqlParameter<int[]>("sp", airbases.Select(e =>
                    Convert.ToInt32(((MilStd2525d) e.Symbology).SectorTwoModifier)).ToArray()));
                await command.ExecuteNonQueryAsync(scribeToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Database Exception");
            }
        }

        public async Task WriteMarkPanelsAsync(List<MarkPanel> markPanels, CancellationToken scribeToken)
        {
            try
            {
                await using var conn = new NpgsqlConnection(GetConnectionString());
                await conn.OpenAsync(scribeToken);
                conn.TypeMapper.UseNetTopologySuite(geographyAsDefault: true);

                await using var command = new NpgsqlCommand(
                @"INSERT INTO markpanels (""id"", time, ""position"", ""text"", coalition, groupid, initiator, updated_at)
                        SELECT * FROM unnest(@i, @t, @p, @s, @co, @gr, @in, @u)", conn);

                /*
                 * The following fields come directly from DCS
                 */
                command.Parameters.Add(new NpgsqlParameter<int[]>("i", markPanels.Select(e =>
                    (int) e.Id).ToArray()));
                command.Parameters.Add(new NpgsqlParameter<double[]>("t", markPanels.Select(e =>
                    e.Time).ToArray()));
                command.Parameters.Add(new NpgsqlParameter<Point[]>("p", markPanels.Select(e =>
                    new Point(new Coordinate(e.Position.Longitude, e.Position.Latitude))).ToArray()));
                command.Parameters.Add(new NpgsqlParameter<string[]>("s", markPanels.Select(e =>
                    e.Text).ToArray()));
                command.Parameters.Add(new NpgsqlParameter<int[]>("co", markPanels.Select(e =>
                    e.Coalition).ToArray()));
                command.Parameters.Add(new NpgsqlParameter<int[]>("gr", markPanels.Select(e =>
                    e.GroupId).ToArray()));
                command.Parameters.Add(new NpgsqlParameter<string[]>("in", markPanels.Select(e =>
                    e.Initiator).ToArray()));
                command.Parameters.Add(new NpgsqlParameter<DateTime[]>("u", markPanels.Select(e =>
                    DateTime.UtcNow).ToArray()));

                await command.ExecuteNonQueryAsync(scribeToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Database Exception");
            }
        }

        public async Task UpdateMarkPanelsAsync(List<MarkPanel> markpanels, CancellationToken scribeToken)
        {
            try
            {
                await using var conn = new NpgsqlConnection(GetConnectionString());
                await conn.OpenAsync(scribeToken);
                conn.TypeMapper.UseNetTopologySuite(geographyAsDefault: true);

                await using var command = new NpgsqlCommand(
                @"INSERT INTO markpanels (""id"", ""position"", ""text"", coalition, groupid, initiator, updated_at)
                        SELECT * FROM unnest(@i, @p, @t, @co, @gr, @in, @u)
                        ON CONFLICT ON CONSTRAINT markpanels_pkey
                        DO UPDATE SET ""position"" = EXCLUDED.position, ""text""=EXCLUDED.text, coalition=EXCLUDED.coalition, groupid=EXCLUDED.groupid, initiator=EXCLUDED.initiator, updated_at = EXCLUDED.updated_at", conn);

                /*
                 * The following fields come directly from DCS
                 */
                command.Parameters.Add(new NpgsqlParameter<int[]>("i", markpanels.Select(e =>
                    (int) e.Id).ToArray()));
                command.Parameters.Add(new NpgsqlParameter<Point[]>("p", markpanels.Select(e =>
                    new Point(new Coordinate(e.Position.Longitude, e.Position.Latitude))).ToArray()));
                command.Parameters.Add(new NpgsqlParameter<string[]>("t", markpanels.Select(e =>
                    e.Text).ToArray()));
                command.Parameters.Add(new NpgsqlParameter<int[]>("co", markpanels.Select(e =>
                    e.Coalition).ToArray()));
                command.Parameters.Add(new NpgsqlParameter<int[]>("gr", markpanels.Select(e =>
                    e.GroupId).ToArray()));
                command.Parameters.Add(new NpgsqlParameter<string[]>("in", markpanels.Select(e =>
                    e.Initiator).ToArray()));
                command.Parameters.Add(new NpgsqlParameter<DateTime[]>("u", markpanels.Select(e =>
                    DateTime.UtcNow).ToArray()));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Database Exception");
            }
        }

        public async Task DeleteMarkPanelsAsync(List<uint> markpanels, CancellationToken scribeToken)
        {
            try
            {
                await using var conn = new NpgsqlConnection(GetConnectionString());
                await conn.OpenAsync(scribeToken);

                await using var command = new NpgsqlCommand(
                    @"DELETE FROM markpanels WHERE id IN (SELECT * FROM unnest(@a))", conn);

                command.Parameters.Add(new NpgsqlParameter<int[]>("a", markpanels.Select(u =>
                    (int) u).ToArray()));

                await command.ExecuteNonQueryAsync(scribeToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Database Exception");
            }
        }

        private string GetConnectionString()
        {
            return new NpgsqlConnectionStringBuilder
            {
                Host = Host,
                Port = Port,
                Database = Name,
                Username = Username,
                Password = Password
            }.ToString();
        }
    }
}