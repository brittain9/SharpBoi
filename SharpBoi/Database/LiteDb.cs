using LiteDB;
using Serilog;
using SharpBoi.Database.Models;

namespace SharpBoi.Database
{
    public class BotDatabase
    {
        private LiteDatabase db;

        public ChatApiConfig chatApiConfig { get; }
        public DiscordBotConfig discordBotConfig { get; }
        public SpeechConfig speechConfig { get; }

        public BotDatabase()
        {
            db = new LiteDatabase(Path.GetTempPath() + "SharpBoi.db");

            chatApiConfig = InitChatApiConfig(); // Setup or load Discord Bot config and token
            discordBotConfig = InitDiscordBotConfig(); // Setup or load Perplexity API config and API key
            speechConfig = InitSpeechConfig();
        }

        public ChatApiConfig InitChatApiConfig()
        {
            // This will create a new table in our db called "chat_api_config" if it doesn't exist.
            // If it exists, then it will retrieve that table
            var chatApiDb = db.GetCollection<ChatApiConfig>("chat_api_config");

            // If the config doesn't exist yet.
            if (chatApiDb.Count() == 0) 
            {
                // This is a new collection so we need to prompt for perplexity API key. Requires PRO $20 subscription
                Console.WriteLine("First time setup for ChatAPI");
                string key = "invalid";
                while (key.Substring(0, 5) != "pplx-")
                {
                    Console.WriteLine("Please enter Perplexity API key starting with 'pplx-': ");
                    key = Console.ReadLine();
                }

                // Create the config and insert into our database
                var newConfig = new ChatApiConfig
                {
                    ApiKey = key,
                    Prompt = "Friendly Robot",
                    WordCount = 25,
                    Model = "mistral-7b-instruct"
                };
                chatApiDb.Insert(newConfig);
            }

            var config = chatApiDb.FindById(1);
            Log.Information($"Chat API loaded with settings:\n" +
                $"\tPrompt: {config.Prompt}\n" +
                $"\tWord Count: {config.WordCount}\n" +
                $"\tModel: {config.Model}\n");

            return config;

        }
        public DiscordBotConfig InitDiscordBotConfig()
        {
            var discordBotDb = db.GetCollection<DiscordBotConfig>("discord_bot_config");

            if (discordBotDb.Count() == 0)
            {
                Console.WriteLine("First time setup for Discord Bot\nEnter Discord Token: ");
                string token = Console.ReadLine();

                var newConfig = new DiscordBotConfig
                {
                    Token = token,
                    Prefix = "!"
                };
                discordBotDb.Insert(newConfig);
            }
            var config = discordBotDb.FindById(1);
            Log.Information($"Discord Bot loaded with settings:\n" +
                $"\tPrefix: {config.Prefix}\n");

            return config;
        }
        public SpeechConfig InitSpeechConfig()
        {
            var speechDb = db.GetCollection<SpeechConfig>("speech_config");

            if (speechDb.Count() == 0)
            {
                var newConfig = new SpeechConfig
                {
                    VoiceName = "en-GB-Neural2-B",
                    LangCode = "en-GB",
                    Gender = Google.Cloud.TextToSpeech.V1.SsmlVoiceGender.Male
                };
                speechDb.Insert(newConfig);
            }

            // Idk why but the db ID for this is not 1.
            var config = speechDb.FindOne(Query.All());
            Log.Information($"Speech config loaded with settings:\n\t" +
                $"Voice: {config.VoiceName}\n\t" +
                $"LangCode: {config.LangCode}\n\t" +
                $"Gender: {config.Gender}\n");

            return config;
        }
    }
}




