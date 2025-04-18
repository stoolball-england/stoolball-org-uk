﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;
using Dapper;
using Moq;
using Stoolball.Data.Abstractions;
using Stoolball.Data.SqlServer.IntegrationTests.Fixtures;
using Stoolball.Matches;
using Stoolball.Statistics;
using Stoolball.Teams;
using Xunit;

namespace Stoolball.Data.SqlServer.IntegrationTests.Statistics
{
    [Collection(IntegrationTestConstants.TestDataIntegrationTestCollection)]
    public class SqlServerStatisticsRepositoryTests : IDisposable
    {
        private readonly SqlServerTestDataFixture _databaseFixture;
        private readonly TransactionScope _scope;

        public SqlServerStatisticsRepositoryTests(SqlServerTestDataFixture databaseFixture)
        {
            _databaseFixture = databaseFixture ?? throw new ArgumentNullException(nameof(databaseFixture));
            _scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
        }

#nullable disable
        [Fact]
        public async Task DeleteBowlingFigures_throws_ArgumentNullException_if_transaction_is_null()
        {
            var repo = new SqlServerStatisticsRepository(Mock.Of<IPlayerRepository>());

            await Assert.ThrowsAsync<ArgumentNullException>(async () => await repo.DeleteBowlingFigures(Guid.NewGuid(), null).ConfigureAwait(false)).ConfigureAwait(false);
        }
#nullable enable

        [Fact]
        public async Task DeleteBowlingFigures_succeeds()
        {
            var repo = new SqlServerStatisticsRepository(Mock.Of<IPlayerRepository>());

            using (var connection = _databaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    var matchInningsId = await connection.QuerySingleAsync<Guid>($"SELECT TOP 1 MatchInningsId FROM {Tables.BowlingFigures}", transaction: transaction).ConfigureAwait(false);

                    await repo.DeleteBowlingFigures(matchInningsId, transaction).ConfigureAwait(false);

                    var count = await connection.QuerySingleAsync<int>($"SELECT COUNT(*) FROM {Tables.BowlingFigures} WHERE MatchInningsId = @matchInningsId", new { matchInningsId }, transaction).ConfigureAwait(false);

                    transaction.Rollback();

                    Assert.Equal(0, count);
                }
            }
        }

#nullable disable
        [Fact]
        public async Task UpdateBowlingFigures_throws_ArgumentNullException_if_innings_is_null()
        {
            var repo = new SqlServerStatisticsRepository(Mock.Of<IPlayerRepository>());

            await Assert.ThrowsAsync<ArgumentNullException>(async () => await repo.UpdateBowlingFigures(null, Guid.NewGuid(), "Member name", Mock.Of<IDbTransaction>()).ConfigureAwait(false)).ConfigureAwait(false);
        }

        [Fact]
        public async Task UpdateBowlingFigures_throws_ArgumentNullException_if_memberName_is_null()
        {
            var repo = new SqlServerStatisticsRepository(Mock.Of<IPlayerRepository>());

            await Assert.ThrowsAsync<ArgumentNullException>(async () => await repo.UpdateBowlingFigures(new MatchInnings(), Guid.NewGuid(), null, Mock.Of<IDbTransaction>()).ConfigureAwait(false)).ConfigureAwait(false);
        }
#nullable enable

        [Fact]
        public async Task UpdateBowlingFigures_throws_ArgumentNullException_if_memberName_is_empty_string()
        {
            var repo = new SqlServerStatisticsRepository(Mock.Of<IPlayerRepository>());

            await Assert.ThrowsAsync<ArgumentNullException>(async () => await repo.UpdateBowlingFigures(new MatchInnings(), Guid.NewGuid(), string.Empty, Mock.Of<IDbTransaction>()).ConfigureAwait(false)).ConfigureAwait(false);
        }

#nullable disable
        [Fact]
        public async Task UpdateBowlingFigures_throws_ArgumentNullException_if_transaction_is_null()
        {
            var repo = new SqlServerStatisticsRepository(Mock.Of<IPlayerRepository>());

            await Assert.ThrowsAsync<ArgumentNullException>(async () => await repo.UpdateBowlingFigures(new MatchInnings(), Guid.NewGuid(), "Member name", null).ConfigureAwait(false)).ConfigureAwait(false);
        }
#nullable enable

        [Fact]
        public async Task UpdateBowlingFigures_throws_ArgumentException_if_any_bowler_is_null()
        {
            var innings = new MatchInnings
            {
                MatchInningsId = Guid.NewGuid(),
                BowlingFigures = new List<BowlingFigures> {
                    {
                        new BowlingFigures()
                    }
                }
            };

            var repo = new SqlServerStatisticsRepository(Mock.Of<IPlayerRepository>());

            await Assert.ThrowsAsync<ArgumentException>(async () => await repo.UpdateBowlingFigures(innings, Guid.NewGuid(), "Member name", Mock.Of<IDbTransaction>()).ConfigureAwait(false)).ConfigureAwait(false);

        }

