using System;
using System.Data;
using Microsoft.Data.SqlClient;

namespace Stoolball.Data.SqlServer.IntegrationTests
{
    internal class IntegrationTestsDatabaseConnectionFactory : IDatabaseConnectionFactory
    {
        private readonly string _connectionString;

        public IntegrationTestsDatabaseConnectionFactory(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new ArgumentException($"'{nameof(connectionString)}' cannot be null or whitespace", nameof(connectionString));
            }

            _connectionString = connectionString;
        }

        public IDbConnection CreateDatabaseConnection()
        {
            return new SqlConnection(_connectionString);
        }
    }
}
