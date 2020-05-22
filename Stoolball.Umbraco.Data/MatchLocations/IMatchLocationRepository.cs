using Stoolball.MatchLocations;
using System;
using System.Threading.Tasks;

namespace Stoolball.Umbraco.Data.MatchLocations
{
    public interface IMatchLocationRepository
    {
        /// <summary>
        /// Creates a match location and populates the <see cref="MatchLocation.MatchLocationId"/>
        /// </summary>
        /// <returns>The created match location</returns>
        Task<MatchLocation> CreateMatchLocation(MatchLocation matchLocation, Guid memberKey, string memberName);

        /// <summary>
        /// Updates a stoolball match location
        /// </summary>
        Task<MatchLocation> UpdateMatchLocation(MatchLocation matchLocation, Guid memberKey, string memberName);
    }
}