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
        public async Task<IEnumerable<T>> QueryAsync<T>(IDbConnection connection, string sql, CommandType commandType)
        {
            return await connection.QueryAsync<T>(sql, commandType);
        }
    }
}
