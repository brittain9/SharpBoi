using Discord.WebSocket;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using SharpBoi.Notifications;
using SharpBoi.Requests;


namespace SharpBoi
{
    public class DiscordEventListener
    {
        private readonly CancellationToken _cancellationToken;

        private readonly DiscordSocketClient _client;
        private readonly IServiceScopeFactory _serviceScope;

        public DiscordEventListener(DiscordSocketClient client, IServiceScopeFactory serviceScope)
        {
            _client = client;
            _serviceScope = serviceScope;
            _cancellationToken = new CancellationTokenSource().Token;
        }

        private IMediator Mediator
        {
            get 
            { 
                var scope = _serviceScope.CreateScope();
                return scope.ServiceProvider.GetRequiredService<IMediator>();
            }
        }

        public async Task StartAsync()
        {
            _client.Ready += OnReadyAsync;
            _client.MessageReceived += OnMessageReceivedAsync;
            _client.SlashCommandExecuted += OnSlashCommandReceivedAsync;

            await Task.CompletedTask;
        }

        // these methods send our request or notification to the appropriate handler
        private Task OnSlashCommandReceivedAsync(SocketSlashCommand arg)
        {
            return Mediator.Send(new SlashCommandRequest(arg), _cancellationToken);
        }

        private Task OnMessageReceivedAsync(SocketMessage arg)
        {
            return Mediator.Publish(new MessageReceivedNotification(arg), _cancellationToken);
        }

        private Task OnReadyAsync()
        {
            return Mediator.Publish(ReadyNotification.Default, _cancellationToken);
        }
    }
}
