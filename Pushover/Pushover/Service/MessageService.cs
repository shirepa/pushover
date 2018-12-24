namespace Pushover.Service
{
    using Pushover.Dto;
    using Pushover.Components;

    public class MessageService : IMessageService
    {

        private readonly HttpRequestOptions messageHttpRequestOptions = new HttpRequestOptions { HttpTimeoutMs = 180000, InstantFailover = false };
        private readonly IHttpClient httpClient;
        private const string PushEndpoint = "/1/messages.json";


        public MessageService(IHttpClient httpClient)
        {
            this.httpClient = httpClient;
        }

        public void SendMessage(PushMessage message)
        {
            httpClient.Post(PushEndpoint, message, messageHttpRequestOptions);
        }
    }
}
