using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Pushover.Components
{
        public sealed class HttpRequestOptions
        {
            private const int HttpTimeoutMsDefault = 100000;
            private const int MaxFailedConnectionsDefault = 10;
            private const bool InstantFailoversDefault = true;

            private readonly Dictionary<string, string> headersDictionary = new Dictionary<string, string>();

            public HttpRequestOptions()
            {
                DownNodeOnHttpCodes = new List<int>();
            }

            /// <summary>
            /// Ammount of connections before failover should occur
            /// </summary>
            public int MaxFailedConnections { get; set; }

            /// <summary>
            /// Sends retry to any other available node
            /// </summary>
            public bool InstantFailover { get; set; }

            /// <summary>
            /// Timeout for http requests
            /// </summary>
            public int HttpTimeoutMs { get; set; }

            /// <summary>
            /// List of HTTP status codes which mark node as down
            /// </summary>
            public List<int> DownNodeOnHttpCodes { get; }

            /// <summary>
            /// Dictionary of HTTP Headers (key, value) which add to request
            /// </summary>
            public IDictionary<string, string> Headers => headersDictionary;

            public static HttpRequestOptions Default => new HttpRequestOptions
            {
                MaxFailedConnections = MaxFailedConnectionsDefault,
                HttpTimeoutMs = HttpTimeoutMsDefault,
                InstantFailover = InstantFailoversDefault
            };
        }
}
