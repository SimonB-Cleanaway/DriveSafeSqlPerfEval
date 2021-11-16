using System;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using DriveSafe.SqlPerfTest;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

[assembly: FunctionsStartup(typeof(DriveSafe.SqlTPerfApi.Startup))]

namespace DriveSafe.SqlTPerfApi
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            var config = new ConfigurationBuilder()
                .AddJsonFile("host.json", optional: true)
                .Build();

            builder.Services
                .AddSingleton<IConfiguration>(config)
                .RegisterService();


        }
    }
}
