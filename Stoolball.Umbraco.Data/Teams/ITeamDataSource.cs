﻿using Stoolball.Teams;
using System.Threading.Tasks;

namespace Stoolball.Umbraco.Data.Teams
{
    /// <summary>
    /// Get stoolball team data from a data source
    /// </summary>
    public interface ITeamDataSource
    {
        /// <summary>
        /// Gets a single team based on its route
        /// </summary>
        /// <param name="route">/teams/example-team</param>
        /// <returns>A matching <see cref="Team"/> or <c>null</c> if not found</returns>
        Task<Team> ReadTeamByRoute(string route);
    }
}