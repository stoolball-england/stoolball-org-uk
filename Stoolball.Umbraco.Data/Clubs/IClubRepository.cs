using Stoolball.Clubs;
using System;
using System.Threading.Tasks;

namespace Stoolball.Umbraco.Data.Clubs
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
    }
}