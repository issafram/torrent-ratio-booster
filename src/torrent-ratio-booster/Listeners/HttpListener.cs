using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using TorrentRatioBooster.Services;

namespace TorrentRatioBooster.Listeners
{
    internal class HttpListener : IListener
    {
        private readonly ILogger<HttpListener> logger;
        private readonly IConfiguration configuration;
        private readonly IRequestService requestService;

        public HttpListener(ILogger<HttpListener> logger, IConfiguration configuration, IRequestService requestService)
        {
            this.logger = logger;
            this.configuration = configuration;
            this.requestService = requestService;
        }

        [RequiresUnreferencedCode("Calls Microsoft.Extensions.Configuration.ConfigurationBinder.GetValue<T>(String)")]
        public async Task ListenAsync()
        {
            var port = this.configuration.GetValue<int?>("port");
            if (port == null)
            {
                throw new Exception("Port is not configured.");
            }

            var runningInContainer = this.configuration.GetValue<bool?>("DOTNET_RUNNING_IN_CONTAINER");
            var onlyAcceptAnnounceRequests = true;
            //ServicePointManager.ServerCertificateValidationCallback += (o, c, ch, er) => true;
            var httpListener = new System.Net.HttpListener();
            if (runningInContainer.HasValue && runningInContainer.Value)
            {
                httpListener.Prefixes.Add($"http://+:{port}/");
            }
            else
            {
                httpListener.Prefixes.Add($"http://localhost:{port}/");
                httpListener.Prefixes.Add($"http://127.0.0.1:{port}/");
            }

            httpListener.Start();
            this.logger.LogInformation($"Listening on port {port}...");
            while (true)
            {
                try
                {
                    var context = await httpListener.GetContextAsync();
                    this.logger.LogInformation($"Request received: {context.Request.RawUrl}");
                    var request = context.Request;
                    if (!request.HttpMethod.Equals("GET", StringComparison.OrdinalIgnoreCase))
                    {
                        this.logger.LogDebug($"Skipping request: {request.RawUrl} due to {request.HttpMethod}");
                        context.Response.StatusCode = 405; // Method Not Allowed
                        context.Response.Close();
                        return;
                    }

                    if (request.Url == null)
                    {
                        context.Response.StatusCode = 400; // Bad Request
                        context.Response.Close();
                        return;
                    }

                    if (onlyAcceptAnnounceRequests == true &&
                        (
                        !request.Url.OriginalString.Contains("announce", StringComparison.OrdinalIgnoreCase) ||
                        request.Url.Segments == null ||
                        request.Url.Segments.Length == 0 ||
                        request.Url.Segments.Count(x => x.Equals("announce", StringComparison.OrdinalIgnoreCase)) == 0))
                    {
                        this.logger.LogDebug($"Skipping request: {request.RawUrl}");
                        context.Response.StatusCode = 404; // Not Found
                        context.Response.Close();
                        return;
                    }

                    var headers = new Dictionary<string, string>();
                    if (request.Headers != null && request.Headers.Count > 0)
                    {
                        foreach (var key in request.Headers.AllKeys)
                        {
                            if (key == null || key.Length == 0)
                            {
                                continue;
                            }
                            headers.Add(key, request.Headers[key]);
                        }
                    }

                    var proxiedResponse = await this.requestService.MakeModifiedRequestAsync(headers, request.RawUrl);
                    proxiedResponse.Headers.ToList().ForEach(x =>
                    {
                        this.logger.LogDebug($"Response Header: {x.Key} - {x.Value.FirstOrDefault()}");
                        context.Response.Headers.Add(x.Key, x.Value.FirstOrDefault());
                    });

                    Console.WriteLine();
                    var acceptedHeaders = new List<string> { "Content-Type", "Content-Length", "Content-Encoding" };
                    proxiedResponse.Content.Headers.ToList().ForEach(x =>
                    {
                        if (x.Value == null)
                        {
                            return;
                        }

                        if (!acceptedHeaders.Contains(x.Key))
                        {
                            this.logger.LogTrace($"SKIPPING - Content header: {x.Key} - {x.Value.FirstOrDefault()}");
                            return;
                        }

                        this.logger.LogDebug($"Content header: {x.Key} - {x.Value.FirstOrDefault()}");
                        context.Response.Headers.Add(x.Key, x.Value.FirstOrDefault());
                    });

                    //context.Response.ContentType =  //"text/html";
                    var memoryStream = new MemoryStream();
                    memoryStream.Write(await proxiedResponse.Content.ReadAsByteArrayAsync(), 0, (int)proxiedResponse.Content.Headers.ContentLength);
                    //context.Response.OutputStream.ReadAsync()
                    var output = memoryStream.ToArray();
                    context.Response.ContentLength64 = memoryStream.Length;

                    //var dictionary = BencodeUtility.DecodeDictionary(output);
                    //foreach (var keyValuePair in dictionary)
                    //{
                    //    Console.WriteLine($"Key: {keyValuePair.Key} - Value: {keyValuePair.Value}");
                    //}

                    var stringRepresentation = Encoding.UTF8.GetString(output);
                    this.logger.LogTrace($"Response content: {stringRepresentation}");
                    //File.WriteAllText($"{DateTime.Now.ToString("yy-MM-dd-HH-mm-ss")}.txt", stringRepresentation);
                    context.Response.StatusCode = (int)proxiedResponse.StatusCode;


                    await context.Response.OutputStream.WriteAsync(memoryStream.ToArray(), 0, (int)memoryStream.Length);//(int)proxiedResponse.Content.Headers.ContentLength);
                                                                                                                        //await proxiedResponse.Content.CopyToAsync(context.Response.OutputStream);

                    //context.Response.OutputStream.Write(await proxiedResponse.Content.ReadAsByteArrayAsync(), 0, (int)proxiedResponse.Content.Headers.ContentLength);
                    //context.Response.OutputStream.Close();
                    context.Response.Close();

                    //var proxiedResponse = await MakeModifiedRequestAsync(headers, request.RawUrl);
                    //context.Response.StatusCode = (int)proxiedResponse.StatusCode;
                    //proxiedResponse.Headers.ToList().ForEach(x => context.Response.Headers.Add(x.Key, x.Value.FirstOrDefault()));
                    //context.Response.OutputStream.Write(await proxiedResponse.Content.ReadAsByteArrayAsync(), 0, (int)proxiedResponse.Content.Headers.ContentLength);
                    //context.Response.OutputStream.Close();
                    //context.Response.Close();
                }
                catch (Exception e)
                {
                    this.logger.LogError(e, "Error sending request to tracker");
                }
            }
        }
    }
}