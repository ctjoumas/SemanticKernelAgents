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

    internal class ConsanantCounterPlugin
    {
        [KernelFunction, Description("Counts the number of consanants in the search results.")]
        public async Task<string> CountConsanants(
            [Description("The response which we are counting consanants in.")]
            string response,
            ILogger? logger = null)
        {
            return "10";
        }
    }
}
