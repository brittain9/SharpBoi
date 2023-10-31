using Discord;
using Discord.Audio;
using Discord.WebSocket;
using MediatR;
using Serilog;
using FFMpegCore;
using FFMpegCore.Pipes;

using SharpBoi.Requests;

namespace SharpBoi.Handlers
{
    public class SlashCommandHandler : IRequestHandler<SlashCommandRequest>
    {
        private ChatApi _chatApi;
        private TextToSpeech _textToSpeech;
        private DiscordSocketClient _client;

        public SlashCommandHandler(ChatApi chatApi, TextToSpeech textToSpeech, DiscordSocketClient client) 
        { 
            _chatApi = chatApi;
            _textToSpeech = textToSpeech;
            _client = client;
        }

        public async Task Handle(SlashCommandRequest request, CancellationToken cancellationToken)
        {
            _ = Task.Run(async () =>
            {
                var commandName = request.SlashCommand.CommandName;
                switch (commandName)
                {
                    case "purge":
                        await PurgeHandler(request.SlashCommand);
                        break;
                    case "chat":
                        await ChatHandler(request.SlashCommand);
                        break;
                    case "say":
                        await SayHandler(request.SlashCommand);
                        break;
                    case "listen":
                        await ListenHandler(request.SlashCommand);
                        break;
                    case "prompt":
                        await PromptHandler(request.SlashCommand);
                        break;
                    default:
                        Log.Error($"Unknown command: {commandName}");
                        break;
                }
            });
            await Task.CompletedTask;
        }

        private async Task PurgeHandler(SocketSlashCommand context)
        {
            // This command may be limited by rate calls. Maybe find optimization if this causes problems
            int amount = Convert.ToInt32(context.Data.Options.First().Value);

            var embedBuiler = new EmbedBuilder()
                .WithColor(Color.DarkRed)
                .WithCurrentTimestamp();

            if (amount <= 0)
            {
                embedBuiler
                    .WithTitle("Purge failed")
                    .WithDescription("Messages must be greater than 0");
                await context.RespondAsync(embed: embedBuiler.Build(), ephemeral: true);
                return;
            }

            IEnumerable<IMessage> messages = await context.Channel.GetMessagesAsync(amount).FlattenAsync(); 

            var filteredMessages = messages.Where(x => (DateTimeOffset.UtcNow - x.Timestamp).TotalDays <= 14);
            var count = filteredMessages.Count();

            await ((ITextChannel)context.Channel).DeleteMessagesAsync(filteredMessages);

            embedBuiler
                .WithTitle("PURGED")
                .WithDescription($"{count} messages");

            await context.RespondAsync(embed: embedBuiler.Build(), ephemeral: true);
            return;
        }

        private async Task ChatHandler(SocketSlashCommand context)
        {
            var dialogue = (string)context.Data.Options.First().Value;
            var response = await _chatApi.SendRequest(dialogue);

            await context.RespondAsync($"{context.User.Username} asked: {dialogue}");
            await Task.Delay(1500);
            await context.Channel.SendMessageAsync(response);
        }

        private async Task SayHandler(SocketSlashCommand context) 
        {
            string userInput = (string)context.Data.Options.ElementAt(0).Value; // requried so just grab the value
            var channelOption = context.Data.Options.ElementAtOrDefault(1); // not require, get option and check null

            var user = (IGuildUser)context.User;

            IVoiceChannel channel = null;

            if (channelOption != null)            
                channel = (IVoiceChannel)channelOption.Value;      
            else
                channel = user.VoiceChannel;
            

            if (channel == null)
            {
                var embedBuilerFailed = new EmbedBuilder()
                    .WithTitle("Say failed")
                    .WithDescription("User must be in channel or channel provided")
                    .WithColor(Color.DarkRed)
                    .WithCurrentTimestamp();
                await context.RespondAsync(embed: embedBuilerFailed.Build(), ephemeral: true);
                return;
            }

            string response = await _chatApi.SendRequest(userInput);
            var embedBuiler = new EmbedBuilder()
                .WithTitle($"{user.Username} asked")
                .WithDescription($"{userInput}")
                .WithColor(Color.Gold)
                .WithCurrentTimestamp();
            await context.RespondAsync(embed: embedBuiler.Build(), ephemeral: true);

            Log.Information($"Saying: {response}");

            MemoryStream toTransmit = await _textToSpeech.ConvertTextToSpeechAsync(response);

            IAudioClient audioClient = await channel.ConnectAsync();
            
            var discordIn = audioClient.CreatePCMStream(AudioApplication.Mixed);

            try { await toTransmit.CopyToAsync(discordIn); }
            finally 
            {
                await discordIn.FlushAsync(); 
                audioClient.Dispose();
            }
        }

        private async Task ListenHandler(SocketSlashCommand context)
        {
            var user = (context.User as SocketGuildUser);
            var channel = user.VoiceChannel;
            if (channel == null)
            {
                context.RespondAsync("User not in channel");
                return;
            }

            IAudioClient audioClient = await channel.ConnectAsync();

            var discordIn = audioClient.CreatePCMStream(AudioApplication.Mixed);
            // I need to transmit some audio before I can listen due to discord API
            await FFMpegArguments
                .FromFileInput("apple.mp3")
                .OutputToPipe(new StreamPipeSink(discordIn), options => options
                .WithCustomArgument("-ac 2 -f s16le -ar 48000")
                )
            .ProcessAsynchronously();


            // TODO: Fix getting receiving the audio
            var audioStreams = audioClient.GetStreams();
            var userAudioStream = audioStreams[user.Id];

            CancellationTokenSource cts = new CancellationTokenSource();
            cts.CancelAfter(TimeSpan.FromSeconds(10));

            MemoryStream userAudioMem = new MemoryStream();
            try
            {
                await userAudioStream.CopyToAsync(discordIn, cts.Token);
            }
            catch(OperationCanceledException)
            {
                audioClient.Dispose();
            }

            //await FFMpegArguments
            //    .FromPipeInput(new StreamPipeSource(userAudioMem), options => options
            //    //.WithCustomArgument("-ac 2 -f s16le -ar 48000")
            //    )
            //    .OutputToFile("test.wav", true
            //    )
            //.ProcessAsynchronously();
        }

        // temp prompt handler
        private async Task PromptHandler(SocketSlashCommand context)
        {
            var option = context.Data.Options.ElementAtOrDefault(0);
            if(option == null)
            {
                await context.RespondAsync($"The current prompt is: {_chatApi.config.Prompt}");
            }
            else
            {
                _chatApi.config.Prompt = (string)option.Value;
                await context.RespondAsync($"The new prompt is: {_chatApi.config.Prompt}");
            }
            return;
        }

        // TODO: Setup Settings Handler. This will be a single command with subgroups? Need to change voice, prompt, count.
    }
}