        [Fact]
        public async Task UpdateBowlingFigures_calls_CreateOrMatchPlayerIdentity_before_inserting()
        {
            using (var connection = _databaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                connection.Open();

                using (var transaction = connection.BeginTransaction())
                {
                    var data = await connection.QuerySingleAsync<(Guid matchInningsId, Guid bowlerPlayerIdentityId)>($"SELECT TOP 1 MatchInningsId, BowlerPlayerIdentityId FROM {Tables.BowlingFigures}", transaction: transaction).ConfigureAwait(false);

                    var innings = new MatchInnings
                    {
                        MatchInningsId = data.matchInningsId,
                        BowlingFigures = new List<BowlingFigures> {
                            {
                                new BowlingFigures { Bowler = new PlayerIdentity { PlayerIdentityId = data.bowlerPlayerIdentityId } }
                            }
                        }
                    };
                    var memberKey = Guid.NewGuid();
                    var memberName = "Member name";

                    var playerRepo = new Mock<IPlayerRepository>();
                    playerRepo.Setup(x => x.CreateOrMatchPlayerIdentity(innings.BowlingFigures[0].Bowler, memberKey, memberName, transaction)).Returns(Task.FromResult(innings.BowlingFigures[0].Bowler));

                    var statisticsRepo = new SqlServerStatisticsRepository(playerRepo.Object);

                    _ = statisticsRepo.UpdateBowlingFigures(innings, memberKey, memberName, transaction).ConfigureAwait(false);

                    playerRepo.Verify(x => x.CreateOrMatchPlayerIdentity(innings.BowlingFigures[0].Bowler, memberKey, memberName, transaction), Times.Once);

                    transaction.Rollback();
                }
            }
        }

        [Fact]
        public async Task UpdateBowlingFigures_inserts_and_returns_BowlingFigures()
        {
            using (var connection = _databaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                connection.Open();

                using (var transaction = connection.BeginTransaction())
                {
                    var data = await connection.QuerySingleAsync<(Guid matchInningsId, Guid bowlerPlayerIdentityId)>($"SELECT TOP 1 MatchInningsId, BowlerPlayerIdentityId FROM {Tables.BowlingFigures}", transaction: transaction).ConfigureAwait(false);

                    var innings = new MatchInnings
                    {
                        MatchInningsId = data.matchInningsId,
                        BowlingFigures = new List<BowlingFigures> {
                            {
                                new BowlingFigures {
                                    BowlingFiguresId = Guid.NewGuid(),
                                    Bowler = new PlayerIdentity { PlayerIdentityId = data.bowlerPlayerIdentityId } ,
                                    Overs = (decimal)5.4,
                                    Maidens =1,
                                    RunsConceded = 25,
                                    Wickets =2
                                }
                            }
                        }
                    };
                    var memberKey = Guid.NewGuid();
                    var memberName = "Member name";

                    var playerRepo = new Mock<IPlayerRepository>();
                    playerRepo.Setup(x => x.CreateOrMatchPlayerIdentity(innings.BowlingFigures[0].Bowler, memberKey, memberName, transaction)).Returns(Task.FromResult(innings.BowlingFigures[0].Bowler));

                    var statisticsRepo = new SqlServerStatisticsRepository(playerRepo.Object);

                    var results = await statisticsRepo.UpdateBowlingFigures(innings, memberKey, memberName, transaction).ConfigureAwait(false);

                    var count = await connection.ExecuteScalarAsync<int>(@$"SELECT COUNT(*) FROM {Tables.BowlingFigures} 
                                        WHERE BowlingFiguresId = @BowlingFiguresId
                                        AND BowlerPlayerIdentityId = @PlayerIdentityId
                                        AND Overs = @Overs
                                        AND Maidens = @Maidens
                                        AND RunsConceded = @RunsConceded
                                        AND Wickets = @Wickets",
                                        new
                                        {
                                            innings.BowlingFigures[0].BowlingFiguresId,
                                            innings.BowlingFigures[0].Bowler.PlayerIdentityId,
                                            innings.BowlingFigures[0].Overs,
                                            innings.BowlingFigures[0].Maidens,
                                            innings.BowlingFigures[0].RunsConceded,
                                            innings.BowlingFigures[0].Wickets
                                        },
                                        transaction).ConfigureAwait(false);

                    Assert.Equal(1, count);

                    for (var i = 0; i < innings.BowlingFigures.Count; i++)
                    {
                        Assert.Equal(innings.BowlingFigures[i].BowlingFiguresId, results[i].BowlingFiguresId);
                        Assert.Equal(innings.BowlingFigures[i].Bowler.PlayerIdentityId, results[i].Bowler.PlayerIdentityId);
                        Assert.Equal(innings.BowlingFigures[i].Overs, results[i].Overs);
                        Assert.Equal(innings.BowlingFigures[i].Maidens, results[i].Maidens);
                        Assert.Equal(innings.BowlingFigures[i].RunsConceded, results[i].RunsConceded);
                        Assert.Equal(innings.BowlingFigures[i].Wickets, results[i].Wickets);
                    }

                    transaction.Rollback();
                }
            }
        }

#nullable disable
        [Fact]
        public async Task DeletePlayerStatistics_throws_ArgumentNullException_if_transaction_is_null()
        {
            var repo = new SqlServerStatisticsRepository(Mock.Of<IPlayerRepository>());

            await Assert.ThrowsAsync<ArgumentNullException>(async () => await repo.DeletePlayerStatistics(Guid.NewGuid(), null).ConfigureAwait(false)).ConfigureAwait(false);
        }
#nullable enable

