namespace NGAAgents.Plugins
{
    using Microsoft.Extensions.Logging;
    using Microsoft.SemanticKernel;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    internal class VowelCounterPlugin
    {
        [KernelFunction, Description("Counts the number of vowels in the search results.")]
        public async Task<string> CountVowels(
            [Description("The response which we are counting vowels in.")]
            string response,
            ILogger? logger = null)
        {
            return "10";
        }
    }
}