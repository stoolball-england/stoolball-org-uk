﻿using Stoolball.Matches;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Stoolball.Umbraco.Data.Matches
{
    /// <summary>
    /// Get stoolball match data from a data source
    /// </summary>
    public interface IMatchDataSource
    {
        /// <summary>
        /// Gets a list of matches and tournaments based on a query
        /// </summary>
        /// <returns>A list of <see cref="MatchListing"/> objects. An empty list if no matches or tournaments are found.</returns>
        Task<List<MatchListing>> ReadMatchListings(MatchQuery matchQuery);
    }
}