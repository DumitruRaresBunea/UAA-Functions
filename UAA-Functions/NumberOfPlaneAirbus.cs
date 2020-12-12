using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using UAA_Functions.Models;

namespace UAA_Functions
{
    public static class NumberOfPlaneAirbus
    {
        [FunctionName("NumberOfPlaneAirbus")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "airplanes/airbus")] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            HttpClient newClient = new HttpClient();
            var response = await newClient.GetAsync("https://uaa.azurewebsites.net/api/airplanes/data");

            var json = await response.Content.ReadAsStringAsync();
            var list = JsonConvert.DeserializeObject<IEnumerable<Plane>>(json);

            var manufacturers = list.GroupBy(x => x.Model).Select(x => x.First()).ToList();
            var manufacturersWithMoreTrhan200Planes = new List<PlanesPerManufacturerModel>();

            manufacturers.ForEach(manufacturer =>
            {
                var count = list.Count(x => x.Manufacturer == manufacturer.Manufacturer);
                if (manufacturer.Manufacturer.Contains("airbus", StringComparison.OrdinalIgnoreCase))
                    manufacturersWithMoreTrhan200Planes.Add(new PlanesPerManufacturerModel() { Manufacturer = manufacturer.Manufacturer, Model = manufacturer.Model, Count = count });
            });

            return new OkObjectResult(manufacturersWithMoreTrhan200Planes);
        }
    }
}