using System.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace Stoolball.Umbraco.Data
{
    public class UmbracoDatabaseConnectionFactory : IDatabaseConnectionFactory
    {
        public IDbConnection CreateDatabaseConnection()
        {
            return new SqlConnection(ConfigurationManager.ConnectionStrings["UmbracoDbDsn"]?.ConnectionString);
        }
    }
}
