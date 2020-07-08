using Stoolball.Competitions;
using System;
using System.Threading.Tasks;

namespace Stoolball.Umbraco.Data.Competitions
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
        /// Updates results settings for a stoolball season
        /// </summary>
        Task<Season> UpdateSeasonResults(Season season, Guid memberKey, string memberName);

        /// <summary>
        /// Delete a stoolball season
        /// </summary>
        Task DeleteSeason(Season season, Guid memberKey, string memberName);
    }
}