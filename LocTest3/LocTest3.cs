using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp3.LocTest3
{
    public class LocTest3Cmd : ICmd
    {
        private readonly string _conStr;

        public LocTest3Cmd(IConfiguration config)
        {
            _conStr = config["ConnectionString"] ?? throw new ArgumentNullException("No connection string defined");
        }

        record VehicleLoc(int VehicleId, string VehicleNo, DateTimeOffset Timestamp, double Latitude, double Longitude, short Speed, short Direction);

        public async Task Run(IReadOnlyList<string> args)
        {
            double latitude = -37.8396f;
            double longitude = 144.9772f;
            int distance = 10000;
            DateTimeOffset from = DateTimeOffset.Now.AddHours(-12);
            DateTimeOffset to = DateTimeOffset.Now.AddHours(1);

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
