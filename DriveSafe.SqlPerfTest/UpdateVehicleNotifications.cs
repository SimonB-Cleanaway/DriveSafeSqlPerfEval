using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace DriveSafe.SqlPerfTest
{
    public partial class UpdateVehicleNotifications
    {
        private readonly Random _rnd;
        private readonly ValidationRuleSim[] _validationRules;
        private readonly DataSimulator _dataSimulator;
        private readonly string _conStr;

        public UpdateVehicleNotifications(IConfiguration config)
        {
            // _conStr = config["ConnectionString"] ?? throw new ArgumentNullException(nameof(config), "No Connection String Defined");
            _conStr = "Server=localhost;Database=DriveSafe;Trusted_Connection=True;";

            _rnd = new Random();

            _validationRules = new[]
            {
                new ValidationRuleSim("A1", "Vehicle Status",       NotLevel.Alert, _rnd, 0.12),
                new ValidationRuleSim("A3", "Invalid Registration", NotLevel.Alert, _rnd, 0.08),
                new ValidationRuleSim("N1", "No Fleet Number",      NotLevel.Info, _rnd, 0.20),
            };

            _dataSimulator = new DataSimulator(_rnd);
        }

        private IReadOnlyDictionary<string, Vehicle>? _vehicles;
        private IReadOnlyDictionary<string, NotificationLevel>? _levels;

        public async Task Run(int simCount, int threadCount)
        {
            _levels = await LoadLevels();
            var bus = await LoadBUs();
            _vehicles = await LoadVehicles(bus);
         
            await LoadValidationRuleIds();

            SectionTimer simTimer;
            using (simTimer = new SectionTimer($"Simulating {simCount} updates using {threadCount} threads"))
            {
                var ctr = 0;
                var batch = 0;

                foreach (var vls in _dataSimulator
                    .SimulateLocationUpdates(_vehicles.Values.ToList())
                    .Take(simCount)
                    .Chunk(100))
                {
                    Interlocked.Increment(ref batch);
                    var ii = Interlocked.Add(ref ctr, vls.Length);
                    if (ii % 100 == 0)
                        Debug.WriteLine($"Processed {ii} ({(100 * ii / simCount):F0}%) ...");

                    // Generate Notifications
                    var notificationTasks = vls
                        .GroupBy(x => x.Registration)
                        .Select(x => x.OrderByDescending(r => r.Timestamp).First())
                        .SelectMany(vl => _validationRules.Select(vr => vr.Validate(vl)))
                        .ToArray();

                    await Task.WhenAll(notificationTasks);

                    var notifications = notificationTasks.Select(x => x.Result).Where(x => x != null).ToList();

                    Debug.WriteLine($"Batch {batch} has {notifications.Count} notifications.");

                    await Update1(notifications);
                }
            }
        }

        private async Task<IReadOnlyDictionary<string, Vehicle>> LoadVehicles(IReadOnlyList<BusinessUnit> bus)
        {
            using (new SectionTimer("Loading Vehicles")) 

            return await RecUtils
                .QueryRecords(
                    _conStr,
                    "select VehicleId, VehicleNo, BusinessUnitId from Vehicle",
                    r =>
                    {
                        var bu = bus.First(x => x.BusUnitId == r.GetInt32(2));
                        return new Vehicle(r.GetInt32(0), r.GetString(1)) { BusinessUnit = bu };
                    })
                .ToDictionaryAsync(v => v.VehicleNo, StringComparer.OrdinalIgnoreCase);
        }

        private async Task<IReadOnlyDictionary<string, NotificationLevel>> LoadLevels()
        {
            using (new SectionTimer("Loading Notification Levels")) 

            return await RecUtils
                .QueryRecords(
                    _conStr,
                    "select NotificationLevelId, Code, Name from NotificationLevel",
                    r => new NotificationLevel(r.GetInt32(0), r.GetString(1), r.GetString(2)))
                .ToDictionaryAsync(v => v.Code, StringComparer.OrdinalIgnoreCase);
        }

        private async Task<IReadOnlyList<BusinessUnit>> LoadBUs()
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

            return bus;
        }

        private async Task LoadValidationRuleIds()
        {
            using (new SectionTimer("Loading Validation Rules"))
            {
                await foreach(var (Id, Code) in RecUtils.QueryRecords(
                        _conStr,
                        "select ValidationRuleId, Code  from ValidationRule",
                        r => (Id: r.GetInt32(0), Code: r.GetString(1))))
                {
                    var r = _validationRules.First(x => string.Equals(x.Code, Code, StringComparison.OrdinalIgnoreCase));
                    r.Id = Id;
                }
            }
        }

        private async Task Update1(IEnumerable<IVehicleValidationResult> notifications)
        {
            await using var conn = new SqlConnection(_conStr);
            await conn.OpenAsync();

            foreach (var vn in notifications.GroupBy(x => x.VehicleId, StringComparer.OrdinalIgnoreCase))
            {
                var curNots = await RecUtils.QueryRecords(conn,
                    "select vn.VehicleNotificationId, vn.VehicleId, vn.ValidationRuleId, vn.NotificationLevelId, vn.Message, vn.CreateDate, vn.ExpiryDate, vn.Active " +
                    "from VehicleNotification vn" +
                    " inner join Vehicle v on vn.VehicleId = v.VehicleId " +
                    "where v.VehicleNo = @VehicleNo",
                    r => new VehicleNotification(r.GetInt32(0), r.GetInt32(1), r.GetInt32(2), r.GetInt32(3), r.GetString(4), r.GetDateTimeOffset(5), r.GetNullableDateTimeOffset(6), r.GetBoolean(7))
                    {
                        ValidationRule = _validationRules.First(x => x.Id == r.GetInt32(2))
                    },
                    x => x.Parameters.AddWithValue("@VehicleNo", vn.Key))
                    .ToListAsync();

                var newNots = notifications.Where(x => !curNots.Any(n => n.ValidationRule.Code == x.RuleCode)).ToList();
                var existingNots = notifications.Except(newNots);

                foreach (var nvn in newNots)
                {
                    var vehicle = _vehicles[nvn.VehicleId];
                    var vr = _validationRules.First(x => x.Code == nvn.RuleCode);
                    var nl = _levels[nvn.Level.ToString()];

                    await using var cmd = conn.CreateCommand();

                    cmd.CommandText = 
                        "insert into VehicleNotification(VehicleId, ValidationRuleId, NotificationLevelId, Message, CreateDate, ExpiryDate, Active) " +
                        "values(@VehicleId, @ValidationRuleId, @NotificationLevelId, @Message, @CreateDate, null, 1)";
                    cmd.Parameters.AddWithValue("@VehicleId", vehicle.VehicleId);
                    cmd.Parameters.AddWithValue("@ValidationRuleId", vr.Id);
                    cmd.Parameters.AddWithValue("@NotificationLevelId", nl.LevelId);
                    cmd.Parameters.AddWithValue("@Message", nvn.Message);
                    cmd.Parameters.AddWithValue("@CreateDate", DateTimeOffset.Now);

                    await cmd.ExecuteNonQueryAsync();
                }

                foreach(var evn in existingNots
                    .Join(curNots, x => x.RuleCode, x => x.ValidationRule.Code, (n, r) => (n, r))
                    .Where(x => x.r.Active && !x.r.ExpiryDate.HasValue || x.r.ExpiryDate < DateTimeOffset.Now))
                {
                    await using var cmd = conn.CreateCommand();

                    cmd.CommandText =
                        "update VehicleNotification set ExpiryDate=@ExpiryDate where VehicleNotificationId = @VehicleNotificationId";
                    cmd.Parameters.AddWithValue("@VehicleNotificationId", evn.r.Id);
                    cmd.Parameters.AddWithValue("@ExpiryDate", DateTimeOffset.Now);

                    await cmd.ExecuteNonQueryAsync();
                }
            }

            await conn.CloseAsync();
        }
    }
}
