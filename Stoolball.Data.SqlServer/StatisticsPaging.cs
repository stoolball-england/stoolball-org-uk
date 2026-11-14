using System.Collections.Generic;
using System.Linq;
using Stoolball.Navigation;
using Stoolball.Statistics;

namespace Stoolball.Data.SqlServer
{
    /// <summary>
    /// Pages ranked statistics queries. From the second page onwards it also selects the result immediately before the page,
    /// so that <see cref="StatisticsResults{T}.PreviousResult"/> can show whether the first result on the page is equal to it.
    /// </summary>
    internal static class StatisticsPaging
    {
        /// <summary>
        /// Gets an <c>OFFSET ... FETCH</c> clause for the page, and adds the parameters it uses.
        /// </summary>
        internal static string OffsetClause(Paging paging, Dictionary<string, object> parameters)
        {
            var includesPreviousResult = IncludesPreviousResult(paging);
            parameters.Add("@PageOffset", includesPreviousResult ? paging.PageOffset - 1 : paging.PageOffset);
            parameters.Add("@PageSize", includesPreviousResult ? (long)paging.PageSize + 1 : paging.PageSize);
            return "OFFSET @PageOffset ROWS FETCH NEXT @PageSize ROWS ONLY";
        }

        /// <summary>
        /// Splits the rows selected using <see cref="OffsetClause"/> into the results on the page and the result before it.
        /// </summary>
        /// <param name="rows">The rows returned by the query.</param>
        /// <param name="paging">The paging passed to <see cref="OffsetClause"/>, or <c>null</c> if the query was not paged.</param>
        internal static StatisticsResults<T> ToStatisticsResults<T>(IEnumerable<StatisticsResult<T>> rows, Paging? paging)
        {
            var results = rows.ToList();
            if (paging is not null && IncludesPreviousResult(paging) && results.Count > 0)
            {
                return new StatisticsResults<T>(results.Skip(1), results[0]);
            }
            return new StatisticsResults<T>(results);
        }

        private static bool IncludesPreviousResult(Paging paging) => paging.PageOffset > 0;
    }
}
