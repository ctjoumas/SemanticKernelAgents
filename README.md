# SemanticKernelAgents
This sample code uses the Semantic Kernel experimental Agent framework to show how multiple agents can collaborate on a single thread in order to carry out a single task:
- Car Manual Agent: Responsible for utilizing an Azure Search plugin, which performs a semantic hybrid vector search Azure Search in order to return matching documents from an indexed car manual, and summarizing the answer to a query (hardcoded as finding the major services required). It also has two other plugins and queries the user to use one or the other in order to show how a plan can be altered based on user input. TODO: update the instructions to include a plan to use both plugins (vowel and consonant plugin).
- Customer Service Agent: Responsible for verifying that the car manual agent has provided information that accurately answers the query
- Report Agent: Uses Assistant API (this experimental agent targets the REST endpoints rather than the SDK) code interpretor in order to generating a report with the summarized information answering the query

To run, rename the local.settings.json.rename to local.settings.json and fill in the values for the Azure OpenAI variables.
