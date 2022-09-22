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
        /// <param name="sql"></param>
        /// <param name="commandType"></param>
        /// <param name="connection"></param>
        /// <returns></returns>
        Task<IEnumerable<T>> QueryAsync<T>(string sql, CommandType commandType, IDbConnection connection);

        /// <summary>
        /// Execute a query asynchronously using Task
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sql"></param>
        /// <param name="param"></param>
        /// <param name="transaction"></param>
        /// <returns></returns>
        Task<IEnumerable<T>> QueryAsync<T>(string sql, object param, IDbTransaction transaction);

        /// <summary>
        /// Execute a command asynchronously using Task
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sql"></param>
        /// <param name="param"></param>
        /// <param name="transaction"></param>
        /// <returns>The number of rows affected</returns>
        Task<int> ExecuteAsync(string sql, object param, IDbTransaction transaction);
    }
}