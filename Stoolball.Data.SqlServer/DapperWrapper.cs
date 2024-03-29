﻿using System;
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
            return await connection.QueryAsync<T>(sql, commandType).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<TReturn>> QueryAsync<TFirst, TSecond, TReturn>(string sql, Func<TFirst, TSecond, TReturn> map, object? param, IDbTransaction transaction, bool buffered = true, string splitOn = "Id", int? commandTimeout = null, CommandType? commandType = null)
        {
            return await transaction.Connection.QueryAsync(new CommandDefinition(sql, param, transaction, commandTimeout, commandType, buffered ? CommandFlags.Buffered : CommandFlags.None), map, splitOn).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<TReturn>> QueryAsync<TFirst, TSecond, TThird, TReturn>(string sql, Func<TFirst, TSecond, TThird, TReturn> map, object? param, IDbTransaction transaction, bool buffered = true, string splitOn = "Id", int? commandTimeout = null, CommandType? commandType = null)
        {
            return await transaction.Connection.QueryAsync(sql, map, param, transaction, buffered, splitOn, commandTimeout, commandType).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<TReturn>> QueryAsync<TFirst, TSecond, TThird, TFourth, TReturn>(string sql, Func<TFirst, TSecond, TThird, TFourth, TReturn> map, object? param, IDbTransaction transaction, bool buffered = true, string splitOn = "Id", int? commandTimeout = null, CommandType? commandType = null)
        {
            return await transaction.Connection.QueryAsync(sql, map, param, transaction, buffered, splitOn, commandTimeout, commandType).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<T> QuerySingleAsync<T>(string sql, object? param, IDbTransaction transaction, int? commandTimeout = null, CommandType? commandType = null)
        {
            return await transaction.Connection.QuerySingleAsync<T>(sql, param, transaction, commandTimeout, commandType).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<T> QuerySingleOrDefaultAsync<T>(string sql, object? param, IDbTransaction transaction, int? commandTimeout = null, CommandType? commandType = null)
        {
            return await transaction.Connection.QuerySingleOrDefaultAsync<T>(sql, param, transaction, commandTimeout, commandType).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<T>> QueryAsync<T>(string sql, object? param, IDbTransaction transaction)
        {
            return await transaction.Connection.QueryAsync<T>(sql, param, transaction).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<int> ExecuteAsync(string sql, object? param, IDbTransaction transaction)
        {
            return await transaction.Connection.ExecuteAsync(sql, param, transaction).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<T> ExecuteScalarAsync<T>(string sql, object? param, IDbTransaction transaction, int? commandTimeout = null, CommandType? commandType = null)
        {
            return await transaction.Connection.ExecuteScalarAsync<T>(sql, param, transaction, commandTimeout, commandType).ConfigureAwait(false);
        }
    }
}
