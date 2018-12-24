namespace Pushover.Components
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Net.Security;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading;
    using System.Threading.Tasks;
    using EnsureThat;
    using Newtonsoft.Json;

    public class ReliableHttpClient : IHttpClient
    {
        private const int MaxValidStatusCodeForRetryOperation = 300;

        private const string RequestPostMethod = "POST";

        private const string JsonContentType = "application/json";

        private readonly Uri BaseUrl = new Uri("https://api.pushover.net");

        private CancellationToken httpClientToken = new CancellationToken(false);


        private readonly JsonSerializer jsonSerializer = JsonSerializer.Create();

        public void Post<TIn>(string uriPath, TIn value, HttpRequestOptions httpRequestOptions)
          where TIn : class
        {
            EnsureArg.IsNotNullOrEmpty(uriPath);
            EnsureArg.IsNotNull(value);
            EnsureArg.IsNotNull(httpRequestOptions);

            var content = GetContent(value);

            var request = BuildHttpRequestMessage(uriPath, RequestPostMethod, httpRequestOptions, content);

            var response = Send(request, httpRequestOptions);

            CheckResponse(response);
        }

        public void Post(string uriPath, HttpRequestOptions httpRequestOptions)
        {
            EnsureArg.IsNotNullOrEmpty(uriPath);
            EnsureArg.IsNotNull(httpRequestOptions);

            var request = BuildHttpRequestMessage(uriPath, RequestPostMethod, httpRequestOptions);

            var response = Send(request, httpRequestOptions);

            CheckResponse(response);
        }

        public async Task PostAsync<TIn>(string uriPath, TIn value, HttpRequestOptions httpRequestOptions) where TIn : class
        {
            EnsureArg.IsNotNullOrEmpty(uriPath);
            EnsureArg.IsNotNull(value);
            EnsureArg.IsNotNull(httpRequestOptions);

            var content = GetContent(value);

            var request = BuildHttpRequestMessage(uriPath, RequestPostMethod, httpRequestOptions, content);

            var response = await SendAsync(request, httpRequestOptions);

            CheckResponse(response);
        }

        public TOut Post<TIn, TOut>(string uriPath, TIn value, HttpRequestOptions httpRequestOptions) where TIn : class
        {
            EnsureArg.IsNotNullOrEmpty(uriPath);
            EnsureArg.IsNotNull(value);

            var content = GetContent(value);

            var request = BuildHttpRequestMessage(uriPath, RequestPostMethod, httpRequestOptions, content);

            var response = Send(request, httpRequestOptions);

            return ParseResponse<TOut>(response);
        }


        private byte[] GetContent<T>(T value)
        {
            byte[] content;
            using (var ms = new MemoryStream())
            {
                using (var msWriter = new StreamWriter(ms))
                {
                    jsonSerializer.Serialize(msWriter, value);
                }

                ms.Flush();
                content = ms.ToArray();
            }

            return content;
        }

        private HttpRequestMessage BuildHttpRequestMessage(
          string uriPath,
          string requestMethod,
          HttpRequestOptions httpClientConfiguration,
          byte[] content = null)
        {
            var message = new HttpRequestMessage(new HttpMethod(requestMethod), new Uri(BaseUrl, uriPath));
            if (content != null)
            {
                var hContent = new ByteArrayContent(content);
                hContent.Headers.Add("Content-Type", JsonContentType);
                message.Content = hContent;
            }

            foreach (var header in httpClientConfiguration.Headers)
            {
                message.Headers.Add(header.Key, header.Value);
            }

            return message;
        }

        private HttpResponseMessage Send(HttpRequestMessage request, HttpRequestOptions httpRequestOptions)
        {
            var response = new HttpResponseMessage();
            try
            {
                response = SendWebRequest(request, httpRequestOptions);
            }
            catch (WebException ex)
            {
                if (ex.Response == null)
                {
                    if (httpRequestOptions.InstantFailover)
                    {
                        return RetryOperation(request, httpRequestOptions);
                    }

                    throw;
                }
            }
            catch (TaskCanceledException e)
            {
                if (e.CancellationToken == httpClientToken)
                {
                    httpClientToken = new CancellationToken(false);
                    throw;
                }

                if (httpRequestOptions.InstantFailover)
                {
                    return RetryOperation(request, httpRequestOptions);
                }

                throw;
            }
            catch (AggregateException ae)
            {
                var innerException = GetInnerException(ae);
                var taskException = innerException as TaskCanceledException;
                if (taskException != null)
                {
                    if (taskException.CancellationToken == httpClientToken)
                    {
                        httpClientToken = new CancellationToken(false);
                        throw taskException;
                    }
                }

                if (httpRequestOptions.InstantFailover)
                {
                    return RetryOperation(request, httpRequestOptions);
                }

                throw innerException;
            }

            if ((int)response.StatusCode < MaxValidStatusCodeForRetryOperation)
            {
                return response;
            }

            if (httpRequestOptions.InstantFailover)
            {
                response.Dispose();
                return RetryOperation(request, httpRequestOptions);
            }

            return response;
        }


        private async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, HttpRequestOptions httpRequestOptions)
        {
            var response = new HttpResponseMessage();
            try
            {
                return await SendWebRequestAsync(request, httpRequestOptions);
            }
            catch (WebException ex)
            {
                if (ex.Response == null)
                {
                    if (httpRequestOptions.InstantFailover)
                    {
                        return await RetryOperationAsync(request, httpRequestOptions);
                    }

                    throw;
                }
            }

            catch (TaskCanceledException e)
            {
                if (e.CancellationToken == httpClientToken)
                {
                    httpClientToken = new CancellationToken(false);
                    throw;
                }

                if (httpRequestOptions.InstantFailover)
                {
                    return await RetryOperationAsync(request, httpRequestOptions);
                }

                throw;
            }

            catch (AggregateException ae)
            {
                var innerException = GetInnerException(ae);
                var taskException = innerException as TaskCanceledException;
                if (taskException != null)
                {
                    if (taskException.CancellationToken == httpClientToken)
                    {
                        httpClientToken = new CancellationToken(false);
                        throw innerException;
                    }

                    if (httpRequestOptions.InstantFailover)
                    {
                        return await RetryOperationAsync(request, httpRequestOptions);
                    }
                }

                throw innerException;
            }

            if ((int)response.StatusCode < MaxValidStatusCodeForRetryOperation)
            {
                return response;
            }

            if (httpRequestOptions.InstantFailover)
            {
                response.Dispose();
                return await RetryOperationAsync(request, httpRequestOptions);
            }

            return response;
        }

        private async Task<HttpResponseMessage> SendWebRequestAsync(HttpRequestMessage request, HttpRequestOptions httpRequestOptions)
        {
            var handler = new HttpClientHandler();
            EnsureNormalOperation(handler);
            using (var client = new HttpClient(handler))
            {
                client.Timeout = TimeSpan.FromMilliseconds(httpRequestOptions.HttpTimeoutMs);
                return await client.SendAsync(request, httpClientToken);
            }
        }


        private void EnsureNormalOperation(HttpClientHandler handler)
        {
            handler.ServerCertificateCustomValidationCallback = UntrustedCertHandler;
        }

        private bool UntrustedCertHandler(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors error)
        {
            // Ignore errors
            return true;
        }

        private HttpResponseMessage SendWebRequest(HttpRequestMessage request, HttpRequestOptions httpRequestOptions)
        {
            return SendWebRequestAsync(request, httpRequestOptions).Result;
        }


        private HttpResponseMessage RetryOperation(
    HttpRequestMessage request,
    HttpRequestOptions httpRequestOptions)
        {
            var retryCount = 0;
            var retryOperationList = new List<RetryOperationContainer>();

            while (retryCount < httpRequestOptions.MaxFailedConnections)
            {
                var retryOperation = new RetryOperationContainer
                {
                    RetryOperationNumber = retryCount
                };
                retryOperationList.Add(retryOperation);

                try
                {
                    retryOperation.OperationResponse = SendWebRequest(request, httpRequestOptions);
                }

                catch (TaskCanceledException ex)
                {
                    if (ex.CancellationToken == httpClientToken)
                    {
                        httpClientToken = new CancellationToken(false);
                        throw;
                    }

                    retryOperation.Exception = ex;

                    ++retryCount;

                    continue;
                }

                catch (AggregateException ae)
                {
                    retryOperation.Exception = GetInnerException(ae);
                    var taskException = retryOperation.Exception as TaskCanceledException;
                    if (taskException != null)
                    {
                        if (taskException.CancellationToken == httpClientToken)
                        {
                            httpClientToken = new CancellationToken(false);
                            throw retryOperation.Exception;
                        }

                        ++retryCount;

                        continue;
                    }
                }

                catch (WebException ex)
                {
                    retryOperation.Exception = ex;

                    if (ex.Response == null)
                    {

                        ++retryCount;

                        continue;
                    }
                }

                if (retryOperation.OperationResponse != null && (int)retryOperation.OperationResponse.StatusCode < MaxValidStatusCodeForRetryOperation)
                {
                    return retryOperation.OperationResponse;
                }

                var code = retryOperation.OperationResponse?.StatusCode ?? 0;

                ++retryCount;
            }

            var lastRetryOperation = retryOperationList.Last();
            retryOperationList.Remove(lastRetryOperation);
            retryOperationList.ForEach(x => x.OperationResponse?.Dispose());
            if (lastRetryOperation.OperationResponse != null)
            {
                return lastRetryOperation.OperationResponse;
            }

            throw new ServerNotResponding(lastRetryOperation.Exception);
        }


        private async Task<HttpResponseMessage> RetryOperationAsync(HttpRequestMessage request, HttpRequestOptions httpRequestOptions)
        {
            var retryCount = 0;
            var retryOperationList = new List<RetryOperationContainer>();

            while (retryCount <= httpRequestOptions.MaxFailedConnections)
            {
                var retryOperation = new RetryOperationContainer
                {
                    RetryOperationNumber = retryCount
                };
                retryOperationList.Add(retryOperation);

                try
                {
                    retryOperation.OperationResponse = await SendWebRequestAsync(request, httpRequestOptions);
                }
                catch (TaskCanceledException ex)
                {
                    if (ex.CancellationToken == httpClientToken)
                    {
                        httpClientToken = new CancellationToken(false);
                        throw;
                    }

                    retryOperation.Exception = ex;


                    ++retryCount;

                    continue;
                }

                catch (AggregateException ae)
                {
                    retryOperation.Exception = GetInnerException(ae);
                    var taskException = ae.InnerException as TaskCanceledException;
                    if (taskException != null)
                    {
                        if (taskException.CancellationToken == httpClientToken)
                        {
                            httpClientToken = new CancellationToken(false);
                            throw retryOperation.Exception;
                        }

                        ++retryCount;

                        continue;
                    }
                }

                if (retryOperation.OperationResponse != null && (int)retryOperation.OperationResponse.StatusCode < MaxValidStatusCodeForRetryOperation)
                {
                    return retryOperation.OperationResponse;
                }

                var code = retryOperation.OperationResponse?.StatusCode ?? 0;

                ++retryCount;
            }

            var lastRetryOperation = retryOperationList.Last();
            retryOperationList.Remove(lastRetryOperation);
            retryOperationList.ForEach(x => x.OperationResponse?.Dispose());
            if (lastRetryOperation.OperationResponse != null)
            {
                return lastRetryOperation.OperationResponse;
            }

            throw new ServerNotResponding(lastRetryOperation.Exception);
        }

        private void CheckResponse(HttpResponseMessage response)
        {
            if (response == null)
            {
                throw new ServerNotResponding();
            }

            try
            {
                if (response.StatusCode == HttpStatusCode.BadRequest)
                {
                    throw new BadParametersException("Invalid parameters in http request");
                }

                if(response.StatusCode == HttpStatusCode.NotFound)
                {
                    throw new EntityNotFoundException("Invalid address");
                }

                if (response.StatusCode != HttpStatusCode.OK)
                {
                    throw new InvalidOperationException("Invalid HTTP status code : " + (int)response.StatusCode);
                }
            }
            finally
            {
                response.Dispose();
            }
        }

        private Exception GetInnerException(Exception aggregateException)
        {
            var result = aggregateException;

            while (result.InnerException != null)
            {
                result = result.InnerException;
            }

            return result;
        }

        private T ReadFromResponse<T>(HttpResponseMessage response)
        {
            var responseStream = response.Content.ReadAsStreamAsync().Result;
            if (responseStream == null)
            {
                throw new HttpStatusCodeException(HttpStatusCode.NoContent);
            }

            using (var streamReader = new StreamReader(responseStream))
            {
                using (var jsonReader = new JsonTextReader(streamReader))
                {
                    return jsonSerializer.Deserialize<T>(jsonReader);
                }
            }
        }

        private T ParseResponse<T>(HttpResponseMessage response)
        {
            if (response == null)
            {
                throw new ServerNotResponding();
            }

            try
            {
                switch (response.StatusCode)
                {
                    case HttpStatusCode.OK:
                        return ReadFromResponse<T>(response);
                    case HttpStatusCode.NotFound:
                        throw new EntityNotFoundException("Entity not found");
                    case HttpStatusCode.InternalServerError:
                        throw new InternalErrorException("Internal Server Error");
                    case HttpStatusCode.BadRequest:
                        throw new BadParametersException("Invalid parameters in http request");
                    default:
                        throw new InvalidOperationException("Invalid HTTP status code : " + (int)response.StatusCode);
                }
            }
            finally
            {
                response.Dispose();
            }
        }

    }
}
