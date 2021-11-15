using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using DriveSafe.SqlPerfTest;

namespace DriveSafe.TestConsole
{
    public class LoadTest1Cmd : LoadTest1, ICmd
    {
        public LoadTest1Cmd(IConfiguration config)
            : base(config)
        {
        }

        public Task Run(IReadOnlyList<string> args)
        {
            var vehicleCount = args.Count > 0 && int.TryParse(args[0], out var c) ? c : 2000;
            var simCount = args.Count > 1 && int.TryParse(args[1], out var s) ? s : 250000;
            var threadCount = args.Count > 2 && int.TryParse(args[2], out var t) ? t : 20;

            return Run(vehicleCount, simCount, threadCount);
        }
    }
}