        [Fact]
        public async Task DeletePlayerStatistics_succeeds()
        {
            var repo = new SqlServerStatisticsRepository(Mock.Of<IPlayerRepository>());

            using (var connection = _databaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    var matchId = await connection.QuerySingleAsync<Guid>($"SELECT TOP 1 MatchId FROM {Tables.PlayerInMatchStatistics}", transaction: transaction).ConfigureAwait(false);

                    await repo.DeletePlayerStatistics(matchId, transaction).ConfigureAwait(false);

                    var count = await connection.QuerySingleAsync<int>($"SELECT COUNT(*) FROM {Tables.PlayerInMatchStatistics} WHERE MatchId = @matchId", new { matchId }, transaction).ConfigureAwait(false);

                    transaction.Rollback();

                    Assert.Equal(0, count);
                }
            }
        }

#nullable disable
        [Fact]
        public async Task UpdatePlayerStatistics_throws_ArgumentNullException_if_statisticsData_is_null()
        {
            var repo = new SqlServerStatisticsRepository(Mock.Of<IPlayerRepository>());

            await Assert.ThrowsAsync<ArgumentNullException>(async () => await repo.UpdatePlayerStatistics(null, Mock.Of<IDbTransaction>()).ConfigureAwait(false)).ConfigureAwait(false);
        }

        [Fact]
        public async Task UpdatePlayerStatistics_throws_ArgumentNullException_if_transaction_is_null()
        {
            var repo = new SqlServerStatisticsRepository(Mock.Of<IPlayerRepository>());

            await Assert.ThrowsAsync<ArgumentNullException>(async () => await repo.UpdatePlayerStatistics(Array.Empty<PlayerInMatchStatisticsRecord>(), null).ConfigureAwait(false)).ConfigureAwait(false);
        }
