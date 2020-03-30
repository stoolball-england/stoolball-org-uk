using Stoolball.Clubs;
using System.Threading.Tasks;

namespace Stoolball.Umbraco.Data.Clubs
{
    /// <summary>
    /// Get stoolball club data from a data source
    /// </summary>
    public interface IClubDataSource
    {
        /// <summary>
        /// Gets a single stoolball club based on its route
        /// </summary>
        /// <param name="route">club/example-club</param>
        /// <returns>A matching <see cref="Club"/> or <c>null</c> if not found</returns>
        Task<Club> ReadClubByRoute(string route);
    }
}