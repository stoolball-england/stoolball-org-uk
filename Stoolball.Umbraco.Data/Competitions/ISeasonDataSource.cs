using Stoolball.Competitions;
using System.Threading.Tasks;

namespace Stoolball.Umbraco.Data.Competitions
{
    /// <summary>
    /// Get stoolball season data from a data source
    /// </summary>
    public interface ISeasonDataSource
    {
        /// <summary>
        /// Gets a single stoolball competition based on its route
        /// </summary>
        /// <param name="route">/competitions/example-competition</param>
        /// <returns>A matching <see cref="Competition"/> or <c>null</c> if not found</returns>
        Task<Competition> ReadCompetitionByRoute(string route);

        /// <summary>
        /// Gets a single stoolball season based on its route
        /// </summary>
        /// <param name="route">/competitions/example-competition/2020</param>
        /// <param name="includeRelated"><c>true</c> to include the teams and other seasons in the competition; <c>false</c> otherwise</param>
        /// <returns>A matching <see cref="Season"/> or <c>null</c> if not found</returns>
        Task<Season> ReadSeasonByRoute(string route, bool includeRelated = false);
    }
}