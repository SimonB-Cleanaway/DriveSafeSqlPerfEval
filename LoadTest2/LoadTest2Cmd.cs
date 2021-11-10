using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace ConsoleApp3
{
    public partial class LoadTest2Cmd : ICmd
    {
        private readonly Random _rnd;
        private readonly IValidationRule[] _validationRules;
        private readonly DataSimulator _dataSimulator;
        private readonly string _conStr;

        public LoadTest2Cmd(IConfiguration config)
        {
            _conStr = config["ConnectionString"];

            _rnd = new Random();

            _validationRules = new[]
            {
                new ValidationRuleSim("A1", "Vehicle Status", NotificationLevel.Alert, _rnd, 0.12),
                new ValidationRuleSim("A3", "Invalid Registration", NotificationLevel.Alert, _rnd, 0.08),
                new ValidationRuleSim("N1", "No Fleet Number", NotificationLevel.Info, _rnd, 0.20),
            };

            _dataSimulator = new DataSimulator(_rnd);
        }

        public async Task Run(IReadOnlyList<string> args)
        {
            var vehicleCount = args.Count > 0 && int.TryParse(args[0], out var c) ? c : 2000;
            var simCount = args.Count > 1 && int.TryParse(args[1], out var s) ? s : 100000;
            var threadCount = args.Count > 2 && int.TryParse(args[2], out var t) ? t : 20;

            var bus = await LoadBUs();
            var vehicles = await LoadVehicles();

            foreach (var vl in _dataSimulator.SimulateLocationUpdates(vehicles, bus).Take(10))
            {

            }
        }

        private async Task<IReadOnlyList<Vehicle>> LoadVehicles()
        {
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

            return vehicles;
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
    }
}
