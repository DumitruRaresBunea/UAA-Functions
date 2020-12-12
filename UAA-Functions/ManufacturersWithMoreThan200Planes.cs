using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using UAA_Functions.Models;

namespace UAA_Functions
{
    public static class ManufacturersWithMoreThan200Planes
    {
        [FunctionName("ManufacturersWithMoreThan200Planes")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "airplanes")] ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            HttpClient newClient = new HttpClient();
            var response = await newClient.GetAsync("https://uaa.azurewebsites.net/api/airplanes/data");

            var json = await response.Content.ReadAsStringAsync();
            var list = JsonConvert.DeserializeObject<IEnumerable<Plane>>(json);

            var manufacturers = list.GroupBy(x => x.Manufacturer).Select(x => x.First()).Select(x => x.Manufacturer).ToList();
            var manufacturersWithMoreTrhan200Planes = new List<PlanesPerManufacturer>();

            manufacturers.ForEach(manufacturer =>
            {
                var count = list.Count(x => x.Manufacturer == manufacturer);
                if (count > 200)
                    manufacturersWithMoreTrhan200Planes.Add(new PlanesPerManufacturer() { Manufacturer = manufacturer, Count = count });
            });

            return new OkObjectResult(manufacturersWithMoreTrhan200Planes);
        }
    }
}