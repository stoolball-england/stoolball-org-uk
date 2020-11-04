using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;

namespace Stoolball.Data.SqlServer.IntegrationTests
{
    internal class IntegrationTestsDatabaseConnectionFactory : IDatabaseConnectionFactory
    {
        public IDbConnection CreateDatabaseConnection()
        {
            var pathToDatabase = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\..\Stoolball.Web\App_Data\Umbraco.mdf"));
            return new SqlConnection($@"Server=(LocalDB)\MSSQLLocalDB;Integrated Security=true;AttachDbFileName={pathToDatabase}");
        }
    }
}
