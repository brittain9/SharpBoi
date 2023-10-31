using MediatR;

using SharpBoi.Notifications;

namespace SharpBoi.Handlers
{
    public class MessageReceivedHandler : INotificationHandler<MessageReceivedNotification>
    {
        public async Task Handle(MessageReceivedNotification notification, CancellationToken cancellationToken)
        {
            Console.WriteLine($"MediatR works! (Received a message by {notification.Message.Author.Username})");
            await Task.CompletedTask;
            // implement
        }
    }
}
