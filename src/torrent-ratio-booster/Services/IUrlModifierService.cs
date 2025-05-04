namespace TorrentRatioBooster.Services
{
    internal interface IUrlModifierService
    {
        string GetModifiedUrl(string originalUrl, double ratio);
    }
}