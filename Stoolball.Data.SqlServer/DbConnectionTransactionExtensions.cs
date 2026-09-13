using System.Data;
using System.Transactions;

namespace Stoolball.Data.SqlServer
{
    internal static class DbConnectionTransactionExtensions
    {
        /// <summary>
        /// Starts a local transaction, unless an ambient <see cref="TransactionScope"/> is already in progress (as in integration tests),
        /// in which case <paramref name="connection"/> enlists in that instead and this returns null. Callers must Commit()/Rollback() via
        /// the null-conditional operator, since the ambient scope controls the outcome instead.
        /// </summary>
        public static IDbTransaction? BeginTransactionIfNoAmbientTransaction(this IDbConnection connection)
        {
            return Transaction.Current == null ? connection.BeginTransaction() : null;
        }
    }
}
