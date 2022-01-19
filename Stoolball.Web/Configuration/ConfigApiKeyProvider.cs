using Microsoft.Extensions.Configuration;

namespace Stoolball.Web.Configuration
{
    public class ConfigApiKeyProvider : IApiKeyProvider
    {
        private readonly IConfiguration _config;

        public ConfigApiKeyProvider(IConfiguration config)
        {
            _config = config ?? throw new System.ArgumentNullException(nameof(config));
        }

        public string GetApiKey(string apiKeyName)
        {
            if (string.IsNullOrEmpty(apiKeyName))
            {
                throw new System.ArgumentException("message", nameof(apiKeyName));
            }

            return _config.GetValue<string>($"Stoolball:{apiKeyName}ApiKey");
        }
    }
}