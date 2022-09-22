using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Dapper;

namespace Stoolball.Data.SqlServer
{
    /// <inheritdoc/>
    public class DapperWrapper : IDapperWrapper
    {
        /// <inheritdoc/>
        public async Task<IEnumerable<T>> QueryAsync<T>(string sql, CommandType commandType, IDbConnection connection)
        {
            return await connection.QueryAsync<T>(sql, commandType);
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<T>> QueryAsync<T>(string sql, object param, IDbTransaction transaction)
        {
            return await transaction.Connection.QueryAsync<T>(sql, param, transaction);
        }

        /// <inheritdoc/>
        public async Task<int> ExecuteAsync(string sql, object param, IDbTransaction transaction)
        {
            return await transaction.Connection.ExecuteAsync(sql, param, transaction);
        }

    }
}
