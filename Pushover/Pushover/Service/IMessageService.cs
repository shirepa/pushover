namespace Pushover.Service
{
    using Pushover.Dto;

    public interface IMessageService
    {
       void SendMessage(PushMessage message);
    }
}
