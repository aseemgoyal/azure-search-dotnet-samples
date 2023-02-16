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
    public class Suggest
    {
        private static string searchApiKey = "OaPGBUkTWIwh3c1Hmuz1FQ4J5b7cCn4Wzf8xDlWGuiAzSeDlga7W";
        private static string searchServiceName = "sharepointsearchwithai";
        private static string searchIndexName = "azureblob-index";
        
        private readonly ILogger<Lookup> _logger;

        public Suggest(ILogger<Lookup> logger)
        {
            _logger = logger;
        }

        [Function("suggest")]
        public async Task<HttpResponseData> RunAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req, 
            FunctionContext executionContext)
        {
            // Get Document Id
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var data = JsonSerializer.Deserialize<RequestBodySuggest>(requestBody);

            // Cognitive Search 
            Uri serviceEndpoint = new($"https://{searchServiceName}.search.windows.net/");

            SearchClient searchClient = new(

                serviceEndpoint,
                searchIndexName,
                new AzureKeyCredential(searchApiKey)
            );

            SuggestOptions options = new()

            {
                Size = data.Size
            };

            var suggesterResponse = await searchClient.SuggestAsync<BookModel>(data.SearchText, data.SuggesterName, options);
            
            // Data to return
            var searchSuggestions = new Dictionary<string, List<SearchSuggestion<BookModel>>>
            {
                ["suggestions"] = suggesterResponse.Value.Results.ToList()
            };

            var response = req.CreateResponse(HttpStatusCode.Found);

            // Serialize data
            var serializer = new JsonObjectSerializer(
                new JsonSerializerOptions(JsonSerializerDefaults.Web));
            await response.WriteAsJsonAsync(searchSuggestions, serializer);
            
            return response;
        }
    }
}
