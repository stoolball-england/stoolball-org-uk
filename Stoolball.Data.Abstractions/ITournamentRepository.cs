using System;
using System.Threading.Tasks;
using Stoolball.Matches;

namespace Stoolball.Data.Abstractions
{
    public interface ITournamentRepository
    {
        /// <summary>
        /// Creates a stoolball tournament
        /// </summary>
        Task<Tournament> CreateTournament(Tournament tournament, Guid memberKey, string memberName);

        /// <summary>
        /// Updates a stoolball tournament
        /// </summary>
        Task<Tournament> UpdateTournament(Tournament tournament, Guid memberKey, string memberName);

        /// <summary>
        /// Updates the teams in a stoolball tournament
        /// </summary>
        Task<Tournament> UpdateTeams(Tournament tournament, Guid memberKey, string memberUsername, string memberName);

        /// <summary>
        /// Updates the seasons a stoolball tournament is listed in
        /// </summary>
        Task<Tournament> UpdateSeasons(Tournament tournament, Guid memberKey, string memberUsername, string memberName);

        /// <summary>
        /// Updates the matches in a stoolball tournament
        /// </summary>
        Task<Tournament> UpdateMatches(Tournament tournament, Guid memberKey, string memberUsername, string memberName);

        /// <summary>
        /// Deletes a stoolball tournament
        /// </summary>
        Task DeleteTournament(Tournament tournament, Guid memberKey, string memberName);
    }
}
