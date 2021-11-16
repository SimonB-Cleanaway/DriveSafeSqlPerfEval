using System;
using System.IO;
using System.Threading.Tasks;
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using DriveSafe.SqlPerfTest;

namespace DriveSafe.SqlTPerfApi
{
    public class QueryVehicleTraceAPI
    {
        private readonly QueryVehicleTrace _queryVehicleTrace;

        public QueryVehicleTraceAPI(QueryVehicleTrace queryVehicleTrace)
        {
            _queryVehicleTrace = queryVehicleTrace ?? throw new ArgumentNullException(nameof(queryVehicleTrace));
        }

        [FunctionName("QueryVehicleTrace")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("UpdateVehicleLocations.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);

            var latitudeParam = (string)req.Query["latitude"] ?? (data?.latitude.ToString());
            var latitude = double.TryParse(latitudeParam, out double d) ? d : -37.8396;

            var longitudeParam = (string)req.Query["longitude"] ?? (data?.longitude.ToString());
            var longitude = double.TryParse(longitudeParam, out d) ? d : 144.9772;

            var distanceParam = (string)req.Query["distance"] ?? (data?.distance.ToString());
            var distance = int.TryParse(distanceParam, out int n) ? n : 10000;

            var backParam = (string)req.Query["back"] ?? (data?.back.ToString());
            var back = int.TryParse(backParam, out n) ? n : -24;
            DateTimeOffset from = DateTimeOffset.Now.AddHours(back);

            var forwardParam = (string)req.Query["forward"] ?? (data?.forward.ToString());
            var forward = int.TryParse(forwardParam, out n) ? n : 24;
            DateTimeOffset to = DateTimeOffset.Now.AddHours(forward);

            var sw = Stopwatch .StartNew();
            var ctr = await _queryVehicleTrace.Run(latitude,longitude,distance, from, to);
            sw.Stop();

            var msg = $"Found {ctr} traces in {sw.ElapsedMilliseconds} ms";

            log.LogInformation("UpdateVehicleLocations - " + msg);

            return new OkObjectResult(msg);
        }
    }
}
