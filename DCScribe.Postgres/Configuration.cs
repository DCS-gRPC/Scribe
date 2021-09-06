using Npgsql;

namespace RurouniJones.DCScribe.Postgres
{
    public class Configuration
    {
        public static void Configure()
        {
            NpgsqlConnection.GlobalTypeMapper.UseNetTopologySuite(geographyAsDefault: true);
        }
    }
}
