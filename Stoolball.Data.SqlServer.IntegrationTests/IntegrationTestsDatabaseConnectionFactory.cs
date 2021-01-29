using System;
using System.Data;
using System.Data.SqlClient;

namespace Stoolball.Data.SqlServer.IntegrationTests
{
    internal class IntegrationTestsDatabaseConnectionFactory : IDatabaseConnectionFactory
    {
        private readonly string _pathToDatabase;

        public IntegrationTestsDatabaseConnectionFactory(string pathToDatabase)
        {
            if (string.IsNullOrWhiteSpace(pathToDatabase))
            {
                throw new ArgumentException($"'{nameof(pathToDatabase)}' cannot be null or whitespace", nameof(pathToDatabase));
            }

            _pathToDatabase = pathToDatabase;
        }

        public IDbConnection CreateDatabaseConnection()
        {
            //var pathToDatabase = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Umbraco\Umbraco.mdf"));
            return new SqlConnection($@"Server=(LocalDB)\MSSQLLocalDB;Integrated Security=true;AttachDbFileName={_pathToDatabase}");
        }
    }
}
