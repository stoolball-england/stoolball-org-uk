using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace Stoolball.Data.SqlServer
{
    /// <summary>
    /// Wrap calls to Dapper so that they can be mocked in unit tests
    /// </summary>
    public interface IDapperWrapper
    {
        /// <summary>
        /// Execute a query asynchronously using Task
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="connection"></param>
        /// <param name="sql"></param>
        /// <param name="commandType"></param>
        /// <returns></returns>
        Task<IEnumerable<T>> QueryAsync<T>(IDbConnection connection, string sql, CommandType commandType);
    }
}