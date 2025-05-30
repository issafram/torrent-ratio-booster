using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using System.Net;
using TorrentRatioBooster.Listeners;
using TorrentRatioBooster.Services;

namespace TorrentRatioBooster
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Copyright (c) 2025 Issa Fram");
            Console.WriteLine("Starting TorrentRatioBooster...");

            var configuration = new ConfigurationBuilder()
                .AddEnvironmentVariables()
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .Build();

            var serviceCollection = new ServiceCollection()
                .AddTransient<IUrlModifierService, UrlModifierService>()
                .AddTransient<IRequestService, RequestService>()
                .AddSingleton<IListener, Listeners.HttpListener>()
                .AddScoped<IConfiguration>(x => configuration)
                .AddLogging(x=>
                {
                    x.ClearProviders();
                    x.SetMinimumLevel(LogLevel.Trace);
                    x.AddNLog();
                });

            serviceCollection.AddHttpClient("httpClient").ConfigurePrimaryHttpMessageHandler(() =>
            {
                return new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true,
                    AllowAutoRedirect = true,
                    AutomaticDecompression = DecompressionMethods.None
                };
            });
            var serviceProvider = serviceCollection.BuildServiceProvider();

            var listener = serviceProvider.GetService<IListener>();
            if (listener == null)
            {
                throw new Exception("Unable to resolve listener");
            }

            await listener.ListenAsync();
        }
    }
}