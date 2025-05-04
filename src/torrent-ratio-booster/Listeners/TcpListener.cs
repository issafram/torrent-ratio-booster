using System.Net;
using System.Text;
using TorrentRatioBooster.Services;

namespace TorrentRatioBooster.Listeners
{
    internal class TcpListener
    {
        private int port;
        private readonly IRequestService requestService;

        public TcpListener(int port, IRequestService requestService)
        {
            this.port = port;
            this.requestService = requestService;
        }

        public async Task StartTcpListnerAsync()
        {
            using var tcpListener = new System.Net.Sockets.TcpListener(IPAddress.Any, this.port);
            tcpListener.Start();
            while (true)
            {
                try
                {
                    using var tcpClient = await tcpListener.AcceptTcpClientAsync();
                    var stream = tcpClient.GetStream();
                    var buffer = new byte[1024];
                    int bytesRead;
                    var output = new List<string>();
                    while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) != 0)
                    {
                        var data = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        Console.WriteLine(data);
                        output.Add(data);
                        //var response = "Hello from server!";
                        //var responseData = Encoding.UTF8.GetBytes(response);
                        //stream.Write(responseData, 0, responseData.Length);
                    }
                    Console.WriteLine("READ ALL THE DATA.");
                    var url = output.FirstOrDefault(x => x.StartsWith("GET")).Replace("GET ", "");
                    var hostHeader = output.FirstOrDefault(x => x.StartsWith("Host:")).Replace("Host: ", "");
                    var userAgentHeader = output.FirstOrDefault(x => x.StartsWith("User-Agent:")).Replace("User-Agent: ", "");
                    var acceptEncodingHeader = output.FirstOrDefault(x => x.StartsWith("Accept-Encoding:")).Replace("Accept-Encoding: ", "");
                    var connectionHeader = output.FirstOrDefault(x => x.StartsWith("Connection:")).Replace("Connection: ", "");

                    var headers = new Dictionary<string, string>();
                    headers.Add("Host", hostHeader);
                    headers.Add("User-Agent", userAgentHeader);
                    headers.Add("Accept-Encoding", acceptEncodingHeader);
                    headers.Add("Connection", connectionHeader);
                    await this.requestService.MakeModifiedRequestAsync(headers, url);
                    //await this.requestService.MakeModifiedRequestOLDAsync(headers, url);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
            }
        }
    }
}