using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Stoolball.Competitions
{
    /// <summary>
    /// Get stoolball season data from a data source
    /// </summary>
    public interface ISeasonDataSource
    {
        /// <summary>
        /// Gets a list of seasons based on a query
        /// </summary>
        /// <returns>A list of <see cref="Season"/> objects. An empty list if no seasons are found.</returns>
        Task<List<Season>> ReadSeasons(CompetitionFilter competitionQuery);

        /// <summary>
        /// Gets a single stoolball season based on its id
        /// </summary>
        /// <param name="includeRelated"><c>true</c> to include the teams and other seasons in the competition; <c>false</c> otherwise</param>
        /// <returns>A matching <see cref="Season"/> or <c>null</c> if not found</returns>
        Task<Season?> ReadSeasonById(Guid seasonId, bool includeRelated = false);

        /// <summary>
        /// Gets a single stoolball season based on its route
        /// </summary>
        /// <param name="route">/competitions/example-competition/2020</param>
        /// <param name="includeRelated"><c>true</c> to include the teams and other seasons in the competition; <c>false</c> otherwise</param>
        /// <returns>A matching <see cref="Season"/> or <c>null</c> if not found</returns>
        Task<Season?> ReadSeasonByRoute(string route, bool includeRelated = false);

        /// <summary>
        /// Reads the points rules that apply for a specific season
        /// </summary>
        Task<IEnumerable<PointsRule>> ReadPointsRules(Guid seasonId);

        /// <summary>
        /// Reads the points adjustments that apply for a specific season
        /// </summary>
        Task<IEnumerable<PointsAdjustment>> ReadPointsAdjustments(Guid seasonId);
    }
}