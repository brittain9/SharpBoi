using Discord;
using Discord.WebSocket;
using Discord.Net;
using MediatR;
using Newtonsoft.Json;
using Serilog;

using SharpBoi.Notifications;
using System.Text;

namespace SharpBoi.Handlers
{
    public class ReadyHandler : INotificationHandler<ReadyNotification>
    {
        private readonly DiscordSocketClient _client;

        public ReadyHandler(DiscordSocketClient client) 
        {
            _client = client;
        }

        public async Task Handle(ReadyNotification notification, CancellationToken cancellationToken)
        {
            // DeleteCommands();
            await CreateCommand();
            await ListCommands();
            await Task.CompletedTask;
        }

        private async Task CreateCommand()
        {
            ulong guildID = 823682849504100372;
            var guild = _client.GetGuild(guildID);

            //var cmd1 = new SlashCommandBuilder()
            //    .WithName("chat")
            //    .WithDescription("Chat with the bot")
            //    .AddOption("say", ApplicationCommandOptionType.String, "What you want to say the bot", isRequired: true);

            var cmd = new SlashCommandBuilder()
                .WithName("prompt")
                .WithDescription("Change prompt")
                .AddOption("prompt", ApplicationCommandOptionType.String, "The new prompt", isRequired: false);


            try
            {
                //await _client.CreateGlobalApplicationCommandAsync(cmd1.Build());
                await guild.CreateApplicationCommandAsync(cmd.Build());
            }
            catch (HttpException exception)
            {
                var json = JsonConvert.SerializeObject(exception.Errors, Formatting.Indented);
                Console.WriteLine(json);
            }
        }

        private async Task ListCommands()
        {
            ulong guildID = 823682849504100372;
            var guild = _client.GetGuild(guildID);

            var locals = await guild.GetApplicationCommandsAsync();
            var globals = await _client.GetGlobalApplicationCommandsAsync();

            StringBuilder s = new StringBuilder();

            s.Append($"Guild Commands in {guild.Name}:");
            foreach (var command in locals)
            {
                s.Append($"\n\t\t{command.Name}");
            }
            Log.Information(s.ToString());

            s.Clear();

            s.Append($"Global Commands:");
            foreach (var command in globals)
            {
                s.Append($"\n\t\t{command.Name}");
            }
            Log.Information(s.ToString());
        }

        private async Task DeleteCommands()
        {
            // Fetch all registered slash commands for your bot
            ulong guildID = 823682849504100372;
            var guild = _client.GetGuild(guildID);

            var locals = await guild.GetApplicationCommandsAsync();
            var globals = await _client.GetGlobalApplicationCommandsAsync();
            var all = locals.Concat(globals);

            List<string> commandsNotToDelete = new List<string>()
            {
                "purge",
                "chat",
                "say"
            };

            var unusedCommands = all.Where(cmd => !commandsNotToDelete.Contains(cmd.Name));

            // Delete the unused commands
            foreach (var command in unusedCommands)
            {
                await command.DeleteAsync();
                Console.WriteLine($"Deleted {command.Name}");
            }
        }
    }
}
