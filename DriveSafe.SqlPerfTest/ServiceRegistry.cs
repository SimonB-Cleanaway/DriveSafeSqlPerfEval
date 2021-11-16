using System;
using Microsoft.Extensions.DependencyInjection;

namespace DriveSafe.SqlPerfTest
{
    public static class ServiceRegistry
    {
        public static IServiceCollection RegisterService(this IServiceCollection services)
        {
            return services
                .AddTransient<UpdateVehicleLocations>()
                .AddTransient<UpdateVehicleNotifications>()
                .AddTransient<QueryVehicleTrace>();
        }
    }
}
