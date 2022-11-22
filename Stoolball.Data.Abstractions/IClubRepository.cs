using System;
using System.Threading.Tasks;
using Stoolball.Clubs;

namespace Stoolball.Data.Abstractions
{
    public interface IClubRepository
    {
        /// <summary>
        /// Creates a stoolball club and populates the <see cref="Club.ClubId"/>
        /// </summary>
        /// <returns>The created club</returns>
        Task<Club> CreateClub(Club club, Guid memberKey, string memberName);

        /// <summary>
        /// Updates a stoolball club
        /// </summary>
        Task<Club> UpdateClub(Club club, Guid memberKey, string memberName);

        /// <summary>
        /// Delete a stoolball club
        /// </summary>
        Task DeleteClub(Club club, Guid memberKey, string memberName);
    }
}