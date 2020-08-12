using Stoolball.Matches;
using System;
using System.Threading.Tasks;

namespace Stoolball.Umbraco.Data.Matches
{
    public interface IMatchRepository
    {
        /// <summary>
        /// Deletes a stoolball match
        /// </summary>
        Task DeleteMatch(Match match, Guid memberKey, string memberName);

        /// <summary>
        /// Creates a stoolball match
        /// </summary>
        Task<Match> CreateMatch(Match match, Guid memberKey, string memberName);

        /// <summary>
        /// Updates a stoolball match
        /// </summary>
        Task<Match> UpdateMatch(Match match, Guid memberKey, string memberName);
    }
}
