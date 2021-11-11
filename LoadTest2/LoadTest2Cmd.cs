using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace ConsoleApp3
{
    public partial class LoadTest2Cmd : ICmd
    {
        private readonly Random _rnd;
        private readonly ValidationRuleSim[] _validationRules;
        private readonly DataSimulator _dataSimulator;
        private readonly string _conStr;

        public LoadTest2Cmd(IConfiguration config)
        {
            _conStr = config["ConnectionString"];

            _rnd = new Random();

            _validationRules = new[]
            {
                new ValidationRuleSim("A1", "Vehicle Status", NotLevel.Alert, _rnd, 0.12),
                new ValidationRuleSim("A3", "Invalid Registration", NotLevel.Alert, _rnd, 0.08),
                new ValidationRuleSim("N1", "No Fleet Number", NotLevel.Info, _rnd, 0.20),
            };

            _dataSimulator = new DataSimulator(_rnd);
        }

        private IReadOnlyDictionary<string, Vehicle> _vehicles;
        private IReadOnlyDictionary<string, NotificationLevel> _levels;

        public async Task Run(IReadOnlyList<string> args)
        {
            var vehicleCount = args.Count > 0 && int.TryParse(args[0], out var c) ? c : 2000;
            var simCount = args.Count > 1 && int.TryParse(args[1], out var s) ? s : 100000;
            var threadCount = args.Count > 2 && int.TryParse(args[2], out var t) ? t : 20;

            var bus = await LoadBUs();
            _vehicles = await LoadVehicles();
            _levels = await LoadLevels();  
            await LoadValidationRuleIds();

            foreach (var vls in _dataSimulator.SimulateLocationUpdates(_vehicles.Values.ToList(), bus)
                .Take(simCount)
                .Chunk(100))
            {
                // Generate Notifications
                var notificationTasks = vls
                    .GroupBy(x => x.Registration)
                    .Select(x => x.OrderByDescending(r => r.Timestamp).First())
                    .SelectMany(vl => _validationRules.Select(vr => vr.Validate(vl)))
                    .ToArray();

                await Task.WhenAll(notificationTasks);

                var notifications = notificationTasks.Select(x => x.Result).Where(x => x != null);

                await Update(notifications);

            }
        }

        private async Task<IReadOnlyDictionary<string, Vehicle>> LoadVehicles()
        {
            using (new SectionTimer("Loading Vehicles")) 

            return await RecUtils
                .QueryRecords(
                    _conStr,
                    "select VehicleId, VehicleNo, BusinessUnitId from Vehicle",
                    r => new Vehicle(r.GetInt32(0), r.GetString(1), r.GetInt32(2)))
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
                await foreach(var vr in RecUtils.QueryRecords(
                        _conStr,
                        "select ValidationRuleId, Code  from ValidationRule",
                        r => (Id: r.GetInt32(0), Code: r.GetString(1))))
                {
                    var r = _validationRules.First(x => string.Equals(x.Code, vr.Code, StringComparison.OrdinalIgnoreCase));
                    r.Id = vr.Id;
                }
            }
        }

        private async Task Update(IEnumerable<IVehicleValidationResult> notifications)
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
                        ValidationRule = _validationRules.FirstOrDefault(x => x.Id == r.GetInt32(2))
                    },
                    x => x.Parameters.AddWithValue("@VehicleNo", vn.Key))
                    .ToListAsync();

                foreach(var newNot in notifications.Where(x => !curNots.Any(n => n.ValidationRule.Code == x.RuleCode)))
                {
                    var vehicle = _vehicles[newNot.VehicleId];
                    var vr = _validationRules.First(x => x.Code == newNot.RuleCode);
                    var nl = _levels[newNot.Level.ToString()];

                    await using var cmd = conn.CreateCommand();

                    cmd.CommandText = 
                        "insert into VehicleNotification(VehicleId, ValidationRuleId, NotificationLevelId, Message, CreateDate, ExpiryDate, Active) " +
                        "values(@VehicleId, @ValidationRuleId, @NotificationLevelId, @Message, @CreateDate, null, 1)";
                    cmd.Parameters.AddWithValue("@VehicleId", vehicle.VehicleId);
                    cmd.Parameters.AddWithValue("@ValidationRuleId", vr.Id);
                    cmd.Parameters.AddWithValue("@NotificationLevelId", nl.LevelId);
                    cmd.Parameters.AddWithValue("@Message", newNot.Message);
                    cmd.Parameters.AddWithValue("@CreateDate", DateTimeOffset.Now);

                    await cmd.ExecuteNonQueryAsync();
                }
            }
            await conn.CloseAsync();
        }
    }
}
