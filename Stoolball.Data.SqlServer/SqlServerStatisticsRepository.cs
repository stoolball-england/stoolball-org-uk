using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using Stoolball.Matches;
using Stoolball.Statistics;
using Stoolball.Teams;
using static Stoolball.Constants;

namespace Stoolball.Data.SqlServer
{
    /// <summary>
    /// Writes stoolball statistics data to the Umbraco database
    /// </summary>
    public class SqlServerStatisticsRepository : IStatisticsRepository
    {
        private readonly IDatabaseConnectionFactory _databaseConnectionFactory;
        private readonly IPlayerRepository _playerRepository;

        public SqlServerStatisticsRepository(IDatabaseConnectionFactory databaseConnectionFactory, IPlayerRepository playerRepository)
        {
            _databaseConnectionFactory = databaseConnectionFactory ?? throw new ArgumentNullException(nameof(databaseConnectionFactory));
            _playerRepository = playerRepository ?? throw new ArgumentNullException(nameof(playerRepository));
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

            await transaction.Connection.ExecuteAsync($"DELETE FROM {Tables.BowlingFigures} WHERE MatchInningsId = @MatchInningsId", new { innings.MatchInningsId }, transaction).ConfigureAwait(false);

            var i = 1;
            foreach (var bowlingFigures in innings.BowlingFigures)
            {
                if (bowlingFigures.Bowler == null)
                {
                    throw new ArgumentException($"{nameof(bowlingFigures.Bowler)} cannot be null in a {typeof(BowlingFigures)}");
                }

                if (innings.MatchInningsId == null)
                {
                    throw new ArgumentException($"{nameof(innings.MatchInningsId)} cannot be null in a {typeof(MatchInnings)}");
                }

                bowlingFigures.Bowler.PlayerIdentityId = await _playerRepository.CreateOrMatchPlayerIdentity(bowlingFigures.Bowler, memberKey, memberName, transaction).ConfigureAwait(false);

                await transaction.Connection.ExecuteAsync($@"INSERT INTO {Tables.BowlingFigures} 
                                (BowlingFiguresId, MatchInningsId, BowlingOrder, PlayerIdentityId, Overs, Maidens, RunsConceded, Wickets, IsFromOversBowled)
                                VALUES 
                                (@BowlingFiguresId, @MatchInningsId, @BowlingOrder, @PlayerIdentityId, @Overs, @Maidens, @RunsConceded, @Wickets, @IsFromOversBowled)",
                        new
                        {
                            BowlingFiguresId = Guid.NewGuid(),
                            innings.MatchInningsId,
                            BowlingOrder = i,
                            bowlingFigures.Bowler.PlayerIdentityId,
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
    }
}
