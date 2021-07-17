using System;
using System.Data;
using System.Threading.Tasks;

namespace Stoolball.Matches
{
    public interface IMatchRepository
    {
        /// <summary>
        /// Deletes a stoolball match
        /// </summary>
        Task DeleteMatch(Match match, Guid memberKey, string memberName);

        /// <summary>
        /// Deletes a stoolball match
        /// </summary>
        Task DeleteMatch(Match match, Guid memberKey, string memberName, IDbTransaction transaction);

        /// <summary>
        /// Creates a stoolball match
        /// </summary>
        Task<Match> CreateMatch(Match match, Guid memberKey, string memberName);

        /// <summary>
        /// Creates a stoolball match
        /// </summary>
        Task<Match> CreateMatch(Match match, Guid memberKey, string memberName, IDbTransaction dbTransaction);

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

        /// <summary>
        /// Updates the bowling scorecard for a single innings of a match
        /// </summary>
        Task<MatchInnings> UpdateBowlingScorecard(Match match, Guid matchInningsId, Guid memberKey, string memberName);

        /// <summary>
        /// Updates the battings scorecard for a single innings of a match
        /// </summary>
        Task<MatchInnings> UpdateBattingScorecard(Match match, Guid matchInningsId, Guid memberKey, string memberName);

        /// <summary>
        /// Updates the format of a match - how many innings and how many overs are played
        /// </summary>
        Task<Match> UpdateMatchFormat(Match match, Guid memberKey, string memberName);
    }
}
