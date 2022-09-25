using System.Collections.Generic;
using Stoolball.Statistics;

namespace Stoolball.Data.SqlServer
{
    public interface IStatisticsQueryBuilder
    {
        /// <summary> 
        /// Adds standard filters to the WHERE clause
        /// </summary> 
        /// <returns>A SQL string starting with AND, with matching parameters, or an empty string</returns>
        (string where, Dictionary<string, object> parameters) BuildWhereClause(StatisticsFilter filter);
    }
}