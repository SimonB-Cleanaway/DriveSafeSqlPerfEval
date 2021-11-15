using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using DriveSafe.SqlPerfTest;

namespace DriveSafe.TestConsole
{
    public partial class LoadTest2Cmd : LoadTest2, ICmd
    {
        public LoadTest2Cmd(IConfiguration config)
            : base(config)
        {
        }

        public Task Run(IReadOnlyList<string> args)
        {
            var simCount = args.Count > 1 && int.TryParse(args[1], out var s) ? s : 100000;
            var threadCount = args.Count > 2 && int.TryParse(args[2], out var t) ? t : 20;

            return Run(simCount, threadCount);
        }
    }
}