#nullable enable

        [Fact]
        public async Task UpdatePlayerStatistics_inserts_minimal_record()
        {
            var repo = new SqlServerStatisticsRepository(Mock.Of<IPlayerRepository>());

            using (var connection = _databaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    var data = await connection.QuerySingleAsync<(
                        Guid matchId, DateTimeOffset startTime, MatchType matchType, PlayerType playerType, string matchName, string matchRoute, Guid? matchLocationId,
                        Guid? tournamentId, Guid? seasonId, Guid? competitionId, Guid matchTeamId, Guid teamId, string teamRoute, string teamName, Guid? clubId,
                        Guid oppositionTeamId, string oppositionTeamRoute, string oppositionTeamName, Guid playerId, string playerRoute, Guid playerIdentityId, string playerIdentityName)>(
                        @$"SELECT TOP 1 m.MatchId, m.StartTime, m.MatchType, m.PlayerType, m.MatchName, m.MatchRoute, m.MatchLocationId, m.TournamentId, m.SeasonId, s.CompetitionId,
                           home.MatchTeamId, home.TeamId, homeTeam.TeamRoute, dbo.fn_TeamName(home.TeamId, m.StartTime) AS TeamName, homeTeam.ClubId,
                           away.TeamId AS OppositionTeamId, awayTeam.TeamRoute AS OppositionTeamRoute, dbo.fn_TeamName(away.TeamId, m.StartTime) AS OppositionTeamName,
                           p.PlayerId, p.PlayerRoute, pi.PlayerIdentityId, pi.PlayerIdentityName
                           FROM {Tables.Match} m INNER JOIN {Tables.MatchTeam} home ON m.MatchId = home.MatchId AND home.TeamRole = '{TeamRole.Home.ToString()}'
                           INNER JOIN {Tables.Team} homeTeam ON home.TeamId = homeTeam.TeamId
                           INNER JOIN {Tables.MatchTeam} away ON m.MatchId = away.MatchId AND away.TeamRole = '{TeamRole.Away.ToString()}'
                           INNER JOIN {Tables.Team} awayTeam ON away.TeamId = awayTeam.TeamId
                           INNER JOIN {Tables.PlayerIdentity} pi ON home.TeamId = pi.TeamId AND pi.PlayerIdentityId = (SELECT TOP 1 PlayerIdentityId FROM {Tables.PlayerIdentity} WHERE TeamId = home.TeamId)
                           INNER JOIN {Tables.Player} p ON pi.PlayerId = p.PlayerId
                           LEFT JOIN {Tables.Season} s ON m.SeasonId = s.SeasonId",
                           transaction: transaction).ConfigureAwait(false);

                    var stats = new PlayerInMatchStatisticsRecord
                    {
                        PlayerId = data.playerId,
                        PlayerIdentityId = data.playerIdentityId,
                        MatchId = data.matchId,
                        MatchTeamId = data.matchTeamId,
                        OppositionTeamId = data.oppositionTeamId,
                        MatchInningsPair = 1,
                        HasRunsConceded = false,
                        PlayerWasDismissed = false,
                        Catches = 0,
                        RunOuts = 0,
                        PlayerOfTheMatch = false
                    };

                    await repo.UpdatePlayerStatistics(new[] { stats }, transaction).ConfigureAwait(false);

                    var count = await connection.QuerySingleAsync<int>(@$"SELECT COUNT(*) FROM {Tables.PlayerInMatchStatistics} 
                                WHERE PlayerId = @PlayerId
                                AND PlayerIdentityId = @PlayerIdentityId
                                AND PlayerIdentityName = @playerIdentityName
                                AND PlayerRoute = @playerRoute
                                AND MatchId = @MatchId
                                AND MatchStartTime = @startTime
                                AND MatchType = @MatchType
                                AND MatchPlayerType = @PlayerType
                                AND MatchName = @matchName
                                AND MatchRoute = @matchRoute
                                AND MatchTeamId = @MatchTeamId
                                AND TeamId = @TeamId
                                AND TeamName = @teamName
                                AND TeamRoute = @teamRoute
                                AND OppositionTeamId = @OppositionTeamId
                                AND OppositionTeamName = @oppositionTeamName
                                AND OppositionTeamRoute = @oppositionTeamRoute
                                AND MatchInningsPair = @MatchInningsPair
                                AND HasRunsConceded = @HasRunsConceded
                                AND PlayerWasDismissed = @PlayerWasDismissed
                                AND Catches = @Catches
                                AND RunOuts = @RunOuts
                                AND PlayerOfTheMatch = @PlayerOfTheMatch
                                AND BattedFirst IS NULL
                                AND PlayerInningsId IS NULL
                                AND PlayerInningsNumber IS NULL
                                AND BattingPosition IS NULL
                                AND BowledByPlayerIdentityId IS NULL
                                AND BowledByPlayerIdentityName IS NULL
                                AND BowledByPlayerRoute IS NULL
                                AND CaughtByPlayerIdentityId IS NULL
                                AND CaughtByPlayerIdentityName IS NULL
                                AND CaughtByPlayerRoute IS NULL
                                AND RunOutByPlayerIdentityId IS NULL
                                AND RunOutByPlayerIdentityName IS NULL
                                AND RunOutByPlayerRoute IS NULL
                                AND DismissalType IS NULL
                                AND RunsScored IS NULL
                                AND BallsFaced IS NULL
                                AND (ClubId = @clubId OR (ClubId IS NULL AND @clubId IS NULL)) 
                                AND (TournamentId = @tournamentId OR (TournamentId IS NULL AND @tournamentId IS NULL)) 
                                AND (SeasonId = @seasonId OR (SeasonId IS NULL AND @seasonId IS NULL)) 
                                AND (CompetitionId = @competitionId OR (CompetitionId IS NULL AND @competitionId IS NULL)) 
                                AND (MatchLocationId = @matchLocationId OR (MatchLocationId IS NULL AND @matchLocationId IS NULL)) 
                                AND Overs IS NULL
                                AND OverNumberOfFirstOverBowled IS NULL
                                AND BallsBowled IS NULL
                                AND Maidens IS NULL
                                AND RunsConceded IS NULL
                                AND Wickets IS NULL
                                AND WicketsWithBowling IS NULL
                                AND NoBalls IS NULL
                                AND Wides IS NULL
                                AND TeamRunsScored IS NULL
                                AND TeamWicketsLost IS NULL
                                AND TeamBonusOrPenaltyRunsAwarded IS NULL
                                AND TeamByesConceded IS NULL
                                AND TeamNoBallsConceded IS NULL
                                AND TeamRunsConceded IS NULL
                                AND TeamWicketsTaken IS NULL
                                AND TeamWidesConceded IS NULL
                                AND WonMatch IS NULL
                                AND WonToss IS NULL",
                                           new
                                           {
                                               stats.PlayerId,
                                               stats.PlayerIdentityId,
                                               data.playerIdentityName,
                                               data.playerRoute,
                                               stats.MatchId,
                                               data.startTime,
                                               MatchType = data.matchType.ToString(),
                                               PlayerType = data.playerType.ToString(),
                                               data.matchName,
                                               data.matchRoute,
                                               stats.MatchTeamId,
                                               data.teamId,
                                               data.teamName,
                                               data.teamRoute,
                                               data.clubId,
                                               data.tournamentId,
                                               data.seasonId,
                                               data.competitionId,
                                               data.matchLocationId,
                                               stats.OppositionTeamId,
                                               data.oppositionTeamName,
                                               data.oppositionTeamRoute,
                                               stats.MatchInningsPair,
                                               stats.HasRunsConceded,
                                               stats.PlayerWasDismissed,
                                               stats.Catches,
                                               stats.RunOuts,
                                               stats.PlayerOfTheMatch
                                           },
                                           transaction).ConfigureAwait(false);

                    Assert.Equal(1, count);

                    transaction.Rollback();
                }
            }
        }

        [Fact]
        public async Task UpdatePlayerStatistics_inserts_multiple_complete_records()
        {
            var repo = new SqlServerStatisticsRepository(Mock.Of<IPlayerRepository>());

            using (var connection = _databaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    // For this test the data doesn't need to be real (ie the team doesn't have to be right for the players, dismissals can be caught and run-out)  
                    // but the IDs do need to be valid foreign keys.

                    var matchData = (await connection.QueryAsync<(Guid matchId, Guid homeMatchTeamId, Guid oppositionTeamId)>(
                        @$"SELECT TOP 3 m.MatchId, home.MatchTeamId AS HomeMatchTeamId, away.TeamId AS OppositionTeamId
                           FROM {Tables.Match} m 
                           INNER JOIN {Tables.MatchTeam} home ON m.MatchId = home.MatchId AND home.TeamRole = '{TeamRole.Home.ToString()}'
                           INNER JOIN {Tables.Team} homeTeam ON home.TeamId = homeTeam.TeamId
                           INNER JOIN {Tables.MatchTeam} away ON m.MatchId = away.MatchId AND away.TeamRole = '{TeamRole.Away.ToString()}'
                           WHERE MatchLocationId IS NOT NULL AND homeTeam.ClubId IS NOT NULL",
                           transaction: transaction).ConfigureAwait(false)).AsList();

                    var playerData = (await connection.QueryAsync<(Guid playerId, Guid playerIdentityId)>(
                        $"SELECT TOP 12 PlayerId, PlayerIdentityId FROM {Tables.PlayerIdentity}", transaction: transaction).ConfigureAwait(false)).AsList();

                    var playerInningsData = (await connection.QueryAsync<Guid>($"SELECT TOP 3 PlayerInningsId FROM {Tables.PlayerInnings}", transaction: transaction).ConfigureAwait(false)).AsList();

                    var statisticsRecords = new List<PlayerInMatchStatisticsRecord>(3);
                    for (var i = 0; i < 3; i++)
                    {
                        var uniqueString = Guid.NewGuid().ToString();
                        var isEven = i % 2 == 0;
                        var playerIndex = i * 4;

                        statisticsRecords.Add(new PlayerInMatchStatisticsRecord
                        {
                            PlayerId = playerData[playerIndex].playerId,
                            PlayerIdentityId = playerData[playerIndex].playerIdentityId,
                            MatchId = matchData[i].matchId,
                            MatchTeamId = matchData[i].homeMatchTeamId,
                            OppositionTeamId = matchData[i].oppositionTeamId,
                            MatchInningsPair = isEven ? 2 : 1,
                            HasRunsConceded = isEven,
                            PlayerWasDismissed = isEven,
                            Catches = i,
                            RunOuts = i + 1,
                            PlayerOfTheMatch = isEven,
                            BattedFirst = isEven,
                            PlayerInningsId = playerInningsData[i],
                            PlayerInningsNumber = isEven ? 1 : 2,
                            BattingPosition = 3 + i,
                            BowledByPlayerIdentityId = playerData[playerIndex + 1].playerIdentityId,
                            CaughtByPlayerIdentityId = playerData[playerIndex + 2].playerIdentityId,
                            RunOutByPlayerIdentityId = playerData[playerIndex + 3].playerIdentityId,
                            DismissalType = isEven ? DismissalType.CaughtAndBowled : DismissalType.Bowled,
                            RunsScored = 33 + i,
                            BallsFaced = 30 + i,
                            Overs = (decimal)2.5 + i,
                            OverNumberOfFirstOverBowled = 7 + i,
                            BallsBowled = 16 + i,
                            Maidens = 0 + i,
                            RunsConceded = 20 + i,
                            Wickets = 3 + i,
                            WicketsWithBowling = 2 + i,
                            NoBalls = 5 + i,
                            Wides = 4 + i,
                            TeamRunsScored = 150 + i,
                            TeamWicketsLost = 8 + i,
                            TeamBonusOrPenaltyRunsAwarded = 5 + i,
                            TeamByesConceded = 3 + i,
                            TeamNoBallsConceded = 10 + i,
                            TeamRunsConceded = 167 + i,
                            TeamWicketsTaken = 5 + i,
                            TeamWidesConceded = 11 + i,
                            WonMatch = isEven ? 0 : 1,
                            WonToss = isEven,
                        });
                    }

                    await repo.UpdatePlayerStatistics(statisticsRecords, transaction).ConfigureAwait(false);

                    foreach (var record in statisticsRecords)
                    {
                        var count = await transaction.Connection.QuerySingleAsync<int>(@$"SELECT COUNT(*) FROM {Tables.PlayerInMatchStatistics} 
                                WHERE PlayerId = @PlayerId
                                AND PlayerIdentityId = @PlayerIdentityId
                                AND MatchId = @MatchId
                                AND MatchTeamId = @MatchTeamId
                                AND OppositionTeamId = @OppositionTeamId
                                AND MatchInningsPair = @MatchInningsPair
                                AND HasRunsConceded = @HasRunsConceded
                                AND PlayerWasDismissed = @PlayerWasDismissed
                                AND Catches = @Catches
                                AND RunOuts = @RunOuts
                                AND PlayerOfTheMatch = @PlayerOfTheMatch
                                AND BattedFirst = @BattedFirst
                                AND PlayerInningsId = @PlayerInningsId
                                AND PlayerInningsNumber = @PlayerInningsNumber
                                AND BattingPosition = @BattingPosition
                                AND BowledByPlayerIdentityId = @BowledByPlayerIdentityId
                                AND CaughtByPlayerIdentityId = @CaughtByPlayerIdentityId
                                AND RunOutByPlayerIdentityId = @RunOutByPlayerIdentityId
                                AND DismissalType = @DismissalType
                                AND RunsScored = @RunsScored
                                AND BallsFaced = @BallsFaced
                                AND Overs = @Overs
                                AND OverNumberOfFirstOverBowled = @OverNumberOfFirstOverBowled
                                AND BallsBowled = @BallsBowled
                                AND Maidens = @Maidens
                                AND RunsConceded = @RunsConceded
                                AND Wickets = @Wickets
                                AND WicketsWithBowling = @WicketsWithBowling
                                AND NoBalls = @NoBalls
                                AND Wides = @Wides
                                AND TeamRunsScored = @TeamRunsScored
                                AND TeamWicketsLost = @TeamWicketsLost
                                AND TeamBonusOrPenaltyRunsAwarded = @TeamBonusOrPenaltyRunsAwarded
                                AND TeamByesConceded = @TeamByesConceded
                                AND TeamNoBallsConceded = @TeamNoBallsConceded
                                AND TeamRunsConceded = @TeamRunsConceded
                                AND TeamWicketsTaken = @TeamWicketsTaken
                                AND TeamWidesConceded = @TeamWidesConceded
                                AND WonMatch = @WonMatch
                                AND WonToss = @WonToss",
                                new
                                {
                                    record.PlayerId,
                                    record.PlayerIdentityId,
                                    record.MatchId,
                                    record.MatchTeamId,
                                    record.OppositionTeamId,
                                    record.MatchInningsPair,
                                    record.HasRunsConceded,
                                    record.PlayerWasDismissed,
                                    record.Catches,
                                    record.RunOuts,
                                    record.PlayerOfTheMatch,
                                    record.BattedFirst,
                                    record.PlayerInningsId,
                                    record.PlayerInningsNumber,
                                    record.BattingPosition,
                                    record.BowledByPlayerIdentityId,
                                    record.CaughtByPlayerIdentityId,
                                    record.RunOutByPlayerIdentityId,
                                    record.DismissalType,
                                    record.RunsScored,
                                    record.BallsFaced,
                                    record.Overs,
                                    record.OverNumberOfFirstOverBowled,
                                    record.BallsBowled,
                                    record.Maidens,
                                    record.RunsConceded,
                                    record.Wickets,
                                    record.WicketsWithBowling,
                                    record.NoBalls,
                                    record.Wides,
                                    record.TeamRunsScored,
                                    record.TeamWicketsLost,
                                    record.TeamBonusOrPenaltyRunsAwarded,
                                    record.TeamByesConceded,
                                    record.TeamNoBallsConceded,
                                    record.TeamRunsConceded,
                                    record.TeamWicketsTaken,
                                    record.TeamWidesConceded,
                                    record.WonMatch,
                                    record.WonToss
                                },
                                transaction).ConfigureAwait(false);

                        Assert.Equal(1, count);
                    }

                    transaction.Rollback();
                }
            }
        }

        [Fact]
        public async Task UpdatePlayerStatistics_inserts_BowlingFiguresId_correctly_for_player_who_has_batted_and_bowled()
        {
            // To test a complex example, find a player who:
            // - batted in both innings of a two innings match
            // - batted twice in at least one of those innings
            // - bowled in both innings
            // - didn't bowl for both sides
            var twoInningsMatches = _databaseFixture.TestData.Matches.Where(m => m.MatchInnings.Count > 2);
            var batterIdentities = twoInningsMatches.SelectMany(m => m.MatchInnings).SelectMany(mi => mi.PlayerInnings).Select(pi => pi.Batter!.PlayerIdentityId).Distinct();

            Stoolball.Matches.Match? match = null;
            Guid? playerIdentityId = null;
            foreach (var id in batterIdentities)
            {
                match = twoInningsMatches.FirstOrDefault(m =>
                    (
                        m.MatchInnings[0].PlayerInnings.Any(pi => pi.Batter!.PlayerIdentityId == id) &&
                        m.MatchInnings[2].PlayerInnings.Any(pi => pi.Batter!.PlayerIdentityId == id) &&
                        (m.MatchInnings[0].PlayerInnings.Count(pi => pi.Batter!.PlayerIdentityId == id) > 1 || m.MatchInnings[2].PlayerInnings.Count(pi => pi.Batter!.PlayerIdentityId == id) > 1) &&
                        m.MatchInnings[1].BowlingFigures.Any(bf => bf.Bowler!.PlayerIdentityId == id) &&
                        m.MatchInnings[3].BowlingFigures.Any(bf => bf.Bowler!.PlayerIdentityId == id) &&
                        !m.MatchInnings[0].BowlingFigures.Any(bf => bf.Bowler!.PlayerIdentityId == id) &&
                        !m.MatchInnings[2].BowlingFigures.Any(bf => bf.Bowler!.PlayerIdentityId == id)
                    )

                    ||

                    (
                        m.MatchInnings[1].PlayerInnings.Any(pi => pi.Batter!.PlayerIdentityId == id) &&
                        m.MatchInnings[3].PlayerInnings.Any(pi => pi.Batter!.PlayerIdentityId == id) &&
                        (m.MatchInnings[1].PlayerInnings.Count(pi => pi.Batter!.PlayerIdentityId == id) > 1 || m.MatchInnings[3].PlayerInnings.Count(pi => pi.Batter!.PlayerIdentityId == id) > 1)) &&
                        m.MatchInnings[0].BowlingFigures.Any(bf => bf.Bowler!.PlayerIdentityId == id) &&
                        m.MatchInnings[2].BowlingFigures.Any(bf => bf.Bowler!.PlayerIdentityId == id) &&
                        !m.MatchInnings[1].BowlingFigures.Any(bf => bf.Bowler!.PlayerIdentityId == id) &&
                        !m.MatchInnings[3].BowlingFigures.Any(bf => bf.Bowler!.PlayerIdentityId == id)
                    );
                if (match is not null)
                {
                    playerIdentityId = id;
                    break;
                }
            }
            if (match is null || playerIdentityId is null) { throw new InvalidOperationException("Required test data not found."); }

            var statisticsRecords = new PlayerInMatchStatisticsBuilder(new PlayerIdentityFinder(), new OversHelper())
                .BuildStatisticsForMatch(match)
                .Where(record => record.PlayerIdentityId == playerIdentityId);


            using (var connection = _databaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    var repo = new SqlServerStatisticsRepository(Mock.Of<IPlayerRepository>());
                    await repo.UpdatePlayerStatistics(statisticsRecords, transaction).ConfigureAwait(false);

                    foreach (var record in statisticsRecords)
                    {
                        int? count = null;
                        // Check the BowlingFiguresId is only in the first batting record in an innings
                        if (record.PlayerInningsNumber > 1)
                        {
                            count = await transaction.Connection.QuerySingleAsync<int>(@$"SELECT COUNT(*) FROM {Tables.PlayerInMatchStatistics} 
                                WHERE PlayerInMatchStatisticsId = @PlayerInMatchStatisticsId 
                                  AND BowlingFiguresId IS NULL",
                                    new
                                    {
                                        record.PlayerInMatchStatisticsId
                                    },
                                    transaction).ConfigureAwait(false);

                        }
                        else
                        {
                            // Check the correct BowlingFiguresId is in each innings
                            var bowlingFiguresId = match.MatchInnings.Where(mi => mi.InningsOrderInMatch == (record.MatchInningsPair * 2) - 1 || mi.InningsOrderInMatch == record.MatchInningsPair * 2)
                                                                     .SelectMany(mi => mi.BowlingFigures)
                                                                     .First(bf => bf.Bowler!.PlayerIdentityId == playerIdentityId)
                                                                     .BowlingFiguresId;

                            count = await transaction.Connection.QuerySingleAsync<int>(@$"SELECT COUNT(*) FROM {Tables.PlayerInMatchStatistics} 
                                WHERE PlayerInMatchStatisticsId = @PlayerInMatchStatisticsId 
                                  AND BowlingFiguresId = @BowlingFiguresId",
                                    new
                                    {
                                        record.PlayerInMatchStatisticsId,
                                        BowlingFiguresId = bowlingFiguresId
                                    },
                                    transaction).ConfigureAwait(false);
                        }

                        Assert.Equal(1, count);
                    }

                    transaction.Rollback();
                }
            }
        }

        [Fact]
        public async Task UpdatePlayerStatistics_inserts_NULL_BowlingFiguresId_for_player_who_only_bowled()
        {
            var playerIdentity = _databaseFixture.TestData.PlayerIdentities
                .First(id => !_databaseFixture.TestData.PlayerInnings.Any(pi => pi.Batter!.PlayerIdentityId == id.PlayerIdentityId) &&
                              _databaseFixture.TestData.BowlingFigures.Any(bf => bf.Bowler!.PlayerIdentityId == id.PlayerIdentityId) &&
                             !_databaseFixture.TestData.Awards.Any(aw => aw.PlayerIdentity!.PlayerIdentityId == id.PlayerIdentityId));

            var match = _databaseFixture.TestData.Matches.First(m => m.MatchInnings.Any(mi => mi.BowlingFigures.Any(pi => pi.Bowler!.PlayerIdentityId == playerIdentity.PlayerIdentityId)));

            var statisticsRecords = new PlayerInMatchStatisticsBuilder(new PlayerIdentityFinder(), new OversHelper())
                .BuildStatisticsForMatch(match)
                .Where(record => record.PlayerIdentityId == playerIdentity.PlayerIdentityId);

            using (var connection = _databaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    var repo = new SqlServerStatisticsRepository(Mock.Of<IPlayerRepository>());
                    await repo.UpdatePlayerStatistics(statisticsRecords, transaction).ConfigureAwait(false);

                    foreach (var record in statisticsRecords)
                    {
                        var bowlingFiguresId = match.MatchInnings.Where(mi => mi.InningsOrderInMatch == (record.MatchInningsPair * 2) - 1 || mi.InningsOrderInMatch == record.MatchInningsPair * 2)
                                                                 .SelectMany(mi => mi.BowlingFigures)
                                                                 .First(bf => bf.Bowler!.PlayerIdentityId == playerIdentity.PlayerIdentityId)
                                                                 .BowlingFiguresId;

                        var count = await transaction.Connection.QuerySingleAsync<int>(@$"SELECT COUNT(*) FROM {Tables.PlayerInMatchStatistics} 
                                WHERE PlayerInMatchStatisticsId = @PlayerInMatchStatisticsId 
                                  AND BowlingFiguresId = @BowlingFiguresId",
                                    new
                                    {
                                        record.PlayerInMatchStatisticsId,
                                        BowlingFiguresId = bowlingFiguresId
                                    },
                                    transaction).ConfigureAwait(false);

                        Assert.Equal(1, count);
                    }

                    transaction.Rollback();
                }
            }
        }

        [Fact]
        public async Task UpdatePlayerStatistics_inserts_NULL_BowlingFiguresId_for_player_who_only_batted()
        {
            var playerIdentity = _databaseFixture.TestData.PlayerIdentities
                .First(id => _databaseFixture.TestData.PlayerInnings.Any(pi => pi.Batter!.PlayerIdentityId == id.PlayerIdentityId) &&
                            !_databaseFixture.TestData.BowlingFigures.Any(bf => bf.Bowler!.PlayerIdentityId == id.PlayerIdentityId) &&
                            !_databaseFixture.TestData.Awards.Any(aw => aw.PlayerIdentity!.PlayerIdentityId == id.PlayerIdentityId));

            var match = _databaseFixture.TestData.Matches.First(m => m.MatchInnings.Any(mi => mi.PlayerInnings.Any(pi => pi.Batter!.PlayerIdentityId == playerIdentity.PlayerIdentityId)));

            var statisticsRecords = new PlayerInMatchStatisticsBuilder(new PlayerIdentityFinder(), new OversHelper())
                .BuildStatisticsForMatch(match)
                .Where(record => record.PlayerIdentityId == playerIdentity.PlayerIdentityId);

            await ActAndAssertThatBowlingFiguresIdIsNull(statisticsRecords).ConfigureAwait(false);
        }

        [Fact]
        public async Task UpdatePlayerStatistics_inserts_BowlingFiguresId_correctly_for_player_who_neither_batted_or_bowled()
        {
            var playerIdentity = _databaseFixture.TestData.PlayerIdentities
                  .First(id => !_databaseFixture.TestData.PlayerInnings.Any(pi => pi.Batter!.PlayerIdentityId == id.PlayerIdentityId) &&
                               !_databaseFixture.TestData.BowlingFigures.Any(bf => bf.Bowler!.PlayerIdentityId == id.PlayerIdentityId) &&
                                _databaseFixture.TestData.Awards.Any(aw => aw.PlayerIdentity!.PlayerIdentityId == id.PlayerIdentityId));

            var match = _databaseFixture.TestData.Matches.First(m => m.Awards.Any(aw => aw.PlayerIdentity!.PlayerIdentityId == playerIdentity.PlayerIdentityId));

            var statisticsRecords = new PlayerInMatchStatisticsBuilder(new PlayerIdentityFinder(), new OversHelper())
                .BuildStatisticsForMatch(match)
                .Where(record => record.PlayerIdentityId == playerIdentity.PlayerIdentityId);

            await ActAndAssertThatBowlingFiguresIdIsNull(statisticsRecords).ConfigureAwait(false);
        }

        private async Task ActAndAssertThatBowlingFiguresIdIsNull(IEnumerable<PlayerInMatchStatisticsRecord> statisticsRecords)
        {
            using (var connection = _databaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    var repo = new SqlServerStatisticsRepository(Mock.Of<IPlayerRepository>());
                    await repo.UpdatePlayerStatistics(statisticsRecords, transaction).ConfigureAwait(false);

                    foreach (var record in statisticsRecords)
                    {
                        var count = await transaction.Connection.QuerySingleAsync<int>(@$"SELECT COUNT(*) FROM {Tables.PlayerInMatchStatistics} 
                                WHERE PlayerInMatchStatisticsId = @PlayerInMatchStatisticsId 
                                  AND BowlingFiguresId IS NULL",
                                new
                                {
                                    record.PlayerInMatchStatisticsId
                                },
                                transaction).ConfigureAwait(false);

                        Assert.Equal(1, count);
                    }

                    transaction.Rollback();
                }
            }
        }


        public void Dispose() => _scope.Dispose();
    }
}
