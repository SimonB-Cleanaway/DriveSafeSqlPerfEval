using Microsoft.Extensions.Configuration;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace ConsoleApp3
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

            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(config);
            foreach (var ct in cmds.Values)
                services.AddTransient(ct);
            var sp = services.BuildServiceProvider();

            if (cmds.TryGetValue(args[0], out var cmdType))
            {
                var cmd = (ICmd)sp.GetService(cmdType);
                await cmd.Run(args.Skip(1).ToList());
            }
        }
    }
}
