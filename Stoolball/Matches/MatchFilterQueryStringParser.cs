﻿using System;
using Microsoft.AspNetCore.WebUtilities;
using Stoolball.Filtering;

namespace Stoolball.Matches
{
    public class MatchFilterQueryStringParser : BaseFilterQueryStringParser, IMatchFilterQueryStringParser
    {
        public MatchFilter ParseQueryString(MatchFilter filter, string? queryString)
        {
            if (filter == null) { throw new ArgumentNullException(nameof(filter)); }

            var updatedFilter = filter.Clone();

            if (!string.IsNullOrEmpty(queryString))
            {
                var query = QueryHelpers.ParseQuery(queryString);
                if (query.ContainsKey("q"))
                {
                    updatedFilter.Query = query["q"].ToString().Trim();
                }

                var (fromDate, untilDate) = ParseDateFilter(updatedFilter.FromDate, updatedFilter.UntilDate, query);
                updatedFilter.FromDate = fromDate;
                updatedFilter.UntilDate = untilDate;
            }

            return updatedFilter;
        }
    }
}