using Azure;
using Azure.Core.Serialization;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;
using WebSearch.Models;

namespace WebSearch.Function
{
    public class Lookup
    {
        private static string searchApiKey = "OaPGBUkTWIwh3c1Hmuz1FQ4J5b7cCn4Wzf8xDlWGuiAzSeDlga7W";
        private static string searchServiceName = "sharepointsearchwithai";
        private static string searchIndexName = "azureblob-index";
        
        private readonly ILogger<Lookup> _logger;

        public Lookup(ILogger<Lookup> logger)
        {
            _logger = logger;
        }


        [Function("lookup")]
        public async Task<HttpResponseData> RunAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req, 
            FunctionContext executionContext)
        {

            // Get Document Id
            var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
            string documentId = query["id"].ToString();

            // Cognitive Search 
            Uri serviceEndpoint = new($"https://{searchServiceName}.search.windows.net/");

            SearchClient searchClient = new(

                serviceEndpoint,
                searchIndexName,
                new AzureKeyCredential(searchApiKey)
            );

            var getDocumentResponse = await searchClient.GetDocumentAsync<SearchDocument>(documentId);

            // Data to return 
            var output = new LookupOutput
            {
                Document = getDocumentResponse.Value
            };

            var response = req.CreateResponse(HttpStatusCode.Found);
            response.Headers.Add("Content-Type", "application/json; charset=utf-8");

            // Serialize data
            var serializer = new JsonObjectSerializer(
                new JsonSerializerOptions(JsonSerializerDefaults.Web));
            await response.WriteAsJsonAsync(output, serializer);

            return response;
        }
    }
}
