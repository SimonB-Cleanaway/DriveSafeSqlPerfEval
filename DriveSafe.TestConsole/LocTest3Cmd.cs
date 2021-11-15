using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using DriveSafe.SqlPerfTest;

namespace DriveSafe.TestConsole
{
    public class LocTest3Cmd : LocTest3, ICmd
    {
        public LocTest3Cmd(IConfiguration config)
            : base(config)
        {
        }

        public Task Run(IReadOnlyList<string> args)
        {
            double latitude = -37.8396f;
            double longitude = 144.9772f;
            int distance = 10000;
            DateTimeOffset from = DateTimeOffset.Now.AddHours(-12);
            DateTimeOffset to = DateTimeOffset.Now.AddHours(1);

            return Run(latitude, longitude, distance, from, to);
        }
    }
}
