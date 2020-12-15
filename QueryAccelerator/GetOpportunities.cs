using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using QueryAccelerator.Model;

namespace QueryAccelerator
{
    public static class GetOpportunities
    {
        [FunctionName("GetOpportunities")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string name = req.Query["name"];

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            name = name ?? data?.name;


            var connection = Environment.GetEnvironmentVariable("azureconnection");
            var containerName = "commondataservice-kapish2020-ca7639d241a143b695207c2fb17d54";
 
            BlobContainerClient container = new BlobContainerClient(connection, containerName);
   
            var model = container.GetBlobs().Where(blb => blb.Name == "model.json").FirstOrDefault();
            var blobC = new BlobClient(connection, containerName, model.Name);
            var resp = await blobC.DownloadAsync();

            StringBuilder sb = new StringBuilder();
            using (var streamReader = new StreamReader(resp.Value.Content))
            {
                while (!streamReader.EndOfStream)
                {
                    var line = await streamReader.ReadLineAsync();
                    sb.Append(line);
                }
            }

            dynamic modeldata = JsonConvert.DeserializeObject(sb.ToString());
            JObject jobject = modeldata;
            JObject entity = (JObject)jobject.GetValue("entities").FirstOrDefault();
            var attributes = entity.GetValue("attributes");
            List<string> attributesList = new List<string>();
            foreach (var att in attributes)
            {
                attributesList.Add(((JObject)att).GetValue("name").ToString());
            }


            var blobs = container.GetBlobs().Where(blb => blb.Name == "opportunity/Snapshot/2020_1608038345.csv");
            //kan jobba med filtret här för att få rätt blobs, så att jag får en snapshot i månaden osv.

            List<OpportunityHistory> opportunities = new List<OpportunityHistory>();
            foreach (var blob in blobs)
            {
                log.LogInformation(blob.Name);
                var blockBlob = new BlockBlobClient(connection, containerName, blob.Name);
                opportunities.AddRange(await QueryFile(blockBlob, log, attributesList));
            }

            return new OkObjectResult(opportunities);
        }

        static async Task<List<OpportunityHistory>> QueryFile(BlockBlobClient blob, ILogger log, List<string> attributes)
        {
            var id = attributes.FindIndex(str => str == "Id") + 1;
            var snapshotdate = attributes.FindIndex(str => str == "SinkModifiedOn") + 1;
            var estimatedvalue = attributes.FindIndex(str => str == "estimatedvalue") + 1;
            var name = attributes.FindIndex(str => str == "name")+1;
            string query = $"SELECT _{id}, _{snapshotdate}, _{estimatedvalue}, _{name} FROM BlobStorage";
            return await DumpQueryCsv(blob, query, false, log, attributes);
        }

        private static async Task<List<OpportunityHistory>> DumpQueryCsv(BlockBlobClient blob, string query, bool headers, ILogger log, List<string> attributes)
        {
            try
            {
                List<OpportunityHistory> opportunities = new List<OpportunityHistory>();
                var id = attributes.FindIndex(str => str == "Id");
                var snapshotdate = attributes.FindIndex(str => str == "SinkModifiedOn");
                var estimatedvalue = attributes.FindIndex(str => str == "estimatedvalue");
                var name = attributes.FindIndex(str => str == "name");

                var counter = 0;

                var options = new BlobQueryOptions()
                {
                    InputTextConfiguration = new BlobQueryCsvTextOptions() { HasHeaders = headers },
                    OutputTextConfiguration = new BlobQueryCsvTextOptions() { HasHeaders = true },
                    ProgressHandler = new Progress<long>((finishedBytes) => Console.Error.WriteLine($"Data read: {finishedBytes}"))
                };
                options.ErrorHandler += (BlobQueryError err) => {
                };
                // BlobDownloadInfo exposes a Stream that will make results available when received rather than blocking for the entire response.
                using (var reader = new StreamReader((await blob.QueryAsync(
                        query,
                        options)).Value.Content))
                {
                    using (var parser = new CsvReader(reader, new CsvConfiguration(CultureInfo.CurrentCulture) { HasHeaderRecord = true }))
                    {
                        while (await parser.ReadAsync())
                        {
                            //get indexOf prop


                            log.LogInformation(String.Join(" ", parser.Context.Record));
                            DateTime parsedDT;
                            decimal parsedDec;
                            opportunities.Add(new OpportunityHistory
                            {
                                Id = new Guid(parser.Context.Record[0]),
                                EstimatedRevenue = decimal.TryParse(parser.Context.Record[2], out parsedDec)? parsedDec:0,
                                SnapshotDate = DateTime.TryParse(parser.Context.Record[1], out parsedDT) ? (DateTime?)parsedDT : null,
                                Topic = parser.Context.Record[3]
                            });
                            counter++;
                            log.LogInformation("Counter: "+counter.ToString());
                        }
                    }
                }

                return opportunities;
            }
            catch (Exception ex)
            {
                log.LogError("Exception: " + ex.ToString());
                return null;
            }
        }
    }
}
