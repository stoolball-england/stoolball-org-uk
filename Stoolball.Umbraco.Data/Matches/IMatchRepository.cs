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

        /// <summary>
        /// Updates details known at the start of play - the location, who won the toss, who is batting, or why cancellation occurred
        /// </summary>
        Task<Match> UpdateStartOfPlay(Match match, Guid memberKey, string memberName);

        /// <summary>
        /// Updates details known at the close of play - the winning team and any awards
        /// </summary>
        Task<Match> UpdateCloseOfPlay(Match match, Guid memberKey, string memberName);
    }
}
