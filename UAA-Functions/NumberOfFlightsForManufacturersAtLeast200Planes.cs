using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using UAA_Functions.Models;

namespace UAA_Functions
{
    public static class NumberOfFlightsForManufacturersAtLeast200Planes
    {
        [FunctionName("NumberOfFlightsForManufacturersAtLeast200Planes")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "airplanes/flight-count")] HttpRequest req, ExecutionContext context,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            var config = new ConfigurationBuilder()
                .SetBasePath(context.FunctionAppDirectory)
                // This gives you access to your application settings in your local development environment
                .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                // This is what actually gets you the application settings in Azure
                .AddEnvironmentVariables()
                .Build();

            var dbString = config["ConnectionString"];

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

            using (SqlConnection conn = new SqlConnection(dbString))
            {
                await conn.OpenAsync();

                SqlCommand cmd = new SqlCommand();
                SqlDataReader reader;
                cmd.Connection = conn;

                var sqlManufacturers = new StringBuilder();
                manufacturersWithMoreTrhan200Planes.ForEach(item =>
                {
                    sqlManufacturers.Append($"'{item.Manufacturer}',");
                });
                sqlManufacturers.Length--;
                cmd.CommandText =
                    "SELECT " +
                        "c.manufacturer as manufacturer," +
                        " COUNT(CASE manufacturer when c.manufacturer then 1 else null end) as count " +
                    "from(" +
                        "select  p.tailnum, p.manufacturer from planes p " +
                            "inner join flights f on p.tailnum = f.tailnum " +
                            $"where manufacturer in ({sqlManufacturers})) as c " +
                    "GROUP BY c.manufacturer";

                reader = await cmd.ExecuteReaderAsync();
                List<ManufacturerCount> resultList = new List<ManufacturerCount>();

                if (reader.HasRows)
                {
                    while (await reader.ReadAsync())
                    {
                        resultList.Add(new ManufacturerCount()
                        {
                            Manufacturer = await reader.GetFieldValueAsync<string>(0),
                            Count = await reader.GetFieldValueAsync<int>(1)
                        });
                    }
                }

                conn.Close();

                return new OkObjectResult(resultList);
            }
        }
    }
}