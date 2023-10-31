using MediatR;

namespace SharpBoi.Notifications
{
    public class ReadyNotification : INotification
    {
        public static readonly ReadyNotification Default
            = new();

        private ReadyNotification() { }
    }
}
