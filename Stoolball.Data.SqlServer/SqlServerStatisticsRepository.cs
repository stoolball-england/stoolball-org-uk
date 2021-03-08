using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using Stoolball.Matches;
using Stoolball.Statistics;

namespace Stoolball.Data.SqlServer
{
    /// <summary>
    /// Writes stoolball statistics data to the Umbraco database
    /// </summary>
    public class SqlServerStatisticsRepository : IStatisticsRepository
    {
        private readonly IPlayerRepository _playerRepository;

        public SqlServerStatisticsRepository(IPlayerRepository playerRepository)
        {
            _playerRepository = playerRepository ?? throw new ArgumentNullException(nameof(playerRepository));
        }

        public async Task DeleteBowlingFigures(Guid matchInningsId, IDbTransaction transaction)
        {
            if (transaction is null)
            {
                throw new ArgumentNullException(nameof(transaction));
            }

            await transaction.Connection.ExecuteAsync($"DELETE FROM {Tables.BowlingFigures} WHERE MatchInningsId = @matchInningsId", new { matchInningsId }, transaction).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<IList<BowlingFigures>> UpdateBowlingFigures(MatchInnings innings, Guid memberKey, string memberName, IDbTransaction transaction)
        {
            if (innings is null)
            {
                throw new ArgumentNullException(nameof(innings));
            }

            if (string.IsNullOrWhiteSpace(memberName))
            {
                throw new ArgumentNullException(nameof(memberName));
            }

            if (transaction is null)
            {
                throw new ArgumentNullException(nameof(transaction));
            }

            var i = 1;
            foreach (var bowlingFigures in innings.BowlingFigures)
            {
                if (!bowlingFigures.BowlingFiguresId.HasValue)
                {
                    bowlingFigures.BowlingFiguresId = Guid.NewGuid();
                }

                if (bowlingFigures.Bowler == null)
                {
                    throw new ArgumentException($"{nameof(bowlingFigures.Bowler)} cannot be null in a {typeof(BowlingFigures)}");
                }

                if (innings.MatchInningsId == null)
                {
                    throw new ArgumentException($"{nameof(innings.MatchInningsId)} cannot be null in a {typeof(MatchInnings)}");
                }

                bowlingFigures.Bowler = await _playerRepository.CreateOrMatchPlayerIdentity(bowlingFigures.Bowler, memberKey, memberName, transaction).ConfigureAwait(false);

                await transaction.Connection.ExecuteAsync($@"INSERT INTO {Tables.BowlingFigures} 
                                (BowlingFiguresId, MatchInningsId, BowlingOrder, BowlerPlayerIdentityId, Overs, Maidens, RunsConceded, Wickets, IsFromOversBowled)
                                VALUES 
                                (@BowlingFiguresId, @MatchInningsId, @BowlingOrder, @BowlerPlayerIdentityId, @Overs, @Maidens, @RunsConceded, @Wickets, @IsFromOversBowled)",
                        new
                        {
                            bowlingFigures.BowlingFiguresId,
                            innings.MatchInningsId,
                            BowlingOrder = i,
                            BowlerPlayerIdentityId = bowlingFigures.Bowler.PlayerIdentityId,
                            bowlingFigures.Overs,
                            bowlingFigures.Maidens,
                            bowlingFigures.RunsConceded,
                            bowlingFigures.Wickets,
                            IsFromOversBowled = true
                        },
                        transaction).ConfigureAwait(false);

                i++;
            }

            return innings.BowlingFigures;
        }

        public async Task DeletePlayerStatistics(Guid matchId, IDbTransaction transaction)
        {
            if (transaction is null)
            {
                throw new ArgumentNullException(nameof(transaction));
            }

            await transaction.Connection.ExecuteAsync($"DELETE FROM {Tables.PlayerInMatchStatistics} WHERE MatchId = @matchId", new { matchId }, transaction).ConfigureAwait(false);
        }

        public async Task UpdatePlayerStatistics(IEnumerable<PlayerInMatchStatisticsRecord> statisticsData, IDbTransaction transaction)
        {
            if (statisticsData is null)
            {
                throw new ArgumentNullException(nameof(statisticsData));
            }

            if (transaction is null)
            {
                throw new ArgumentNullException(nameof(transaction));
            }

            foreach (var record in statisticsData)
            {
                await transaction.Connection.ExecuteAsync($@"INSERT INTO {Tables.PlayerInMatchStatistics}
                    (PlayerInMatchStatisticsId, PlayerId, PlayerIdentityId, PlayerIdentityName, PlayerRoute, MatchId, MatchStartTime, MatchType, MatchPlayerType, MatchName, MatchRoute, 
                     TournamentId, SeasonId, CompetitionId, MatchTeamId, ClubId, TeamId, TeamName, TeamRoute, OppositionTeamId, OppositionTeamName, OppositionTeamRoute, MatchLocationId, 
                     MatchInningsPair, TeamRunsScored, TeamWicketsLost, TeamBonusOrPenaltyRunsAwarded, TeamRunsConceded, TeamNoBallsConceded, TeamWidesConceded, TeamByesConceded, TeamWicketsTaken, 
                     BowlingFiguresId, OverNumberOfFirstOverBowled, BallsBowled, Overs, Maidens, NoBalls, Wides, RunsConceded, HasRunsConceded, Wickets, WicketsWithBowling, 
                     WonToss, BattedFirst, PlayerInningsNumber, PlayerInningsId, BattingPosition, DismissalType, PlayerWasDismissed, BowledByPlayerIdentityId, BowledByPlayerIdentityName, BowledByPlayerRoute, 
                     CaughtByPlayerIdentityId, CaughtByPlayerIdentityName, CaughtByPlayerRoute, RunOutByPlayerIdentityId, RunOutByPlayerIdentityName, RunOutByPlayerRoute, 
                     RunsScored, BallsFaced, Catches, RunOuts, WonMatch, PlayerOfTheMatch)
                    VALUES
                    (@PlayerInMatchStatisticsId, @PlayerId, @PlayerIdentityId, @PlayerIdentityName, @PlayerRoute, @MatchId, @MatchStartTime, @MatchType, @MatchPlayerType, @MatchName, @MatchRoute,
                     @TournamentId, @SeasonId, @CompetitionId, @MatchTeamId, @ClubId, @TeamId, @TeamName, @TeamRoute, @OppositionTeamId, @OppositionTeamName, @OppositionTeamRoute, @MatchLocationId,
                     @MatchInningsPair, @TeamRunsScored, @TeamWicketsLost, @TeamBonusOrPenaltyRunsAwarded, @TeamRunsConceded, @TeamNoBallsConceded, @TeamWidesConceded, @TeamByesConceded, @TeamWicketsTaken, 
                     @BowlingFiguresId, @OverNumberOfFirstOverBowled, @BallsBowled, @Overs, @Maidens, @NoBalls, @Wides, @RunsConceded, @HasRunsConceded, @Wickets, @WicketsWithBowling,
                     @WonToss, @BattedFirst, @PlayerInningsNumber, @PlayerInningsId, @BattingPosition, @DismissalType, @PlayerWasDismissed, @BowledByPlayerIdentityId, @BowledByPlayerIdentityName, @BowledByPlayerRoute,
                     @CaughtByPlayerIdentityId, @CaughtByPlayerIdentityName, @CaughtByPlayerRoute, @RunOutByPlayerIdentityId, @RunOutByPlayerIdentityName, @RunOutByPlayerRoute,
                     @RunsScored, @BallsFaced, @Catches, @RunOuts, @WonMatch, @PlayerOfTheMatch)",
                        new
                        {
                            PlayerInMatchStatisticsId = Guid.NewGuid(),
                            record.PlayerId,
                            record.PlayerIdentityId,
                            record.PlayerIdentityName,
                            record.PlayerRoute,
                            record.MatchId,
                            record.MatchStartTime,
                            MatchType = record.MatchType.ToString(),
                            MatchPlayerType = record.MatchPlayerType.ToString(),
                            record.MatchName,
                            record.MatchRoute,
                            record.TournamentId,
                            record.SeasonId,
                            record.CompetitionId,
                            record.MatchTeamId,
                            record.ClubId,
                            record.TeamId,
                            record.TeamName,
                            record.TeamRoute,
                            record.OppositionTeamId,
                            record.OppositionTeamName,
                            record.OppositionTeamRoute,
                            record.MatchLocationId,
                            record.MatchInningsPair,
                            record.TeamRunsScored,
                            record.TeamWicketsLost,
                            record.TeamBonusOrPenaltyRunsAwarded,
                            record.TeamRunsConceded,
                            record.TeamNoBallsConceded,
                            record.TeamWidesConceded,
                            record.TeamByesConceded,
                            record.TeamWicketsTaken,
                            record.BowlingFiguresId,
                            record.OverNumberOfFirstOverBowled,
                            record.BallsBowled,
                            record.Overs,
                            record.Maidens,
                            record.NoBalls,
                            record.Wides,
                            record.RunsConceded,
                            record.HasRunsConceded,
                            record.Wickets,
                            record.WicketsWithBowling,
                            record.WonToss,
                            record.BattedFirst,
                            record.PlayerInningsNumber,
                            record.PlayerInningsId,
                            record.BattingPosition,
                            record.DismissalType,
                            record.PlayerWasDismissed,
                            record.BowledByPlayerIdentityId,
                            record.BowledByPlayerIdentityName,
                            record.BowledByPlayerRoute,
                            record.CaughtByPlayerIdentityId,
                            record.CaughtByPlayerIdentityName,
                            record.CaughtByPlayerRoute,
                            record.RunOutByPlayerIdentityId,
                            record.RunOutByPlayerIdentityName,
                            record.RunOutByPlayerRoute,
                            record.RunsScored,
                            record.BallsFaced,
                            record.Catches,
                            record.RunOuts,
                            record.WonMatch,
                            record.PlayerOfTheMatch
                        },
                        transaction).ConfigureAwait(false);
            }
        }
    }
}
