using System;
using Microsoft.AspNetCore.WebUtilities;
using Stoolball.Filtering;
using Stoolball.Teams;

namespace Stoolball.Statistics
{
    public class StatisticsFilterQueryStringParser : BaseFilterQueryStringParser, IStatisticsFilterQueryStringParser
    {
        public StatisticsFilter ParseQueryString(StatisticsFilter filter, string? queryString)
        {
            if (filter == null) { throw new ArgumentNullException(nameof(filter)); }

            var updatedFilter = filter.Clone();

            if (!string.IsNullOrEmpty(queryString))
            {
                var query = QueryHelpers.ParseQuery(queryString);
                if (query.ContainsKey("page") && int.TryParse(query["page"], out var pageNumber))
                {
                    updatedFilter.Paging.PageNumber = pageNumber > 0 ? pageNumber : 1;
                }

                var (fromDate, untilDate) = ParseDateFilter(updatedFilter.FromDate, updatedFilter.UntilDate, query);
                updatedFilter.FromDate = fromDate;
                updatedFilter.UntilDate = untilDate;

                if (query.ContainsKey("team") && Guid.TryParse(query["team"], out var teamId))
                {
                    updatedFilter.Team = new Team { TeamId = teamId };
                }
            }

            return updatedFilter;
        }
    }
}