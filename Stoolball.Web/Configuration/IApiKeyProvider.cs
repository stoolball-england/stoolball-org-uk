namespace Stoolball.Web.Configuration
{
    public interface IApiKeyProvider
    {
        string GetApiKey(string apiKeyName);
    }
}