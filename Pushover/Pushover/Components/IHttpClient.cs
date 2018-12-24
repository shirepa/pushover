namespace Pushover.Components
{
    using System.Threading.Tasks;
    using JetBrains.Annotations;
    using Pushover.Components;

    public interface IHttpClient

    {
        TOut Post<TIn, TOut>(string uriPath, TIn value, HttpRequestOptions httpRequestOptions) where TIn : class;

        void Post<TIn>(string uriPath, TIn value, HttpRequestOptions httpRequestOptions) where TIn : class;

        void Post(string uriPath,  HttpRequestOptions httpRequestOptions);

        Task PostAsync<TIn>( string uriPath, TIn value,  HttpRequestOptions httpRequestOptions) where TIn : class;
    }
}
