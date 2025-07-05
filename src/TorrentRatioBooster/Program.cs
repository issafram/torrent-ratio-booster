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

            var dotNetRunningInContainerValue = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER");
            var runningInContainer = !string.IsNullOrEmpty(dotNetRunningInContainerValue) && dotNetRunningInContainerValue.Equals("true", StringComparison.OrdinalIgnoreCase);

            IConfigurationBuilder configurationBuilder = new ConfigurationBuilder();
            configurationBuilder = configurationBuilder.AddEnvironmentVariables();
            if (runningInContainer == false)
            {
                configurationBuilder = configurationBuilder.AddCommandLine(args);
            }
            var configuration = configurationBuilder.Build();

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

            serviceCollection.AddHttpClient(Constants.HttpClientName).ConfigurePrimaryHttpMessageHandler(() =>
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