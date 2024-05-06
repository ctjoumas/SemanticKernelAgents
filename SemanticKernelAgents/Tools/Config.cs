namespace SemanticKernelAgents.Tools
{
    using Microsoft.Extensions.Configuration;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// Configuration for the demo based on environment:
    ///     OPENAI_KEY - The OpenAI API key
    ///     OPENAI_MODEL - The target transformer model (defaults to gpt-4-1106-preview)
    /// 
    /// Only OPENAI_KEY is required.
    /// </summary>
    internal class Config
    {
        private static IConfiguration configuration = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("local.settings.json").Build();
    
        /// <summary>
        /// Required OpenAI API key.
        /// </summary>
        public static string AzureOpenAiKey =>
            configuration["AZURE_OPENAI_KEY"] ??
            throw new InvalidOperationException("'AZURE_OPENAI_KEY' undefined.");

        /// <summary>
        /// The model name (defaults to gpt-4-1106-preview).
        /// </summary>
        public static string AzureOpenAiEndpoint =>
            configuration["AZURE_OPENAI_ENDPOINT"] ??
            throw new InvalidOperationException("'AZURE_OPENAI_ENDPOINT' undefined.");

        /// <summary>
        /// The model name (defaults to gpt-4-1106-preview).
        /// </summary>
        public static string ModelName =>
            configuration["AZURE_OPENAI_MODEL"] ??
            throw new InvalidOperationException("'AZURE_OPENAI_MODEL' undefined.");

        /// <summary>
        /// The search service key
        /// </summary>
        public static string SearchServiceKey =>
            configuration["Search_Service_Key"] ??
            throw new InvalidOperationException("'Search_Service_Key' undefined.");

        /// <summary>
        /// The search index name
        /// </summary>
        public static string SearchIndexName =>
            configuration["Search_Index_Name"] ??
            throw new InvalidOperationException("'Search_Index_Name' undefined.");

        /// <summary>
        /// The search index name
        /// </summary>
        public static string SearchConfigName =>
            configuration["Search_Config_Name"] ??
            throw new InvalidOperationException("'Search_Configx_Name' undefined.");

        /// <summary>
        /// The search index name
        /// </summary>
        public static string SearchServiceEndpoint =>
            configuration["Search_Service_Endpoint"] ??
            throw new InvalidOperationException("'Search_Service_Endpoint' undefined.");

        
    }
}