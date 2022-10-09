namespace Stoolball.Matches
{
    public interface IMatchFilterQueryStringParser
    {
        /// <summary>
        /// Updates a filter based on query string parameters
        /// </summary>
        /// <param name="filter">An existing filter to be updated</param>
        /// <param name="queryString">The querystring from which to take filter parameters</param>
        /// <returns>A new filter combining the original filter with the querystring values</returns>
        MatchFilter ParseQueryString(MatchFilter filter, string? queryString);
    }
}