namespace Pushover.Components
{
    using System;
    using System.Net.Http;

    internal sealed class RetryOperationContainer
    {
        public Exception Exception { get; set; }

        public HttpResponseMessage OperationResponse { get; set; }

        public int RetryOperationNumber { get; set; }
    }
}
