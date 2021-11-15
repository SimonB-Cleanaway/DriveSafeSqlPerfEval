using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp3.LocTest3
{
    public class LocTest3Cmd : ICmd
    {
        private readonly string _conStr;

        public LocTest3Cmd(IConfiguration config)
        {
            _conStr = config["ConnectionString"] ?? throw new ArgumentNullException("No connection string defined");
        }

        public async Task Run(IReadOnlyList<string> args)
        {
            using (new SectionTimer($"Loading Locations"))
            {

            }
        }
    }
}
