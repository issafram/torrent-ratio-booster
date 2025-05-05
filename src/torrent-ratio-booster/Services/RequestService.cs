using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;
using System.Net;

namespace TorrentRatioBooster.Services
{
    internal class RequestService : IRequestService
    {
        private readonly ILogger<RequestService> logger;
        private readonly IConfiguration configuration;
        private readonly IHttpClientFactory httpClientFactory;
        private readonly IUrlModifierService urlModifierService;

        public RequestService(ILogger<RequestService> logger, IConfiguration configuration, IHttpClientFactory httpClientFactory, IUrlModifierService urlModifierService)
        {
            this.logger = logger;
            this.configuration = configuration;
            this.httpClientFactory = httpClientFactory;
            this.urlModifierService = urlModifierService;
        }

        [RequiresUnreferencedCode("Calls Microsoft.Extensions.Configuration.ConfigurationBinder.GetValue<T>(String)")]
        public async Task<HttpResponseMessage> MakeModifiedRequestAsync(Dictionary<string, string> headers, string uri)
        {
            double ratio = this.configuration.GetValue<double?>("RATIO") ?? 1.0;
            var httpClient = this.httpClientFactory.CreateClient("httpClient");
            httpClient.DefaultRequestHeaders.Clear();

            try
            {
                var modifiedUrl = this.urlModifierService.GetModifiedUrl(uri, ratio);
                var requestMessage = new HttpRequestMessage
                {
                    Method = new HttpMethod("GET"),
                    RequestUri = new Uri(modifiedUrl, false),
                    Version = new Version(1, 1)
                };
                if (headers != null && headers.Count > 0)
                {
                    foreach (var keyValuePair in headers)
                    {
                        if (keyValuePair.Key.Equals("Host", StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }
                        requestMessage.Headers.Add(keyValuePair.Key, keyValuePair.Value);
                    }
                }

                PrintEntireUriContents(requestMessage.RequestUri);
                this.logger.LogDebug($"Making request to: {requestMessage.RequestUri}");
                //var clientResponse = await httpClient.GetAsync(modifiedUrl);
                var clientResponse = await httpClient.SendAsync(requestMessage);
                //var clientResponse = await httpClient.GetAsync(request.Url);
                this.logger.LogDebug($"Response status code: {clientResponse.StatusCode}");
                //Console.WriteLine($"Response headers: {clientResponse.Headers}");
                //Console.WriteLine($"Response content: {await clientResponse.Content.ReadAsStringAsync()}");
                return clientResponse;
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, $"Error: {ex.Message}");
                throw;
            }
        }

        private void PrintEntireUriContents(Uri requestUri)
        {
            if (requestUri == null)
            {
                return;
            }
            this.logger.LogTrace($"ToString: {requestUri}");
            this.logger.LogTrace($"AbsoluteUri: {requestUri.AbsoluteUri}");
            this.logger.LogTrace($"OriginalString: {requestUri.OriginalString}");
            this.logger.LogTrace($"Scheme: {requestUri.Scheme}");
            this.logger.LogTrace($"Host: {requestUri.Host}");
            this.logger.LogTrace($"Port: {requestUri.Port}");
            this.logger.LogTrace($"Path: {requestUri.AbsolutePath}");
            this.logger.LogTrace($"Query: {requestUri.Query}");
            this.logger.LogTrace($"Fragment: {requestUri.Fragment}");
            
            this.logger.LogTrace($"DnsSafeHost: {requestUri.DnsSafeHost}");
            this.logger.LogTrace($"IsDefaultPort: {requestUri.IsDefaultPort}");
            this.logger.LogTrace($"IsFile: {requestUri.IsFile}");
            this.logger.LogTrace($"IsLoopback: {requestUri.IsLoopback}");
            this.logger.LogTrace($"IsUnc: {requestUri.IsUnc}");
            this.logger.LogTrace($"LocalPath: {requestUri.LocalPath}");
            this.logger.LogTrace($"UserEscaped: {requestUri.UserEscaped}");
            this.logger.LogTrace($"UserInfo: {requestUri.UserInfo}");
            this.logger.LogTrace($"HostNameType: {requestUri.HostNameType}");
            this.logger.LogTrace($"IdnHost: {requestUri.IdnHost}");
            this.logger.LogTrace($"Segments: {string.Join(", ", requestUri.Segments)}");
        }

        public async Task<HttpResponseMessage> MakeModifiedRequestOLDAsync(Dictionary<string, string> headers, string uri)
        {
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Clear();

            try
            {
                var requestMessage = new HttpRequestMessage
                {
                    Method = new HttpMethod("GET"),
                    RequestUri = new Uri(uri),
                    Version = new Version(1, 1)
                };
                if (headers != null && headers.Count > 0)
                {
                    foreach (var keyValuePair in headers)
                    {
                        requestMessage.Headers.Add(keyValuePair.Key, keyValuePair.Value);
                    }
                }

                this.logger.LogDebug($"Making request to: {requestMessage.RequestUri}");
                var clientResponse = await httpClient.SendAsync(requestMessage);
                //var clientResponse = await httpClient.GetAsync(request.Url);
                //Console.WriteLine($"Response status code: {clientResponse.StatusCode}");
                //Console.WriteLine($"Response headers: {clientResponse.Headers}");
                //Console.WriteLine($"Response content: {await clientResponse.Content.ReadAsStringAsync()}");
                return clientResponse;
            }
            catch (Exception ex)
            {
                this.logger.LogError($"Error: {ex.Message}");
                throw;
            }
        }
    }
}