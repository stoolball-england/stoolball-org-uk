using System;
using Microsoft.Data.SqlClient;

namespace Stoolball.Data.SqlServer
{
    /// <summary>
    /// Retries opening a connection when Azure SQL reports a transient error, such as during a failover or reconfiguration.
    /// </summary>
    /// <remarks>
    /// This applies only to opening the connection, never to executing a command, so a write cannot be run twice.
    /// </remarks>
    public static class SqlServerConnectionRetryLogic
    {
        /// <summary>
        /// SQL errors that are worth retrying when opening a connection.
        /// </summary>
        /// <remarks>
        /// 18456 (login failed) is included because Azure SQL returns it briefly during failovers.
        /// -2 (timeout) is deliberately excluded, because retrying when the database is overloaded makes it worse.
        /// See https://learn.microsoft.com/en-us/azure/azure-sql/database/troubleshoot-common-errors-issues
        /// </remarks>
        public static readonly int[] TransientErrors = [
            18456, // Login failed
            4060, // Cannot open database requested by the login
            4221, // Login to read-secondary failed due to long wait on HADR_DATABASE_WAIT_FOR_TRANSITION_TO_VERSIONING
            40197, // The service has encountered an error processing your request
            40501, // The service is currently busy
            40613, // Database is not currently available
            49918, 49919, 49920, // Cannot process request, too many operations in progress
            10928, 10929, // Resource limit reached
            233, 64, 10053, 10054, 10060 // Network errors while connecting
        ];

        /// <summary>
        /// Creates a retry provider to assign to <see cref="SqlConnection.RetryLogicProvider"/>.
        /// </summary>
        public static SqlRetryLogicBaseProvider CreateProvider()
        {
            return SqlConfigurableRetryFactory.CreateExponentialRetryProvider(new SqlRetryLogicOption
            {
                NumberOfTries = 3,
                DeltaTime = TimeSpan.FromSeconds(1),
                MaxTimeInterval = TimeSpan.FromSeconds(10),
                TransientErrors = TransientErrors
            });
        }
    }
}
