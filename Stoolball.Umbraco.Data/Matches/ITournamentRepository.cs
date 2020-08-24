using Stoolball.Matches;
using System;
using System.Threading.Tasks;

namespace Stoolball.Umbraco.Data.Matches
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
        /// Deletes a stoolball tournament
        /// </summary>
        Task DeleteTournament(Tournament tournament, Guid memberKey, string memberName);
    }
}
