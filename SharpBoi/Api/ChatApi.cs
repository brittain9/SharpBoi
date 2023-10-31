using RestSharp;
using Newtonsoft.Json.Linq;
using SharpBoi.Database;
using SharpBoi.Database.Models;

namespace SharpBoi
{
    public class ChatApi
    {
        public RestClientOptions options { get; set; }
        public RestClient client { get; set; }
        public RestRequest request { get; set; }

        public ChatApiConfig config { get; set; }

        public ChatApi(BotDatabase db)
        {
            options = new RestClientOptions("https://api.perplexity.ai/chat/completions");
            client = new RestClient(options);

            config = db.chatApiConfig;
        }

        async public Task<string> SendRequest(string userInput)
        {
            // TODO: Optimize and customize this request more. 
            // TODO: Give error message if API key doesn't work.
            // I might stop paying for the API so I want an nice notification
            var request = new RestRequest("");
            request.AddHeader("accept", "application/json");
            request.AddHeader("authorization", "Bearer " + config.ApiKey);

            var body = new
            {
                model = config.Model,
                messages = new[]
                {
                    new
                    {
                        role = "system",
                        content = "Concise and precise",
                    },
                    new
                    {
                        role = "user",
                        content = $"Only use {config.WordCount} words, respond as {config.Prompt} to this prompt: {userInput}"
                    }
                }
            };
            request.AddJsonBody(body);

            RestResponse response = await client.PostAsync(request);

            JObject chatResponse = JObject.Parse(response.Content);
            return (string)chatResponse["choices"][0]["message"]["content"]; // return just the message
        }
    }
}