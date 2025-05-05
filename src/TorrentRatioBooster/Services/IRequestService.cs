
namespace TorrentRatioBooster.Services
{
    internal interface IRequestService
    {
        Task<HttpResponseMessage> MakeModifiedRequestAsync(Dictionary<string, string> headers, string uri);
    }
}