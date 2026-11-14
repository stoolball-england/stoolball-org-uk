using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Stoolball.Statistics
{
    /// <summary>
    /// A page of ranked statistics results, plus the result immediately before the page if there is one.
    /// </summary>
    /// <remarks>
    /// Enumerating this returns only the results on the page. <see cref="PreviousResult"/> is there so that the first result on
    /// the page can be shown as equal to the last result on the page before.
    /// </remarks>
    public class StatisticsResults<T> : IEnumerable<StatisticsResult<T>>
    {
        public StatisticsResults(IEnumerable<StatisticsResult<T>> results, StatisticsResult<T>? previousResult = null)
        {
            Results = results?.ToList() ?? throw new ArgumentNullException(nameof(results));
            PreviousResult = previousResult;
        }

        /// <summary>
        /// Gets the results on the page.
        /// </summary>
        public IReadOnlyList<StatisticsResult<T>> Results { get; }

        /// <summary>
        /// Gets the result immediately before the first result on the page, or <c>null</c> if the page is the first page or is not paged.
        /// </summary>
        public StatisticsResult<T>? PreviousResult { get; }

        public IEnumerator<StatisticsResult<T>> GetEnumerator() => Results.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
