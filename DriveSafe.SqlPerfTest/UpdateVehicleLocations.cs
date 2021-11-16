using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace DriveSafe.SqlPerfTest
{
    public class UpdateVehicleLocations
    {
        private readonly string _conStr;
        private readonly DataSimulator _dataSimulator = new();

        public UpdateVehicleLocations(IConfiguration config)
        {
            // _conStr = config["ConnectionString"] ?? throw new ArgumentNullException(nameof(config), "No Connection String Defined");
            _conStr = "Server=localhost;Database=DriveSafe;Trusted_Connection=True;";
        }

        public async Task Run(int vehicleCount, int simCount, int threadCount)
        {
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

                await ForEachAsync(
                    _dataSimulator.SimulateLocationUpdates(vehicles, bus).Take(simCount),
                    threadCount, 
                    async vu =>
                    {
                        var ii = Interlocked.Increment(ref ctr);
                        if (ii % 5000 == 0)
                            Debug.WriteLine($"Processed {ii} ({(100 * ii/ simCount):F0}%) ...");

                        await UpdateVehicleLocation(vu).ConfigureAwait(false);
                    });
            }
            Debug.WriteLine($"Took {simTimer.ElapsedMilliseconds} ms for {simCount} updates or {1.0 * simTimer.ElapsedMilliseconds / simCount} ms per update");
        }

        private static Task ForEachAsync<T>(IEnumerable<T> source, int dop, Func<T, Task> body)
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
                var v = new Vehicle(0, _dataSimulator.RandomRego(), bus[rnd.Next(bus.Count)].BusUnitId);

                if (!vehicles.ContainsKey(v.VehicleNo))
                    vehicles.Add(v.VehicleNo, v);
            }

            await RecUtils.AddRecords(_conStr, x => x.VehicleId, vehicles.Values.Where(x => x.VehicleId == 0).ToList());

            return vehicles.Values.ToList();
        }

        public async Task UpdateVehicleLocation(ValidationRequest vlu)
        {
            await using var conn = new SqlConnection(_conStr);
            await conn.OpenAsync();

            await using (var cmdCurLoc = conn.CreateCommand())
            {
                cmdCurLoc.CommandText =
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

                cmdCurLoc.Parameters.AddWithValue("@VehicleNo", vlu.Id);
                cmdCurLoc.Parameters.AddWithValue("@timestamp", vlu.Timestamp);
                cmdCurLoc.Parameters.AddWithValue("@latitude", vlu.Lat);
                cmdCurLoc.Parameters.AddWithValue("@longitude", vlu.Lng);
                cmdCurLoc.Parameters.AddWithValue("@direction", vlu.Dir);
                cmdCurLoc.Parameters.AddWithValue("@speed", vlu.Speed);

                await cmdCurLoc.ExecuteNonQueryAsync();
            }

            await using (var cmdTrace = conn.CreateCommand())
            {
                cmdTrace.CommandText =
                    "insert into VehicleTrace(VehicleId, Timestamp, Location, Direction, Speed) " +
                    "select v.VehicleId, @timestamp, geography::Point(@latitude, @longitude, 4326), @direction, @speed " +
                    "from Vehicle v " +
                    "where v.VehicleNo = @VehicleNo"; 
 
                cmdTrace.Parameters.AddWithValue("@VehicleNo", vlu.Id); 
                cmdTrace.Parameters.AddWithValue("@timestamp", vlu.Timestamp); 
                cmdTrace.Parameters.AddWithValue("@latitude", vlu.Lat); 
                cmdTrace.Parameters.AddWithValue("@longitude", vlu.Lng);
                cmdTrace.Parameters.AddWithValue("@direction", vlu.Dir);
                cmdTrace.Parameters.AddWithValue("@speed", vlu.Speed);

                await cmdTrace.ExecuteNonQueryAsync();
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
