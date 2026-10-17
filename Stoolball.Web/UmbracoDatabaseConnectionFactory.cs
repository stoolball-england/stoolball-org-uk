using System.Data;
using System.Linq;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Stoolball.Data.SqlServer;

namespace Stoolball.Web
{
    public class UmbracoDatabaseConnectionFactory : IDatabaseConnectionFactory
    {
        private readonly IConfiguration _config;
        private readonly SqlRetryLogicBaseProvider _retryLogicProvider;

        public UmbracoDatabaseConnectionFactory(IConfiguration config, ILogger<UmbracoDatabaseConnectionFactory> logger)
        {
            _config = config ?? throw new System.ArgumentNullException(nameof(config));
            if (logger is null)
            {
                throw new System.ArgumentNullException(nameof(logger));
            }

            _retryLogicProvider = SqlServerConnectionRetryLogic.CreateProvider();
            _retryLogicProvider.Retrying += (sender, e) =>
            {
                var lastError = e.Exceptions.LastOrDefault() as SqlException;
                logger.LogWarning("Retrying opening a database connection after transient SQL error {SqlErrorNumber}. Attempt {RetryCount}, waiting {Delay}.", lastError?.Number, e.RetryCount, e.Delay);
            };
        }

        public IDbConnection CreateDatabaseConnection()
        {
            return new SqlConnection(_config.GetValue<string>($"ConnectionStrings:UmbracoDbDsn"))
            {
                RetryLogicProvider = _retryLogicProvider
            };
        }
    }
}
