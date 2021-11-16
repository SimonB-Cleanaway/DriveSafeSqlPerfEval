using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using DriveSafe.SqlPerfTest;

namespace DriveSafe.SqlTPerfApi
{
    public class UpdateVehicleNotificationsAPI
    {
        private readonly UpdateVehicleNotifications _updateNotifications;

        public UpdateVehicleNotificationsAPI(UpdateVehicleNotifications updateNotifications)
        {
            _updateNotifications = updateNotifications ?? throw new ArgumentNullException(nameof(updateNotifications));
        }

        [FunctionName("UpdateVehicleNotifications")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("UpdateVehicleNotifications");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);

            var simCountParam = (string)req.Query["simCount"] ?? (data?.simCount.ToString());
            var simCount = int.TryParse(simCountParam, out int n) ? n : 100_000;

            var threadCountParam = (string)req.Query["threadCount"] ?? (data?.threadCount.ToString());
            var threadCount = int.TryParse(threadCountParam, out n) ? n : 20;

            await _updateNotifications.Run(simCount, threadCount);

            return new OkObjectResult("All Good");
        }
    }
}
