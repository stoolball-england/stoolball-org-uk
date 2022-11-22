using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Stoolball.Matches;
using Stoolball.Statistics;

namespace Stoolball.Data.Abstractions
{
    public interface IStatisticsRepository
    {
        /// <summary>
        /// Delete bowling figures for a match innings.
        /// </summary>
        Task DeleteBowlingFigures(Guid matchInningsId, IDbTransaction transaction);
        Task DeletePlayerStatistics(Guid matchId, IDbTransaction transaction);

        /// <summary>
        /// Updates the bowling figures for a single innings of a match. Assumes caller will handle auditing and logging.
        /// </summary>
        Task<IList<BowlingFigures>> UpdateBowlingFigures(MatchInnings innings, Guid memberKey, string memberName, IDbTransaction transaction);
        Task UpdatePlayerStatistics(IEnumerable<PlayerInMatchStatisticsRecord> statisticsData, IDbTransaction transaction);
    }
}
