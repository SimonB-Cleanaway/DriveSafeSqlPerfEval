using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace ConsoleApp3
{
    public class LoadTest1Cmd : ICmd
    {
        private readonly string _conStr;

        public LoadTest1Cmd(IConfiguration config)
        {
            _conStr = config["ConnectionString"];
        }

        private readonly Random _rnd = new ();

        public record Vehicle(int VehicleId, string VehicleNo, int BusinessUnitId);

        public record BusinessUnit(int BusUnitId, string Code, string Name);

        public record LocationUpdate(string VehicleNo, string BusUnitCode, DateTimeOffset Timestamp, float Lat, float Lng, int Speed, int Dir);

        public async Task Run(IReadOnlyList<string> args)
        {
            var vehicleCount = args.Count > 0 && int.TryParse(args[0], out var c) ? c : 2000;
            var simCount = args.Count > 1 && int.TryParse(args[1], out var s) ? s : 100000;
            var threadCount = args.Count > 2 && int.TryParse(args[2], out var t) ? t : 20;

            IReadOnlyList<BusinessUnit> bus;
            using (new SectionTimer("Loading Business Units"))
            {
                bus = await RecUtils.QueryRecords(
                        _conStr,
                        "select BusinessUnitId, Code, Name from BusinessUnit",
                        r => new BusinessUnit(r.GetInt32(0), r.GetString(1), r.GetString(2)))
                    .ToListAsync();
            }

            // Get or create random vehicles
            IReadOnlyList<Vehicle> vehicles;
            using (new SectionTimer("Loading Vehicles"))
            {
                vehicles = await RecUtils
                    .QueryRecords(
                        _conStr,
                        "select VehicleId, VehicleNo, BusinessUnitId from Vehicle",
                        r => new Vehicle(r.GetInt32(0), r.GetString(1), r.GetInt32(2)))
                    .ToListAsync();
            }

            if (vehicles.Count < vehicleCount)
            {
                using (new SectionTimer("Creating New Vehicles"))
                    vehicles = await CreateTestVehicles(bus, vehicles, vehicleCount);
            }

            SectionTimer simTimer;
            using (simTimer = new SectionTimer($"Simulating {simCount} updates using {threadCount} threads"))
            {
                var ctr = 0;
                await ForEachAsync(SimulateLocationUpdates(vehicles, bus).Take(simCount), threadCount, async vu =>
                {
                    var ii = Interlocked.Increment(ref ctr);
                    if (ii % 5000 == 0)
                        Debug.WriteLine($"Processed {ii} ({(100 * ii/ simCount):F0}%) ...");

                    await UpdateVehicleLocation(vu);
                });
            }
            Debug.WriteLine($"Took {simTimer.ElapsedMilliseconds} ms for {simCount} updates or {1.0 * simTimer.ElapsedMilliseconds / simCount} ms per update");
        }

        private Task ForEachAsync<T>(IEnumerable<T> source, int dop, Func<T, Task> body)
        {
            return Task.WhenAll(
                from partition in Partitioner.Create(source).GetPartitions(dop)
                select Task.Run(async delegate 
                {
                    using (partition)
                        while (partition.MoveNext())
                            await body(partition.Current);
                }));
        }

        private async Task<IReadOnlyList<Vehicle>> CreateTestVehicles(IReadOnlyList<BusinessUnit> bus, IReadOnlyList<Vehicle> curVehicles, int requiredVehicleCount)
        {
            var rnd = new Random();

            var vehicles = curVehicles.ToDictionary(x => x.VehicleNo, StringComparer.OrdinalIgnoreCase);

            while (vehicles.Count < requiredVehicleCount)
            {
                var v = new Vehicle(0, RandomRego(rnd), bus[rnd.Next(bus.Count)].BusUnitId);

                if (!vehicles.ContainsKey(v.VehicleNo))
                    vehicles.Add(v.VehicleNo, v);
            }

            await RecUtils.AddRecords(_conStr, x => x.VehicleId, vehicles.Values.Where(x => x.VehicleId == 0).ToList());

            return vehicles.Values.ToList();
        }

        public static string RandomRego(Random rnd) => new (new[]
        {
            (char)('A' + rnd.Next(26)),
            (char)('A' + rnd.Next(26)),
            (char)('A' + rnd.Next(26)),
            (char)('0' + rnd.Next(10)),
            (char)('0' + rnd.Next(10)),
            (char)('0' + rnd.Next(10)),
        });

        private IEnumerable<LocationUpdate> SimulateLocationUpdates(IReadOnlyList<Vehicle> vehicles, IReadOnlyList<BusinessUnit> bus)
        {

            while (true)
            {
                var v = vehicles[_rnd.Next(vehicles.Count)];
                var bu = bus.FirstOrDefault(x => x.BusUnitId == v.BusinessUnitId)?.Code;

                yield return SimulateLocationUpdate(v, bu);
            }
        }

        private LocationUpdate SimulateLocationUpdate(Vehicle v, string bu)
        {
            const float maxLat = -11.45397008875731f;
            const float maxLng = 153.58748984080026f;
            const float minLat = -43.56502683925548f;
            const float minLng = 113.31736337056127f;

            return new LocationUpdate(
                v.VehicleNo,
                bu,
                DateTimeOffset.Now.AddSeconds(_rnd.Next(60) - 120),
                (float)(minLat + (_rnd.NextDouble() * (maxLat - minLat))),
                (float)(minLng + (_rnd.NextDouble() * (maxLng - minLng))),
                _rnd.Next(120),
                _rnd.Next(360));
        }

        public async Task UpdateVehicleLocation(LocationUpdate vlu)
        {
            await using var conn = new SqlConnection(_conStr);
            await conn.OpenAsync();

            await using (var cmdUpdate = conn.CreateCommand())
            {
                cmdUpdate.CommandText =
                    "update vl " +
                    "set LastUpdated=@timestamp, Latitude=@latitude, Longitude=@longitude, Direction=@direction, Speed=@speed " +
                    "from VehicleLocation vl inner " +
                    "join Vehicle v on v.VehicleId = vl.VehicleId " +
                    "where VehicleNo = @VehicleNo and LastUpdated < @timestamp; " +
                    "if (@@ROWCOUNT = 0)  " +
                    "insert into VehicleLocation(VehicleId, LastUpdated, Latitude, Longitude, Direction, Speed) " +
                    "select v.VehicleId, @timestamp, @latitude, @longitude, @direction, @speed " +
                    "from Vehicle v left outer join VehicleLocation vl on vl.VehicleId = v.VehicleId " +
                    "where v.VehicleNo = @VehicleNo and vl.VehicleId is null";

                cmdUpdate.Parameters.AddWithValue("@VehicleNo", vlu.VehicleNo);
                cmdUpdate.Parameters.AddWithValue("@timestamp", vlu.Timestamp);
                cmdUpdate.Parameters.AddWithValue("@latitude", vlu.Lat);
                cmdUpdate.Parameters.AddWithValue("@longitude", vlu.Lng);
                cmdUpdate.Parameters.AddWithValue("@direction", vlu.Dir);
                cmdUpdate.Parameters.AddWithValue("@speed", vlu.Speed);

                await cmdUpdate.ExecuteNonQueryAsync();
            }

            await conn.CloseAsync();
        }
    }

    public static class TaskUtils
    {
        // https://devblogs.microsoft.com/pfxteam/implementing-a-simple-foreachasync-part-2/
        public static Task ForEachAsync<T>(this IEnumerable<T> source, int dop, Func<T, Task> body)
        {
            return Task.WhenAll(
                from partition in Partitioner.Create(source).GetPartitions(dop)
                select Task.Run(async delegate {
                    using (partition)
                        while (partition.MoveNext())
                            await body(partition.Current);
                }));
        }
    }
}
