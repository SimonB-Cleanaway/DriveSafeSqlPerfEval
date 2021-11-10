using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace ConsoleApp3
{
    public class Test1Cmd : ICmd
    {
        public const string ConnStr = @"Server=localhost;Database=DriveSafe;Trusted_Connection=True;";

        public record Vehicle(
            int VehicleId,
            string VehicleNo,
            string BusinessUnitCode,
            string BusinessUnitName,
            DateTimeOffset? LastUpdate,
            double? Latitude,
            double? Longitude,
            int? Speed,
            int? Direction);

        public async Task Run(IReadOnlyList<string> args)
        {
            var sw = Stopwatch.StartNew();
            await foreach (var v in QueryVehicles())
                Debug.WriteLine(v);

            sw.Stop();
            Debug.WriteLine($"Took {sw.ElapsedMilliseconds} ms");
        }

        public IAsyncEnumerable<Vehicle> QueryVehicles() => RecUtils.QueryRecords(
            ConnStr,
            @"select v.VehicleId, v.VehicleNo, bu.Code as BusinessUnitCode, bu.Name as BusinessUnitName, l.LastUpdated, l.Latitude, l.Longitude, l.Speed, l.Direction " +
            @"from Vehicle v inner join BusinessUnit bu on bu.BusinessUnitId = v.BusinessUnitId left outer join VehicleLocation l on l.VehicleId = v.VehicleId",
            rdr =>
            {
                var idx = 0;
                return new Vehicle(
                    rdr.GetInt32(idx++),
                    rdr.GetString(idx++),
                    rdr.GetString(idx++),
                    rdr.GetString(idx++),
                    rdr.GetNullableDateTimeOffset(ref idx),
                    rdr.GetNullableDouble(ref idx),
                    rdr.GetNullableDouble(ref idx),
                    rdr.GetNullableShort(ref idx),
                    rdr.GetNullableShort(ref idx));
            });
    }
}
