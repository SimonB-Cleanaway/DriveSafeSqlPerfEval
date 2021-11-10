using System;
using System.Collections.Generic;
using System.Linq;

namespace ConsoleApp3
{
    public class DataSimulator
    {
        private readonly Random _rnd = new();

        public DataSimulator(Random rnd = null)
        {
            _rnd = rnd ?? new Random();
        }

        public string RandomRego() => new(new[]
         {
            (char)('A' + _rnd.Next(26)),
            (char)('A' + _rnd.Next(26)),
            (char)('A' + _rnd.Next(26)),
            (char)('0' + _rnd.Next(10)),
            (char)('0' + _rnd.Next(10)),
            (char)('0' + _rnd.Next(10)),
        });

        public IEnumerable<ValidationRequest> SimulateLocationUpdates(IReadOnlyList<Vehicle> vehicles, IReadOnlyList<BusinessUnit> bus)
        {
            while (true)
            {
                var v = vehicles[_rnd.Next(vehicles.Count)];
                var bu = bus.FirstOrDefault(x => x.BusUnitId == v.BusinessUnitId)?.Code;

                yield return SimulateLocationUpdate(v, bu);
            }
        }

        private readonly (double Prob, string Status)[] _vehicleStatus = new[] { (0.8, "AX"), (0.2, "AV") };
        private string VehicleStatus()
        {
            var idx = 0;
            var p = _rnd.NextDouble();
            var ps = 0.0;
 
            while(ps < p)
                ps += _vehicleStatus[idx++].Prob;

            return _vehicleStatus[idx - 1].Status;
        }


        public ValidationRequest SimulateLocationUpdate(Vehicle v, string bu)
        {
            const float maxLat = -11.45397008875731f;
            const float maxLng = 153.58748984080026f;
            const float minLat = -43.56502683925548f;
            const float minLng = 113.31736337056127f;

            return new ValidationRequest(
                v.VehicleNo,
                bu,
                VehicleStatus(),
                DateTimeOffset.Now.AddSeconds(_rnd.Next(60) - 120),
                null,
                null,
                (float)(minLat + (_rnd.NextDouble() * (maxLat - minLat))),
                (float)(minLng + (_rnd.NextDouble() * (maxLng - minLng))),
                _rnd.Next(120),
                _rnd.Next(360));
        }
    }
}
