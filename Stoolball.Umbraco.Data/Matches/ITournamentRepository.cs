using Stoolball.Matches;
using System;
using System.Threading.Tasks;

namespace Stoolball.Umbraco.Data.Matches
{
    public interface ITournamentRepository
    {
        /// <summary>
        /// Deletes a stoolball tournament
        /// </summary>
        Task DeleteTournament(Tournament tournament, Guid memberKey, string memberName);
    }
}
