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
    public class UpdateVehicleLocationsAPI
    {
        private readonly UpdateVehicleLocations _updateVehicleLocations;

        public UpdateVehicleLocationsAPI(UpdateVehicleLocations updateVehicleLocations)
        {
            _updateVehicleLocations = updateVehicleLocations ?? throw new ArgumentNullException(nameof(updateVehicleLocations));
        }


        [FunctionName("UpdateVehicleLocations")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("UpdateVehicleLocations.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);

            var vehicleCountParam = (string)req.Query["vehicleCount"] ?? (data?.vehicleCount.ToString());
            var vehicleCount = int.TryParse(vehicleCountParam, out int n) ? n : 2000;

            var simCountParam = (string)req.Query["simCount"] ?? (data?.simCount.ToString());
            var simCount = int.TryParse(simCountParam, out n) ? n : 250_000;

            var threadCountParam = (string)req.Query["threadCount"] ?? (data?.threadCount.ToString());
            var threadCount = int.TryParse(threadCountParam, out n) ? n : 20;

            await _updateVehicleLocations.Run(vehicleCount, simCount, threadCount);

            return new OkObjectResult("All Good");
        }
    }
}
