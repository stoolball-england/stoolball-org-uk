using System;
using System.Threading.Tasks;

namespace Stoolball.Competitions
{
    public interface ISeasonRepository
    {
        /// <summary>
        /// Creates a stoolball season and populates the <see cref="Season.SeasonId"/>
        /// </summary>
        /// <returns>The created season</returns>
        Task<Season> CreateSeason(Season season, Guid memberKey, string memberName);

        /// <summary>
        /// Updates a stoolball season
        /// </summary>
        Task<Season> UpdateSeason(Season season, Guid memberKey, string memberName);

        /// <summary>
        /// Updates league points settings for a stoolball season
        /// </summary>
        Task<Season> UpdateResultsTable(Season season, Guid memberKey, string memberName);

        /// <summary>
        /// Updates the teams in a stoolball season
        /// </summary>
        Task<Season> UpdateTeams(Season season, Guid memberKey, string memberName);

        /// <summary>
        /// Delete a stoolball season
        /// </summary>
        Task DeleteSeason(Season season, Guid memberKey, string memberName);
    }
}