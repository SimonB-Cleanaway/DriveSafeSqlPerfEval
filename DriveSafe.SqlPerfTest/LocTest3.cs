using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace DriveSafe.SqlPerfTest
{
    public class LocTest3
    {
        private readonly string _conStr;

        public LocTest3(IConfiguration config)
        {
            _conStr = config["ConnectionString"] ?? throw new ArgumentNullException("No connection string defined");
        }

        record VehicleLoc(int VehicleId, string VehicleNo, DateTimeOffset Timestamp, double Latitude, double Longitude, short Speed, short Direction);

        public async Task Run(
            double latitude,
            double longitude,
            int distance,
            DateTimeOffset from,
            DateTimeOffset to)
        {
            var sqlQry =
                "select v.VehicleId, v.VehicleNo, vt.Timestamp, vt.Location.Lat as Latitude, vt.Location.Long as Longitude, vt.Speed, vt.Direction " +
                "from VehicleTrace vt inner " +
                "join Vehicle v on v.VehicleId = vt.VehicleId " +
                "where geography::Point(@latitude, @longitude, 4326).STDistance(vt.Location) <= @distance and vt.Timestamp >= @from and vt.Timestamp <= @to";

            using (new SectionTimer($"Loading Locations"))
            {
                await foreach(var vl in RecUtils.QueryRecords(_conStr, sqlQry,
                    r => new VehicleLoc(r.GetInt32(0), r.GetString(1), r.GetDateTimeOffset(2), r.GetDouble(3), r.GetDouble(4), r.GetInt16(5), r.GetInt16(6)),
                    c =>
                    {
                        c.Parameters.AddWithValue("@latitude", latitude);
                        c.Parameters.AddWithValue("@longitude", longitude);
                        c.Parameters.AddWithValue("@distance", distance);
                        c.Parameters.AddWithValue("@from", from);
                        c.Parameters.AddWithValue("@to", to);
                    }))
                {
                    Debug.WriteLine(vl);
                }
            }
        }
    }
}
