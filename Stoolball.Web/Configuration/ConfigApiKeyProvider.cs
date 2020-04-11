using System.Configuration;

namespace Stoolball.Web.Configuration
{
    public class ConfigApiKeyProvider : IApiKeyProvider
    {
        public string GetApiKey(string apiKeyName)
        {
            if (string.IsNullOrEmpty(apiKeyName))
            {
                throw new System.ArgumentException("message", nameof(apiKeyName));
            }

            return ConfigurationManager.AppSettings[$"Stoolball.{apiKeyName}ApiKey"];
        }
    }
}