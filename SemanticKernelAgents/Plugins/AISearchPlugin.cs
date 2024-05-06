namespace NGAAgents.Plugins
{
    using Azure.AI.OpenAI;
    using Azure;
    using Microsoft.Extensions.Logging;
    using Microsoft.SemanticKernel;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using NGAAgents.Infrastructure;
    using System.ComponentModel;
    using Azure.Search.Documents.Indexes;
    using Azure.Search.Documents;
    using Azure.Search.Documents.Models;
    using SemanticKernelAgents.Tools;

    internal class AISearchPlugin
    {
        private string AZURE_OPENAI_ENDPOINT = Config.AzureOpenAiEndpoint;
        private string AZURE_OPENAI_KEY = Config.AzureOpenAiKey;
        private string AZURE_OPENAI_MODEL = Config.ModelName;

        private string SemanticSearchConfigName = Config.SearchConfigName;
        private string ServiceEndpoint = Config.SearchServiceEndpoint;
        private string SearchServiceKey = Config.SearchServiceKey;
        private string IndexName = Config.SearchIndexName;
        private string MODEL_DEPLOYMENT_NAME = "text-embedding-ada-002";

        [KernelFunction, Description("Searches the Azure Search index for information about the Tesla based on the Tesla user manual.")]
        public async Task<string> SearchManualsIndex(
            [Description("The search query that we are searching the Azure Search Tesla Manual Index for information matching the search query.")]
            string query,
            ILogger? logger = null)
        {
            StringBuilder content = new StringBuilder();

            Console.WriteLine("Searching Manuals Index...\n");

            List<UserManualDetails> userManualDetailsList = await SemanticHybridSearch(query, logger);

            foreach (var item in userManualDetailsList)
            {
                //Console.WriteLine(item.Chunk);
                content.Append(item.Chunk);
            }

            return content.ToString();
        }

        /// <summary>
        /// Performs a semantic hybrid search for a specific trend against the vector data stored in the cognitive
        /// search database.
        /// </summary>
        /// <param name="searchClient">Search Service client</param>
        /// <param name="openAIClient">Azure OpenAI Client used create a vector representation of the query/trend</param>
        /// <param name="query">The query/trend being searched for</param>
        /// <returns>A list of details about possible matches to the given query</returns>
        public async Task<List<UserManualDetails>> SemanticHybridSearch(string query, ILogger logger)
        {
            // Initialize OpenAI client      
            var credential = new AzureKeyCredential(AZURE_OPENAI_KEY);
            var openAIClient = new OpenAIClient(new Uri(AZURE_OPENAI_ENDPOINT), credential);

            // Initialize Azure Cognitive Search clients      
            var searchCredential = new AzureKeyCredential(SearchServiceKey);
            var indexClient = new SearchIndexClient(new Uri(ServiceEndpoint), searchCredential);
            var searchClient = indexClient.GetSearchClient(IndexName);

            // Generate the embedding for the query  
            var queryEmbeddings = await GenerateEmbeddings(query, openAIClient);

            // Perform the vector similarity search  
            var searchOptions = new SearchOptions
            {
                VectorSearch = new()
                {
                    Queries = { new VectorizedQuery(queryEmbeddings.ToArray()) { KNearestNeighborsCount = 3, Fields = { "vector" } } },
                },
                SemanticSearch = new()
                {
                    SemanticConfigurationName = SemanticSearchConfigName,
                    QueryCaption = new(QueryCaptionType.Extractive),
                    QueryAnswer = new(QueryAnswerType.Extractive),
                },
                Size = 10,
                Select = { "title", "chunk" },
                QueryType = SearchQueryType.Semantic,
            };

            SearchResults<SearchDocument> response = await searchClient.SearchAsync<SearchDocument>(query, searchOptions);

            List<UserManualDetails> userManualDetailsList = new List<UserManualDetails>();

            int count = 0;
            await foreach (SearchResult<SearchDocument> result in response.GetResultsAsync())
            {
                count++;

                logger.LogInformation($"Document title: {result.Document["title"]}");
                logger.LogInformation($"Content: {result.Document["chunk"]}");
                logger.LogInformation($"Score: {result.Score}");
                logger.LogInformation($"Reranker Score: {result.SemanticSearch.RerankerScore}\n");

                UserManualDetails userManualDetails = new UserManualDetails();
                userManualDetails.Title = (string)result.Document["title"];
                userManualDetails.Chunk = (string)result.Document["chunk"];
                userManualDetails.Score = (double)result.Score;
                userManualDetails.RerankerScore = (double)result.SemanticSearch.RerankerScore;

                userManualDetailsList.Add(userManualDetails);
            }

            logger.LogInformation($"Total Results: {count}\n");

            return userManualDetailsList;
        }

        private async Task<ReadOnlyMemory<float>> GenerateEmbeddings(string text, OpenAIClient openAIClient)
        {
            EmbeddingsOptions embeddingsOptions = new()
            {
                DeploymentName = MODEL_DEPLOYMENT_NAME,
                Input = { text },
            };

            Response<Embeddings> response = await openAIClient.GetEmbeddingsAsync(embeddingsOptions);

            return response.Value.Data[0].Embedding;
        }
    }
}
