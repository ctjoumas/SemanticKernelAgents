# SemanticKernelAgents
This sample code uses the Semantic Kernel Agent framework v2 which supersedes the experimental Agent framework and shows how multiple agents can collaborate in a group chat to carry out a series of tasks:
- Car Manual Agent: Responsible for utilizing an Azure Search plugin, which performs a semantic hybrid vector search Azure Search in order to return matching documents from an indexed car manual, and summarizing the answer to a query (hardcoded as finding the major services required). It also has two other plugins and queries the user to use one or the other in order to show how a plan can be altered based on user input. TODO: update the instructions to include a plan to use both plugins (vowel and consonant plugin).
- Vowel Counter Agent: Responsible for counting the number of vowels in the car manual agent's response
- Consonant Counter Agent: Responsible for counting the number of consonants in the car manual agent's response, using the Assistant API agent targeting the Assistant API
- Report Agent: Uses Assistant API code interpretor in order to generating a report with the summarized information answering the query

The new framework introduces a termination strategy which prevents the group chat from going on forever as well as a selection strategy which provides a deterministic order of communication between the agents.

To run, rename the local.settings.json.rename to local.settings.json and fill in the values for the Azure OpenAI variables.
