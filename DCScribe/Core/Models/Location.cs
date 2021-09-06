using System;

namespace RurouniJones.DCScribe.Core.Models
{
    public class Location
    {
        public double Latitude { get; }
        public double Longitude { get; }
        public double Altitude { get; }

        public Location(double latitude, double longitude, double altitude)
        {
            Latitude = Math.Min(Math.Max(latitude, -90.0), 90.0);
            Longitude = longitude;
            Altitude = altitude;
        }
    }
}
