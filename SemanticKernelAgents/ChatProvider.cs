namespace NGAAgents
{
    using Microsoft.SemanticKernel;
    using Microsoft.SemanticKernel.Agents;
    using Microsoft.SemanticKernel.Agents.Chat;
    using Microsoft.SemanticKernel.Agents.OpenAI;
    using Microsoft.SemanticKernel.ChatCompletion;
    using Microsoft.SemanticKernel.Connectors.OpenAI;
    using NGAAgents.Plugins;
    using SemanticKernelAgents.Tools;
    using System;
    using System.Threading.Tasks;

    internal class ChatProvider
    {
        private string AZURE_OPENAI_ENDPOINT = Config.AzureOpenAiEndpoint;
        private string AZURE_OPENAI_KEY = Config.AzureOpenAiKey;
        private string AZURE_OPENAI_MODEL = Config.ModelName;

        private const string CarManualAgentName = "CarManualAgent";
        private const string CarManualAgentInstructions =
            """
            You are a car agent which returns a summary of information from a car user manual based on a user's request. The goal is to neatly summarize the information and not perform any other task.
            Do not perform any other task other than providing this summary of information from the car user manual using the search plugin. There will be a vowel agent to count the vowels, a consonant
            agent to count the consonants, and a report agent to generate the report. You will not perform these tasks and let these other agents perform these tasks."
            """;

        private const string VowelAgentName = "VowelAgent";
        private const string VowelAgentInstructions =
            """
            You are a vowel counter assistant which takes the summary from a car user manual and counts the number of vowels in the summary. The goal is to only count the number of vowels
            and you will not perform any other task other than counting vowels. There will be a consonant agent to count the consonants and a report agent to generate the report. You will
            not perform these tasks and let these other agents perform these tasks.
            """;
        
        private const string ConsonantAgentName = "ConsonantAgent";
        private const string ConsonantAgentInstructions =
            """
            You are a consonant counter assistant which takes the summary from a car user manual and counts the number of consonant in the summary. The goal is to only count the number of consonants
            and you will not perform any other task other than counting consonants. There will be a report agent to generate the report. You will not attempt to generate a report and let the report
            agent generate the report.
            """;

        private const string ReportGeneratorAgentName = "ReportGeneratorAgent";
        private const string ReportGeneratorAgentInstructions =
            """
            You are a report generator assistant which takes the sumamry from a car user manual as well as the provided vowel and consonant counts and generates a PDF report. Once the report
            is generated, say: DONE!
            """;

        public async Task Chat()
        {
            Kernel kernel = CreateKernelWithChatCompletion();

            ChatCompletionAgent carManualAgent =
                new()
                {
                    Name = CarManualAgentName,
                    Instructions = CarManualAgentInstructions,
                    Kernel = kernel,
                    ExecutionSettings = new OpenAIPromptExecutionSettings() { ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions },
                };

            // Add plugins to the agent's Kernel
            //carManualAgent.Kernel.Plugins.AddRange(GetPlugins());
            carManualAgent.Kernel.Plugins.Add(KernelPluginFactory.CreateFromType<AISearchPlugin>());

            OpenAIAssistantAgent vowelAgent =
                await OpenAIAssistantAgent.CreateAsync(
                    kernel: new(),
                    config: new(AZURE_OPENAI_KEY, AZURE_OPENAI_ENDPOINT),
                    new()
                    {
                        Name = VowelAgentName,
                        Instructions = VowelAgentInstructions,
                        ModelId = AZURE_OPENAI_MODEL,
                        EnableCodeInterpreter = true
                    });

            OpenAIAssistantAgent consonantAgent =
                await OpenAIAssistantAgent.CreateAsync(
                    kernel: new(),
                    config: new(AZURE_OPENAI_KEY, AZURE_OPENAI_ENDPOINT),
                    new()
                    {
                        Name = ConsonantAgentName,
                        Instructions = ConsonantAgentInstructions,
                        ModelId = AZURE_OPENAI_MODEL,
                        EnableCodeInterpreter = true
                    });

            OpenAIAssistantAgent reportGeneratorAgent =
                await OpenAIAssistantAgent.CreateAsync(
                    kernel: new(),
                    config: new(AZURE_OPENAI_KEY, AZURE_OPENAI_ENDPOINT),
                    new()
                    {
                        Name = ReportGeneratorAgentName,
                        Instructions = ReportGeneratorAgentInstructions,
                        ModelId = AZURE_OPENAI_MODEL,
                        EnableCodeInterpreter = true
                    });


            KernelFunction selectionFunction =
                KernelFunctionFactory.CreateFromPrompt(
                    $$$"""
                    Your job is to determine which participant takes the next turn in a conversation according to the action of the most recent participant.
                    State only the name of the participant to take the next turn.
                
                    Choose only from these participants:
                    - {{{CarManualAgentName}}}
                    - {{{VowelAgentName}}}
                    - {{{ConsonantAgentName}}}
                    - {{{ReportGeneratorAgentName}}}
                
                    Always follow these rules when selecting the next participant:
                    - After user input, it is {{{CarManualAgentName}}}'s turn.
                    - After {{{CarManualAgentName}}} replies, it is {{{VowelAgentName}}}'s turn.
                    - After {{{VowelAgentName}}} replies, it is {{{ConsonantAgentName}}}'s turn.
                    - After {{{VowelAgentName}}} replies, it is {{{ReportGeneratorAgentName}}}'s turn.

                    History:
                    {{$history}}
                    """);

            // Create a chat for agent interactions
            AgentGroupChat chat =
                //new(carManualAgent, customerServiceAgent, reportGeneratorAgent)
                new(carManualAgent, vowelAgent, consonantAgent, reportGeneratorAgent)
                {
                    ExecutionSettings =
                        new()
                        {
                            // TerminationStrategy subclass is used that will terminate when the customer service agent says "DONE!"
                            TerminationStrategy =
                                new ApprovalTerminationStrategy()
                                {
                                    // Only the customer service agent may consider this done
                                    //Agents = [customerServiceAgent],
                                    Agents = [reportGeneratorAgent],
                                    // Limit total number of turns
                                    MaximumIterations = 10,
                                },
                            // Here a KernelFunctionSelectionStrategy selects agents based on a prompt function
                            SelectionStrategy =
                                new KernelFunctionSelectionStrategy(selectionFunction, CreateKernelWithChatCompletion())
                                {
                                    // Returns the entire result value as a string.
                                    ResultParser = (result) => result.GetValue<string>() ?? ReportGeneratorAgentName,
                                    // The prompt variable name for the agents argument.
                                    AgentsVariableName = "agents",
                                    // The prompt variable name for the history argument.
                                    HistoryVariableName = "history",
                                },
                        }
                };

            // invoke the chat and display messages
            string agentsTask =
                """
                Tell me what the major services are that I need to perform on the car. Count the number of vowels and consonants that exist in this response and then generate a PDF report of the summary and vowels/consanants.
                """;
            chat.AddChatMessage(new ChatMessageContent(AuthorRole.User, agentsTask));
            Console.WriteLine($"# {AuthorRole.User}: '{agentsTask}\n");

            await foreach(var content in chat.InvokeAsync())
            {
                SetConsoleForegroundColor(content.AuthorName);

                Console.WriteLine($"# {content.Role} - {content.AuthorName ?? "*"}: '{content.Content}'\n");

                /*if (content.Content.Contains("anything else", StringComparison.OrdinalIgnoreCase))
                {
                    string userInput = Console.ReadLine();

                    chat.AddChatMessage(new ChatMessageContent(AuthorRole.User, userInput));
                }*/
            }

            Console.ForegroundColor = ConsoleColor.White;

            Console.WriteLine($"# IS COMPLETE: {chat.IsComplete}");
        }

        protected Kernel CreateKernelWithChatCompletion()
        {
            // Create the Kernel
            Kernel kernel = Kernel.CreateBuilder()
                .AddAzureOpenAIChatCompletion(AZURE_OPENAI_MODEL, AZURE_OPENAI_ENDPOINT, AZURE_OPENAI_KEY)
                .AddAzureOpenAIFiles(AZURE_OPENAI_ENDPOINT, AZURE_OPENAI_KEY)
                .Build();

            return kernel;
        }


        /// <summary>
        /// Sets the foreground color of the console app for each agent for readability.
        /// </summary>
        /// <param name="agentName"></param>
        private void SetConsoleForegroundColor(string agentName)
        {
            if (agentName.Equals("user"))
            {
                Console.ForegroundColor = ConsoleColor.White;
            }
            else if (agentName.Equals(CarManualAgentName))
            {
                Console.ForegroundColor = ConsoleColor.Blue;
            }
            else if (agentName.Equals(VowelAgentName))
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
            }
            else if (agentName.Equals(ConsonantAgentName))
            {
                Console.ForegroundColor = ConsoleColor.Gray;
            }
            else if (agentName.Equals(ReportGeneratorAgentName))
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
            }
        }

        public static IEnumerable<KernelPlugin> GetPlugins()
        {
            yield return KernelPluginFactory.CreateFromType<AISearchPlugin>();
            yield return KernelPluginFactory.CreateFromType<ConsanantCounterPlugin>();
            yield return KernelPluginFactory.CreateFromType<VowelCounterPlugin>();
        }
    }
    public class ApprovalTerminationStrategy : TerminationStrategy
    {
        // Terminate when the final message contains the term "DONE!"
        protected override Task<bool> ShouldAgentTerminateAsync(Agent agent, IReadOnlyList<ChatMessageContent> history, CancellationToken cancellationToken)
            => Task.FromResult(history[history.Count - 1].Content?.Contains("DONE!", StringComparison.OrdinalIgnoreCase) ?? false);
    }
}