namespace NGAAgents
{
    using Microsoft.Extensions.Configuration;
    using Microsoft.SemanticKernel;
    using Microsoft.SemanticKernel.Experimental.Agents;
    using NGAAgents.Plugins;
    using SemanticKernelAgents.Tools;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;
    using System.Threading.Tasks;

    internal class ChatProvider
    {
        private string AZURE_OPENAI_ENDPOINT = Config.AzureOpenAiEndpoint;
        private string AZURE_OPENAI_KEY = Config.AzureOpenAiKey;
        //private string AZURE_OPENAI_MODEL = "gpt-35-turbo";
        //private string AZURE_OPENAI_MODEL = "gpt-35-turbo-16k";
        private string AZURE_OPENAI_MODEL = Config.ModelName;

        public async Task Chat()
        {
            Kernel kernel = Kernel.CreateBuilder()
                .AddAzureOpenAIChatCompletion(AZURE_OPENAI_MODEL, AZURE_OPENAI_ENDPOINT, AZURE_OPENAI_KEY)
                .AddAzureOpenAIFiles(AZURE_OPENAI_ENDPOINT, AZURE_OPENAI_KEY)
                .Build();

            /*** This uses a single thread to show how agents can collaborate - no need for a coordinator here ***/
            var carManualAgent = await new AgentBuilder()
                .WithAzureOpenAIChatCompletion(AZURE_OPENAI_ENDPOINT, AZURE_OPENAI_MODEL, AZURE_OPENAI_KEY)
                .WithInstructions(@$"You are a car agent which returns information from the user manual to the user based on their request. You will also count the number of vowels or consanants, but you must ask the user which they want to count, vowels or consanants and prompt this with VOWELS OR CONSANANTS?.")
                .WithPlugins(GetPlugins())
                .WithName("Car Manual Agent")
                .WithDescription("Search the Azure Search index for information from a car manual based on a user question and counts either or both consanants and vowels in the respons.")
                .BuildAsync();

            var customerServiceAgent = await new AgentBuilder()
                .WithAzureOpenAIChatCompletion(AZURE_OPENAI_ENDPOINT, AZURE_OPENAI_MODEL, AZURE_OPENAI_KEY)
                .WithInstructions(@$"You are a customer service AI assistant which determines if the car manual agent responded with the appropriate information based on the user query. The goal is to ask the user for additional information if needed. If the car manual agent does not include anything about an oil change, provide that feedback to the car manual agent and have the agent retry. If the search results do not match the user query, provide feedback that the search results are incorrect. Always respond to the most recent message by evaluating the result of the of the search based on the user query. If everything looks right, provide a quick response and end by saying: DONE! If user input is required, say: MORE!")
                .WithName("Customer Service Agent")
                .WithDescription("Verifies the search results from the Car Manual Agent are correct, including things such as an oil change.")
                .BuildAsync();

            var reportAgent = await new AgentBuilder()
                .WithAzureOpenAIChatCompletion(AZURE_OPENAI_ENDPOINT, AZURE_OPENAI_MODEL, AZURE_OPENAI_KEY)
                .WithInstructions(@$"You are a helpful AI assistant which takes the results from the car manual agent, as long as the customer service agent determines the output is sufficient, and generates a PDF report with the response.")
                .WithCodeInterpreter()
                .WithName("Report Generator Agent")
                .WithDescription("Generates a PDF showing the results from the car manual agent.")
                .BuildAsync();

            // threads are not attached to specific agents if you have more than one
            IAgentThread thread = await carManualAgent.NewThreadAsync();

            // add the user message
            var messageUser = await thread.AddUserMessageAsync("Tell me what the major services are that I need to perform on the car.");
            DisplayMessage(messageUser);

            // if for any reason the answer is not sufficient, utilize retry logic to break out
            int retries = 0;

            bool isComplete = false;
            do
            {
                // Initiate car manual agent input
                var agentMessages = await thread.InvokeAsync(carManualAgent).ToArrayAsync();
                DisplayMessages(agentMessages, carManualAgent);
                if (agentMessages.First().Content.Contains("VOWELS OR CONSANANTS?", StringComparison.OrdinalIgnoreCase))
                {
                    // if more information is requested, the customer service agent should prompt the user
                    string additionalInput = Console.ReadLine();

                    // add the user message
                    messageUser = await thread.AddUserMessageAsync(additionalInput);

                    agentMessages = await thread.InvokeAsync(carManualAgent).ToArrayAsync();
                }
                DisplayMessages(agentMessages, carManualAgent);

                // Initiate checking agent input
                agentMessages = await thread.InvokeAsync(customerServiceAgent).ToArrayAsync();
                DisplayMessages(agentMessages, customerServiceAgent);

                // Evaluate if goal is met
                if (agentMessages.First().Content.Contains("DONE!", StringComparison.OrdinalIgnoreCase))
                {
                    await thread.AddUserMessageAsync("Generate a report using the data from the customer service agent.");

                    // Initiate report generation agent 
                    agentMessages = await thread.InvokeAsync(reportAgent).ToArrayAsync();
                    DisplayMessages(agentMessages, reportAgent);

                    isComplete = true;
                }
                else if (agentMessages.First().Content.Contains("MORE!", StringComparison.OrdinalIgnoreCase))
                {
                    // if more information is requested, the customer service agent should prompt the user
                    string additionalInput = Console.ReadLine();

                    // add the user message
                    messageUser = await thread.AddUserMessageAsync(additionalInput);
                    //DisplayMessage(messageUser);
                }

                retries++;
            }
            while (!isComplete && retries < 3);
            /*** End single thread model ***/
        }

        public static IEnumerable<KernelPlugin> GetPlugins()
        {
            yield return KernelPluginFactory.CreateFromType<AISearchPlugin>();
            yield return KernelPluginFactory.CreateFromType<ConsanantCounterPlugin>();
            yield return KernelPluginFactory.CreateFromType<VowelCounterPlugin>();
        }

        private void DisplayMessages(IEnumerable<IChatMessage> messages, IAgent? agent = null)
        {
            foreach (var message in messages)
            {
                DisplayMessage(message, agent);
            }
        }

        private void DisplayMessage(IChatMessage message, IAgent? agent = null)
        {
            Console.WriteLine($"[{message.Id}]");
            if (agent != null)
            {
                Console.WriteLine($"# {message.Role}: ({agent.Name}) {message.Content}");
            }
            else
            {
                Console.WriteLine($"# {message.Role}: {message.Content}");
            }

            Console.WriteLine();
        }
    }
}