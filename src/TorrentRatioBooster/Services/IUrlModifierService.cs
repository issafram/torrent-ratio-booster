namespace TorrentRatioBooster.Services
{
    public interface IUrlModifierService
    {
        string GetModifiedUrl(string originalUrl, double ratio);
    }
}