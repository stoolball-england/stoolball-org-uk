using System.Data;

namespace Stoolball.Data.SqlServer
{
    public interface IDatabaseConnectionFactory
    {
        IDbConnection CreateDatabaseConnection();
    }
}