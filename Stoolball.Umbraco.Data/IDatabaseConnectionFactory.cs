using System.Data;

namespace Stoolball.Umbraco.Data
{
    public interface IDatabaseConnectionFactory
    {
        IDbConnection CreateDatabaseConnection();
    }
}