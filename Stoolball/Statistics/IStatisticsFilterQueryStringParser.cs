namespace Stoolball.Statistics
{
    public interface IStatisticsFilterQueryStringParser
    {
        /// <summary>
        /// Parses filter criteria based on query string parameters
        /// </summary>
        /// <param name="queryString">The querystring from which to take filter parameters</param>
        /// <returns>A new filter with the querystring values</returns>
        StatisticsFilter ParseQueryString(string? queryString);
    }
}