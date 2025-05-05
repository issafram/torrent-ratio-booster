using Microsoft.Extensions.Logging;
using System.Text;

namespace TorrentRatioBooster.Services
{
    internal class UrlModifierService : IUrlModifierService
    {
        private readonly ILogger<UrlModifierService> logger;

        public UrlModifierService(ILogger<UrlModifierService> logger)
        {
            this.logger = logger;
        }

        public string GetModifiedUrl(string originalUrl, double ratio)
        {
            this.logger.LogTrace($"Original Url: {originalUrl}");
            var parser = new UriBuilder(originalUrl);
            var queryString = GetQueryString(originalUrl);
            this.logger.LogTrace($"Query String: {queryString}");

            var queryStringList = ParseQueryString(queryString);
            if (queryStringList.Exists(x => x.Key.Equals("event", StringComparison.OrdinalIgnoreCase) && x.Value.Equals("started", StringComparison.OrdinalIgnoreCase)))
            {
                this.logger.LogDebug($"Skipping request: {originalUrl} due to event=started");
                return originalUrl;
            }

            var downloadedValue = queryStringList.FirstOrDefault(x => x.Key.Equals("downloaded", StringComparison.OrdinalIgnoreCase)).Value;
            var uploadedValue = Convert.ToInt64(Math.Ceiling(Convert.ToInt64(downloadedValue) * ratio));

            this.logger.LogInformation($"Reporting downloaded: {downloadedValue} - Reporting uploaded: {uploadedValue}");

            var uploadIndex = queryStringList.FindIndex(x => x.Key.Equals("uploaded", StringComparison.OrdinalIgnoreCase));
            if (uploadIndex >= 0)
            {
                queryStringList[uploadIndex] = new KeyValuePair<string, string>("uploaded", uploadedValue.ToString());
            }

            queryStringList.ForEach(x => this.logger.LogDebug($"{x.Key} = {x.Value}"));

            var rebuiltUrl = RebuildUrl(parser, queryStringList);
            this.logger.LogDebug($"Rebuilt Url: {rebuiltUrl}");
            var valuesEqual = rebuiltUrl == originalUrl;

            return rebuiltUrl;
        }

        private static string GetQueryString(string url)
        {
            return url.Replace("http://", string.Empty)
                .Replace("https://", string.Empty)
                .Split('?')[1];
        }

        private static string RebuildUrl(UriBuilder parser, List<KeyValuePair<string, string>> collection)
        {
            var sb = new StringBuilder();
            sb.Append(parser.Scheme);
            sb.Append(Uri.SchemeDelimiter);
            sb.Append(parser.Host);
            sb.Append(':');
            sb.Append(parser.Port);
            sb.Append(parser.Path);
            sb.Append('?');
            sb.Append(string.Join("&", collection.Select(x => $"{x.Key}={x.Value}")));

            return sb.ToString();
        }

        static List<KeyValuePair<string, string>> ParseQueryString(string queryString)
        {
            var keyValuePairs = new List<KeyValuePair<string, string>>();
            var querySegments = queryString.Split('&');
            foreach (var segment in querySegments)
            {
                string[] parts = segment.Split('=');
                if (parts.Length == 0)
                {
                    continue;
                }

                var key = parts[0].Trim(new char[] { '?', ' ' });
                var val = parts[1].Trim();
                keyValuePairs.Add(new KeyValuePair<string, string>(key, val));
            }

            return keyValuePairs;
        }
    }
}