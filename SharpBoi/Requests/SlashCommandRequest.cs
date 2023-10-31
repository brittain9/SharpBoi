using Discord.WebSocket;
using MediatR;

namespace SharpBoi.Requests
{
    public class SlashCommandRequest : IRequest
    {
        public SlashCommandRequest(SocketSlashCommand slashCommand) 
        {
            SlashCommand = slashCommand;
        }
        public SocketSlashCommand SlashCommand { get;}
    }
}
 