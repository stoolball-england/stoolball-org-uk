using Microsoft.AspNetCore.WebUtilities;
using Stoolball.Filtering;
using Stoolball.Teams;

namespace Stoolball.Statistics
{
    public class StatisticsFilterQueryStringParser : BaseFilterQueryStringParser, IStatisticsFilterQueryStringParser
    {
        /// <inheritdoc />
        public StatisticsFilter ParseQueryString(string? queryString)
        {
            var filter = new StatisticsFilter();

            if (!string.IsNullOrEmpty(queryString))
            {
                var query = QueryHelpers.ParseQuery(queryString);
                if (query.ContainsKey("page") && int.TryParse(query["page"], out var pageNumber))
                {
                    filter.Paging.PageNumber = pageNumber > 0 ? pageNumber : 1;
                }

                var (fromDate, untilDate) = ParseDateFilter(filter.FromDate, filter.UntilDate, query);
                filter.FromDate = fromDate;
                filter.UntilDate = untilDate;

                if (query.ContainsKey("team") && !string.IsNullOrEmpty(query["team"]))
                {
                    filter.Team = new Team { TeamRoute = "/teams/" + query["team"] };
                }
            }

            return filter;
        }
    }
}