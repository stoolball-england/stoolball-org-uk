using System.Data;
using System.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Stoolball.Data.SqlServer;

namespace Stoolball.Web
{
    public class UmbracoDatabaseConnectionFactory : IDatabaseConnectionFactory
    {
        private readonly IConfiguration _config;

        public UmbracoDatabaseConnectionFactory(IConfiguration config)
        {
            _config = config ?? throw new System.ArgumentNullException(nameof(config));
        }

        public IDbConnection CreateDatabaseConnection()
        {
            return new SqlConnection(_config.GetValue<string>($"ConnectionStrings:UmbracoDbDsn"));
        }
    }
}
