using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NetTopologySuite.Geometries;
using Npgsql;
using RurouniJones.DCScribe.Shared.Interfaces;

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
            await using var conn = new NpgsqlConnection(GetConnectionString());
            await conn.OpenAsync();

            await using var cmd = new NpgsqlCommand("TRUNCATE TABLE units", conn);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task UpdateUnitsAsync(List<Shared.Models.Unit> units, CancellationToken scribeToken)
        {
            try
            {
                await using var conn = new NpgsqlConnection(GetConnectionString());
                await conn.OpenAsync(scribeToken);
                conn.TypeMapper.UseNetTopologySuite(geographyAsDefault: true);

                await using var command = new NpgsqlCommand(
                @"INSERT INTO units (id, ""position"", altitude, type, name, callsign, player, group_name,
                        coalition, heading, speed, updated_at)
                        SELECT * FROM unnest(@i, @l, @a, @t, @n, @pi, @pl, @g, @c, @h, @s, @u)
                        ON CONFLICT ON CONSTRAINT units_pkey
                        DO UPDATE SET ""position"" = EXCLUDED.position, altitude = EXCLUDED.altitude, 
                        heading = EXCLUDED.heading, speed = EXCLUDED.speed, updated_at = EXCLUDED.updated_at", conn);

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