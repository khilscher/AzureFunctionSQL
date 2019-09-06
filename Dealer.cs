using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Extensions.Configuration;
using System.Data.SqlClient;
using System.Collections.Generic;

namespace Trimble
{
    public static class Dealer
    {
        [FunctionName("Dealer")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log, ExecutionContext context)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            var config = new ConfigurationBuilder()
                            .SetBasePath(context.FunctionAppDirectory)
                            .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                            .AddEnvironmentVariables()
                            .Build();

            try
            {
                var cnnString = config.GetConnectionString("CONNECTION_STRING");

                if (req.Method == HttpMethods.Post)
                {

                    string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                    dynamic data = JsonConvert.DeserializeObject(requestBody);

                    using (SqlConnection connection = new SqlConnection(cnnString))
                    {

                        var text = $"INSERT INTO Dealers (DealerName, Address, City) VALUES('{data.DealerName}', '{data.Address}', '{data.City}'); ";

                        using (SqlCommand cmd = new SqlCommand(text, connection))
                        {
                            connection.Open();
                            await cmd.ExecuteNonQueryAsync();
                            connection.Close();
                        }
                    }

                    return (ActionResult)new OkResult();
                }
                else if(req.Method == HttpMethods.Get)
                {
                    string queryString = "SELECT TOP (50) * FROM dbo.Dealers order by DealerID desc;";

                    var retList = new List<Dealers>();

                    using (SqlConnection connection = new SqlConnection(cnnString))
                    {

                        SqlCommand command = new SqlCommand(queryString, connection);
                        connection.Open();
                        SqlDataReader reader = command.ExecuteReader();

                        while (reader.Read())
                        {
                            var dealer = new Dealers();

                            dealer.DealerID = reader[0].ToString();
                            dealer.DealerName = reader[1].ToString();
                            dealer.Address = reader[2].ToString();
                            dealer.City = reader[3].ToString();

                            retList.Add(dealer);

                        }

                        reader.Close();
                    }

                    return (ActionResult)new OkObjectResult(retList);
                }
                else
                {
                    return (ActionResult)new BadRequestResult();
                }
            }
            catch (SqlException sqlex)
            {
                return (ActionResult)new BadRequestObjectResult($"The following SqlException happened: {sqlex.Message}");
            }
            catch (Exception ex)
            {
                return (ActionResult)new BadRequestObjectResult($"The following Exception happened: {ex.Message}");
            }

        }
    }

    public class Dealers
    {
        public string DealerID { get; set; }
        public string DealerName { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
    }
}
