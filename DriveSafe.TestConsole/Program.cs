using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace DriveSafe.TestConsole
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            if (args.Length < 1)
                return;

            var cmds = typeof(Program).Assembly
                .GetTypes()
                .Where(x => x.IsClass && !x.IsAbstract && typeof(ICmd).IsAssignableFrom(x))
                .ToDictionary(x => x.Name.EndsWith("Cmd") ? x.Name[..^3] : x.Name, x => x, StringComparer.OrdinalIgnoreCase);

            var config = new ConfigurationBuilder()
                .AddJsonFile("config.json", optional: true)
                .Build();

            var sp = ConfigServices(cmds.Values, config);

            if (cmds.TryGetValue(args[0], out var cmdType))
            {
                var cmd = (ICmd)sp.GetService(cmdType);
                await cmd.Run(args.Skip(1).ToList());
            }
        }

        private static IServiceProvider ConfigServices(IEnumerable<Type> cmdTypes, IConfigurationRoot config)
        {
            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(config);
            foreach (var ct in cmdTypes)
                services.AddTransient(ct);
            var sp = services.BuildServiceProvider();
            return sp;
        }
    }
}
