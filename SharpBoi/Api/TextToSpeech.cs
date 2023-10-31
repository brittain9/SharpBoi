using Google.Cloud.TextToSpeech.V1;
using FFMpegCore.Pipes;
using FFMpegCore;

using SharpBoi.Database;
using SharpBoi.Database.Models;
using Serilog;
using Discord.Audio.Streams;
using Discord.Audio;
using Google.Protobuf;

namespace SharpBoi
{
    public class Voice
    {
        private string Name { get; set; }
        private string LangCode { get; set; }
        private SsmlVoiceGender Gender { get; set; }

        private Voice(string name, string lang, SsmlVoiceGender gender)
        {
            Name = name;
            LangCode = lang;
            Gender = gender;
        }

        // Arrays used to check if voice exists
        public static readonly string[] accents = {
            "american",
            "australian",
            "british",
            "indian",
            "russian"
        };
        public static readonly string[] genders = {
            "male",
            "female"
        };

        public static Voice GetVoice(string accent, string gender)
        {
            // Returns Voice of selected accent or null if voice doesn't exist

            accent = accent.ToLower();
            gender = gender.ToLower();

            // American
            if (accent == accents[0])
            {
                if (gender == genders[0])
                {
                    return new Voice("en-US-Neural2-I", "en-US", SsmlVoiceGender.Male);
                }
                else if (gender == genders[1])
                {
                    return new Voice("en-US-Neural2-H", "en-US", SsmlVoiceGender.Female);
                }
            }
            // Australian
            else if (accent == accents[1])
            {
                if (gender == genders[0])
                {
                    return new Voice("en-AU-Neural2-D", "en-AU", SsmlVoiceGender.Male);
                }
                else if (gender == genders[1])
                {
                    return new Voice("en-AU-Neural2-C", "en-AU", SsmlVoiceGender.Female);
                }
            }
            // British
            else if(accent == accents[2])
            {
                if (gender == genders[0])
                {
                    return new Voice("en-GB-Neural2-D", "en-GB", SsmlVoiceGender.Male);
                }
                else if (gender == genders[1])
                {
                    return new Voice("en-GB-Neural2-F", "en-GB", SsmlVoiceGender.Female);
                }
            }
            // Indian
            else if(accent == accents[3])
            {
                if (gender == genders[0])
                {
                    return new Voice("en-IN-Neural2-C", "en-IN", SsmlVoiceGender.Male);
                }
                else if (gender == genders[1])
                {
                    return new Voice("en-IN-Neural2-A", "en-IN", SsmlVoiceGender.Female);
                }
            }
            // Russian
            else if(accent == accents[4])
            {
                if (gender == genders[0])
                {
                    return new Voice("ru-RU-Standard-D", "ru-RU", SsmlVoiceGender.Male);
                }
                else if (gender == genders[1])
                {
                    return new Voice("ru-RU-Standard-C", "ru-RU", SsmlVoiceGender.Female);
                }
            }
            return null;
        }
    }

    public class TextToSpeech
    {
        // Hopefully storing these here instead of creating each time will make the response faster
        private readonly TextToSpeechClient _client;
        private readonly AudioConfig _audioConfig;

        // TODO: Create a method for editing voice to be used in combination with slash command
        private VoiceSelectionParams _voiceSelection;

        public static SpeechConfig speechConfig { get; set; }

        public TextToSpeech(BotDatabase db) 
        {
            speechConfig = db.speechConfig;

            // Need to use Google Cloud SDK shell to set up credentials for API
            // https://cloud.google.com/docs/authentication/application-default-credentials
            Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS",
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)
                + @"\gcloud\application_default_credentials.json");

            _client = TextToSpeechClient.Create();

            _audioConfig = new AudioConfig
            {
                AudioEncoding = AudioEncoding.OggOpus
            };

            _voiceSelection = new VoiceSelectionParams()
            {
                LanguageCode = speechConfig.LangCode,
                SsmlGender = speechConfig.Gender,
                Name = speechConfig.VoiceName
            };
        }

        public async Task<MemoryStream> ConvertTextToSpeechAsync(string text)
        {
            SynthesisInput input = new SynthesisInput
            {
                Text = text
            };

            SynthesizeSpeechResponse response = await
                _client.SynthesizeSpeechAsync(input, _voiceSelection, _audioConfig);

            MemoryStream GoogleStream = new MemoryStream();
            response.AudioContent.WriteTo(GoogleStream);
            GoogleStream.Position = 0;

            var DiscordStream = new MemoryStream();

            await FFMpegArguments
                .FromPipeInput(new StreamPipeSource(GoogleStream))
                .OutputToPipe(new StreamPipeSink(DiscordStream), options => options
                .WithCustomArgument("-ac 2 -f s16le -ar 48000")
                )
            .ProcessAsynchronously();

            DiscordStream.Position = 0;
            return DiscordStream;
        }
    }
}
