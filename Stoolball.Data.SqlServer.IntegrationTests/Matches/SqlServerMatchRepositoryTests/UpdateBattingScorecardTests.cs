using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Moq;
using Stoolball.Data.SqlServer.IntegrationTests.Fixtures;
using Stoolball.Matches;
using Stoolball.Statistics;
using Xunit;

namespace Stoolball.Data.SqlServer.IntegrationTests.Matches.SqlServerMatchRepositoryTests
{
    [Collection(IntegrationTestConstants.TestDataIntegrationTestCollection)]
    public class UpdateBattingScorecardTests : MatchRepositoryTestsBase, IDisposable
    {
        public UpdateBattingScorecardTests(SqlServerTestDataFixture databaseFixture) : base(databaseFixture) { }


        delegate Task<PlayerIdentity> CreateOrMatchPlayerIdentityReturns(PlayerIdentity pi, Guid memberKey, string memberName, IDbTransaction transaction);

        [Theory]
        [InlineData(false, false, false, false)]
        [InlineData(true, false, false, false)]
        [InlineData(false, true, false, false)]
        [InlineData(false, false, true, false)]
        [InlineData(false, false, false, true)]
        [InlineData(true, true, true, true)]
        public async Task UpdateBattingScorecard_inserts_new_player_innings(bool hasFielder, bool hasBowler, bool hasRuns, bool hasBallsFaced)
        {
            var match = CloneValidMatch(DatabaseFixture.TestData.MatchInThePastWithFullDetails!);
            var possibleBatters = DatabaseFixture.TestData.PlayerIdentities.Where(x => x.Team!.TeamId == match.MatchInnings[0].BattingTeam!.Team!.TeamId);
            var possibleFielders = DatabaseFixture.TestData.PlayerIdentities.Where(x => x.Team!.TeamId == match.MatchInnings[0].BowlingTeam!.Team!.TeamId);

            var playerInnings = AddOneNewPlayerInnings(match.MatchInnings[0].PlayerInnings, possibleBatters, possibleFielders, hasFielder, hasBowler, hasRuns, hasBallsFaced);

            var returnedInnings = await Repository.UpdateBattingScorecard(match, match.MatchInnings[0].MatchInningsId!.Value, MemberKey, MemberName).ConfigureAwait(false);

            Assert.Equal(match.MatchInnings[0].PlayerInnings.Count, returnedInnings.PlayerInnings.Count);
            Assert.Equal(1, returnedInnings.PlayerInnings.Count(x =>
                x.BattingPosition == playerInnings.BattingPosition &&
                x.Batter!.PlayerIdentityId == playerInnings.Batter?.PlayerIdentityId &&
                x.DismissalType == playerInnings.DismissalType &&
                x.DismissedBy?.PlayerIdentityId == playerInnings.DismissedBy?.PlayerIdentityId &&
                x.Bowler?.PlayerIdentityId == playerInnings.Bowler?.PlayerIdentityId &&
                x.RunsScored == playerInnings.RunsScored &&
                x.BallsFaced == playerInnings.BallsFaced));

            using (var connection = DatabaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                var savedInnings = await connection.QuerySingleOrDefaultAsync<(Guid BatterPlayerIdentityId, int? BattingPosition, DismissalType DismissalType, Guid? DismissedByPlayerIdentityId, Guid? BowledByPlayerIdentityId, int? RunsScored, int? BallsFaced)?>(
                    @$"SELECT BatterPlayerIdentityId, BattingPosition, DismissalType, DismissedByPlayerIdentityId, BowlerPlayerIdentityId, RunsScored, BallsFaced 
                       FROM {Tables.PlayerInnings} 
                       WHERE MatchInningsId = @MatchInningsId
                       AND BattingPosition = @BattingPosition",
                    new
                    {
                        match.MatchInnings[0].MatchInningsId,
                        playerInnings.BattingPosition
                    }).ConfigureAwait(false);

                Assert.NotNull(savedInnings);
                Assert.Equal(playerInnings.Batter!.PlayerIdentityId, savedInnings.Value.BatterPlayerIdentityId);
                Assert.Equal(match.MatchInnings[0].PlayerInnings.Count, savedInnings.Value.BattingPosition);
                Assert.Equal(playerInnings.DismissalType, savedInnings.Value.DismissalType);
                Assert.Equal(playerInnings.DismissedBy?.PlayerIdentityId, savedInnings.Value.DismissedByPlayerIdentityId);
                Assert.Equal(playerInnings.Bowler?.PlayerIdentityId, savedInnings.Value.BowledByPlayerIdentityId);
                Assert.Equal(playerInnings.RunsScored, savedInnings.Value.RunsScored);
                Assert.Equal(playerInnings.BallsFaced, savedInnings.Value.BallsFaced);
            }
        }

        private PlayerInnings AddOneNewPlayerInnings(List<PlayerInnings> innings, IEnumerable<PlayerIdentity> possibleBatters, IEnumerable<PlayerIdentity> possibleFielders, bool hasFielder, bool hasBowler, bool hasRuns, bool hasBallsFaced)
        {
            var playerInnings = new PlayerInnings
            {
                BattingPosition = innings.Count + 1,
                Batter = possibleBatters.First(),
                DismissalType = DismissalType.RunOut,
                DismissedBy = hasFielder ? possibleFielders.First() : null,
                Bowler = hasBowler ? possibleFielders.Last() : null,
                RunsScored = hasRuns ? 57 : null,
                BallsFaced = hasBallsFaced ? 64 : null
            };
            innings.Add(playerInnings);

            return playerInnings;
        }

        [Theory]
        [InlineData(false, false, false, false, false, false)]
        [InlineData(true, false, false, false, false, false)]
        [InlineData(false, true, false, false, false, false)]
        [InlineData(false, false, true, false, false, false)]
        [InlineData(false, false, false, true, false, false)]
        [InlineData(false, false, false, false, true, false)]
        [InlineData(false, false, false, false, false, true)]
        public async Task UpdateBattingScorecard_updates_player_innings_previously_added(bool batterHasChanged, bool dismissalTypeHasChanged, bool fielderHasChanged, bool bowlerHasChanged, bool runsScoredHasChanged, bool ballsFacedHasChanged)
        {
            var modifiedMatch = CloneValidMatch(DatabaseFixture.TestData.MatchInThePastWithFullDetails!);
            var modifiedInnings = modifiedMatch.MatchInnings[0];
            var modifiedPlayerInnings = modifiedInnings.PlayerInnings.Last();
            var originalPlayerInnings = new PlayerInnings
            {
                PlayerInningsId = modifiedPlayerInnings.PlayerInningsId,
                BattingPosition = modifiedPlayerInnings.BattingPosition,
                Batter = modifiedPlayerInnings.Batter,
                DismissalType = modifiedPlayerInnings.DismissalType,
                DismissedBy = modifiedPlayerInnings.DismissedBy,
                Bowler = modifiedPlayerInnings.Bowler,
                RunsScored = modifiedPlayerInnings.RunsScored,
                BallsFaced = modifiedPlayerInnings.BallsFaced
            };


            if (batterHasChanged)
            {
                var possibleBatters = DatabaseFixture.TestData.PlayerIdentities.Where(x => x.Team!.TeamId == modifiedMatch.MatchInnings[0].BattingTeam!.Team!.TeamId);
                modifiedPlayerInnings.Batter = possibleBatters.First(x => x.PlayerIdentityId != modifiedPlayerInnings.Batter?.PlayerIdentityId);
            }
            if (dismissalTypeHasChanged) { modifiedPlayerInnings.DismissalType = modifiedPlayerInnings.DismissalType == DismissalType.Caught ? DismissalType.CaughtAndBowled : DismissalType.Caught; }
            if (fielderHasChanged || bowlerHasChanged)
            {
                var possibleFielders = DatabaseFixture.TestData.PlayerIdentities.Where(x => x.Team!.TeamId == modifiedMatch.MatchInnings[0].BowlingTeam!.Team!.TeamId);
                if (fielderHasChanged) { modifiedPlayerInnings.DismissedBy = possibleFielders.First(x => x.PlayerIdentityId != modifiedPlayerInnings.DismissedBy?.PlayerIdentityId); }
                if (bowlerHasChanged) { modifiedPlayerInnings.Bowler = possibleFielders.First(x => x.PlayerIdentityId != modifiedPlayerInnings.Bowler?.PlayerIdentityId); }
            }
            if (runsScoredHasChanged) { modifiedPlayerInnings.RunsScored = modifiedPlayerInnings.RunsScored.HasValue ? modifiedPlayerInnings.RunsScored + 1 : 60; }
            if (ballsFacedHasChanged) { modifiedPlayerInnings.BallsFaced = modifiedPlayerInnings.BallsFaced.HasValue ? modifiedPlayerInnings.BallsFaced + 1 : 70; }

            var result = await Repository.UpdateBattingScorecard(
                    modifiedMatch,
                    modifiedInnings.MatchInningsId!.Value,
                    MemberKey,
                    MemberName);

            Assert.Equal(modifiedInnings.PlayerInnings.Count, result.PlayerInnings.Count);

            using (var connection = DatabaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                var savedInnings = await connection.QuerySingleOrDefaultAsync<(Guid BatterPlayerIdentityId, int? BattingPosition, DismissalType DismissalType, Guid? DismissedByPlayerIdentityId, Guid? BowledByPlayerIdentityId, int? RunsScored, int? BallsFaced)?>(
                    @$"SELECT BatterPlayerIdentityId, BattingPosition, DismissalType, DismissedByPlayerIdentityId, BowlerPlayerIdentityId, RunsScored, BallsFaced 
                       FROM {Tables.PlayerInnings} 
                       WHERE PlayerInningsId = @PlayerInningsId",
                    modifiedPlayerInnings
                    ).ConfigureAwait(false);

                Assert.NotNull(savedInnings);
                Assert.Equal(modifiedPlayerInnings.Batter!.PlayerIdentityId, savedInnings.Value.BatterPlayerIdentityId);
                Assert.Equal(modifiedPlayerInnings.BattingPosition, savedInnings.Value.BattingPosition);
                Assert.Equal(modifiedPlayerInnings.DismissalType, savedInnings.Value.DismissalType);
                Assert.Equal(modifiedPlayerInnings.DismissedBy?.PlayerIdentityId, savedInnings.Value.DismissedByPlayerIdentityId);
                Assert.Equal(modifiedPlayerInnings.Bowler?.PlayerIdentityId, savedInnings.Value.BowledByPlayerIdentityId);
                Assert.Equal(modifiedPlayerInnings.RunsScored, savedInnings.Value.RunsScored);
                Assert.Equal(modifiedPlayerInnings.BallsFaced, savedInnings.Value.BallsFaced);
            }
        }

        [Fact]
        public async Task UpdateBattingScorecard_deletes_player_innings_removed_from_scorecard()
        {
            var repository = CreateRepository(new SqlServerStatisticsRepository(PlayerRepository));

            var modifiedMatch = CloneValidMatch(DatabaseFixture.TestData.MatchInThePastWithFullDetails!);
            var modifiedInnings = modifiedMatch.MatchInnings[0];

            var playerInningsToRemove = modifiedInnings.PlayerInnings.Last();
            modifiedInnings.PlayerInnings.Remove(playerInningsToRemove);

            var result = await repository.UpdateBattingScorecard(
                    modifiedMatch,
                    modifiedInnings.MatchInningsId!.Value,
                    MemberKey,
                    MemberName);

            Assert.Equal(modifiedInnings.PlayerInnings.Count, result.PlayerInnings.Count);

            using (var connection = DatabaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                var savedInningsId = await connection.QuerySingleOrDefaultAsync<Guid?>(
                    $"SELECT PlayerInningsId FROM {Tables.PlayerInnings} WHERE PlayerInningsId = @PlayerInningsId",
                    playerInningsToRemove).ConfigureAwait(false);

                Assert.Null(savedInningsId);
            }
        }

        [Fact]
        public async Task UpdateBattingScorecard_unchanged_player_innings_are_retained()
        {
            var match = DatabaseFixture.TestData.MatchInThePastWithFullDetails!;
            var innings = match.MatchInnings.First(x => x.PlayerInnings.Count > 0);

            var result = await Repository.UpdateBattingScorecard(
                    match,
                    innings.MatchInningsId!.Value,
                    MemberKey,
                    MemberName);

            Assert.Equal(innings.PlayerInnings.Count, result.PlayerInnings.Count);

            using (var connection = DatabaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                foreach (var playerInnings in innings.PlayerInnings)
                {
                    var savedInnings = await connection.QuerySingleOrDefaultAsync<(Guid BatterPlayerIdentityId, DismissalType DismissalType, Guid? DismissedByPlayerIdentityId, Guid? BowledByPlayerIdentityId, int? RunsScored, int? BallsFaced)?>(
                                        @$"SELECT BatterPlayerIdentityId, DismissalType, DismissedByPlayerIdentityId, BowlerPlayerIdentityId, RunsScored, BallsFaced 
                       FROM {Tables.PlayerInnings} 
                       WHERE PlayerInningsId = @PlayerInningsId",
                       playerInnings).ConfigureAwait(false);

                    Assert.NotNull(savedInnings);
                    Assert.Equal(playerInnings.Batter!.PlayerIdentityId, savedInnings.Value.BatterPlayerIdentityId);
                    Assert.Equal(playerInnings.DismissalType, savedInnings.Value.DismissalType);
                    Assert.Equal(playerInnings.DismissedBy?.PlayerIdentityId, savedInnings.Value.DismissedByPlayerIdentityId);
                    Assert.Equal(playerInnings.Bowler?.PlayerIdentityId, savedInnings.Value.BowledByPlayerIdentityId);
                    Assert.Equal(playerInnings.RunsScored, savedInnings.Value.RunsScored);
                    Assert.Equal(playerInnings.BallsFaced, savedInnings.Value.BallsFaced);
                }
            }
        }

        /// <summary>
        /// UpdateBattingScorecard expected the BowlingFiguresId in its input to be unchanged for existing bowlers and pre-generated for new bowlers. #660
        /// </summary>
        [Fact]
        public async Task UpdateBattingScorecard_with_incorrect_BowlingFiguresId_for_existing_bowler_updates_extras_and_final_score()
        {
            int byes = 5, wides = 10, noBalls = 15, bonus = 20, runs = 140, wickets = 8;
            Func<PlayerInnings, bool> selectPlayerInnings = pi => pi.Bowler is not null;
            Func<MatchInnings, bool> selectMatchInnings = mi => mi.Byes != byes && mi.Wides != wides && mi.NoBalls != noBalls && mi.BonusOrPenaltyRuns != bonus && mi.Runs != runs && mi.Wickets != wickets
                                                             && mi.PlayerInnings.Any(selectPlayerInnings);

            var match = DatabaseFixture.TestData.Matches.First(m => m.MatchInnings.Any(selectMatchInnings));
            var clonedMatch = CloneValidMatch(match);
            var matchInnings = clonedMatch.MatchInnings.First(selectMatchInnings);

            var bowler = matchInnings.PlayerInnings.First(selectPlayerInnings).Bowler;
            matchInnings.BowlingFigures.Single(bf => bf.Bowler!.PlayerIdentityId == bowler!.PlayerIdentityId).BowlingFiguresId = Guid.NewGuid();

            await TestUpdateExtrasAndFinalScore(byes, wides, noBalls, bonus, runs, wickets, clonedMatch, matchInnings).ConfigureAwait(false);
        }

        [Theory]
        [InlineData(null, null, null, null, null, null)]
        [InlineData(5, 10, 15, 20, 140, 8)]
        public async Task UpdateBattingScorecard_updates_extras_and_final_score(int? byes, int? wides, int? noBalls, int? bonus, int? runs, int? wickets)
        {
            Func<MatchInnings, bool> inningsSelector = mi => mi.Byes != byes && mi.Wides != wides && mi.NoBalls != noBalls && mi.BonusOrPenaltyRuns != bonus && mi.Runs != runs && mi.Wickets != wickets;

            var match = DatabaseFixture.TestData.Matches.First(m => m.MatchInnings.Any(inningsSelector));
            var clonedMatch = CloneValidMatch(match);
            var innings = clonedMatch.MatchInnings.First(inningsSelector);

            await TestUpdateExtrasAndFinalScore(byes, wides, noBalls, bonus, runs, wickets, clonedMatch, innings).ConfigureAwait(false);
        }

        private async Task TestUpdateExtrasAndFinalScore(int? byes, int? wides, int? noBalls, int? bonus, int? runs, int? wickets, Stoolball.Matches.Match clonedMatch, MatchInnings innings)
        {
            innings.Byes = byes;
            innings.Wides = wides;
            innings.NoBalls = noBalls;
            innings.BonusOrPenaltyRuns = bonus;
            innings.Runs = runs;
            innings.Wickets = wickets;

            var returnedInnings = await Repository.UpdateBattingScorecard(clonedMatch, innings.MatchInningsId!.Value, MemberKey, MemberName).ConfigureAwait(false);

            Assert.NotNull(returnedInnings);
            Assert.Equal(innings.MatchInningsId, returnedInnings.MatchInningsId);
            Assert.Equal(byes, returnedInnings.Byes);
            Assert.Equal(wides, returnedInnings.Wides);
            Assert.Equal(noBalls, returnedInnings.NoBalls);
            Assert.Equal(bonus, returnedInnings.BonusOrPenaltyRuns);
            Assert.Equal(runs, returnedInnings.Runs);
            Assert.Equal(wickets, returnedInnings.Wickets);

            using (var connection = DatabaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                var savedInnings = await connection.QuerySingleOrDefaultAsync<MatchInnings>(
                    @$"SELECT Byes, Wides, NoBalls, BonusOrPenaltyRuns, Runs, Wickets 
                       FROM {Tables.MatchInnings} 
                       WHERE MatchInningsId = @MatchInningsId",
                    new
                    {
                        innings.MatchInningsId
                    }).ConfigureAwait(false);

                Assert.NotNull(savedInnings);
                Assert.Equal(byes, savedInnings.Byes);
                Assert.Equal(wides, savedInnings.Wides);
                Assert.Equal(noBalls, savedInnings.NoBalls);
                Assert.Equal(bonus, savedInnings.BonusOrPenaltyRuns);
                Assert.Equal(runs, savedInnings.Runs);
                Assert.Equal(wickets, savedInnings.Wickets);
            }
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(0)]
        [InlineData(1)]
        public async Task UpdateBattingScorecard_updates_players_per_team_for_match(int numberOfInningsComparedToPlayersPerTeam)
        {
            var repository = CreateRepository(new SqlServerStatisticsRepository(PlayerRepository));

            var match = CloneValidMatch(DatabaseFixture.TestData.MatchInThePastWithFullDetails!);
            var possibleBatters = DatabaseFixture.TestData.PlayerIdentities.Where(x => x.Team!.TeamId == match.MatchInnings[0].BattingTeam!.Team!.TeamId);
            var possibleFielders = DatabaseFixture.TestData.PlayerIdentities.Where(x => x.Team!.TeamId == match.MatchInnings[0].BowlingTeam!.Team!.TeamId);

            var expectedPlayersPerTeam = match.PlayersPerTeam;
            if (numberOfInningsComparedToPlayersPerTeam > expectedPlayersPerTeam) { expectedPlayersPerTeam += numberOfInningsComparedToPlayersPerTeam; }

            if (numberOfInningsComparedToPlayersPerTeam >= 0)
            {
                while (match.MatchInnings[0].PlayerInnings.Count < expectedPlayersPerTeam)
                {
                    _ = AddOneNewPlayerInnings(match.MatchInnings[0].PlayerInnings, possibleBatters, possibleFielders, true, true, true, true);
                }
            }
            else
            {
                while (match.MatchInnings[0].PlayerInnings.Count >= expectedPlayersPerTeam)
                {
                    match.MatchInnings[0].PlayerInnings.RemoveAt(match.MatchInnings[0].PlayerInnings.Count - 1);
                }
            }

            _ = await repository.UpdateBattingScorecard(match, match.MatchInnings[0].MatchInningsId!.Value, MemberKey, MemberName).ConfigureAwait(false);

            using (var connection = DatabaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                var savedPlayersPerTeam = await connection.QuerySingleOrDefaultAsync<int>(
                    @$"SELECT PlayersPerTeam
                       FROM {Tables.Match} 
                       WHERE MatchId = @MatchId",
                    match).ConfigureAwait(false);

                Assert.Equal(expectedPlayersPerTeam, savedPlayersPerTeam);
            }
        }

        [Theory]
        [InlineData(false, false, false, false, false, false, false, false, false, false, false, false)]
        [InlineData(true, false, false, false, false, false, false, false, false, false, false, false)]
        [InlineData(false, true, false, false, false, false, false, false, false, false, false, false)]
        [InlineData(false, false, true, false, false, false, false, false, false, false, false, false)]
        [InlineData(false, false, false, true, false, false, false, false, false, false, false, false)]
        [InlineData(false, false, false, false, true, false, false, false, false, false, false, false)]
        [InlineData(false, false, false, false, false, true, false, false, false, false, false, false)]
        [InlineData(false, false, false, false, false, false, true, false, false, false, false, false)]
        [InlineData(false, false, false, false, false, false, false, true, false, false, false, false)]
        [InlineData(false, false, false, false, false, false, false, false, true, false, false, false)]
        [InlineData(false, false, false, false, false, false, false, false, false, true, false, false)]
        [InlineData(false, false, false, false, false, false, false, false, false, false, true, false)]
        [InlineData(false, false, false, false, false, false, false, false, false, false, false, true)]
        public async Task UpdateBattingScorecard_updates_bowling_figures_and_player_statistics_if_data_has_changed(
            bool batterHasChanged, bool dismissalTypeHasChanged, bool fielderHasChanged, bool bowlerHasChanged, bool runsScoredHasChanged, bool ballsFacedHasChanged,
            bool byesHasChanged, bool widesHasChanged, bool noBallsHasChanged, bool bonusHasChanged, bool teamRunsHasChanged, bool teamWicketsHasChanged
            )
        {
            var modifiedMatch = CloneValidMatch(DatabaseFixture.TestData.MatchInThePastWithFullDetails!);
            var modifiedMatchInnings = modifiedMatch.MatchInnings[0];
            var modifiedPlayerInnings = modifiedMatchInnings.PlayerInnings.Last();
            var originalPlayerInnings = new PlayerInnings
            {
                PlayerInningsId = modifiedPlayerInnings.PlayerInningsId,
                BattingPosition = modifiedPlayerInnings.BattingPosition,
                Batter = modifiedPlayerInnings.Batter,
                DismissalType = modifiedPlayerInnings.DismissalType,
                DismissedBy = modifiedPlayerInnings.DismissedBy,
                Bowler = modifiedPlayerInnings.Bowler,
                RunsScored = modifiedPlayerInnings.RunsScored,
                BallsFaced = modifiedPlayerInnings.BallsFaced
            };
            var playerInningsHasChanged = batterHasChanged || dismissalTypeHasChanged || fielderHasChanged || bowlerHasChanged || runsScoredHasChanged || ballsFacedHasChanged;
            var matchInningsHasChanged = byesHasChanged || widesHasChanged || noBallsHasChanged || bonusHasChanged || teamRunsHasChanged || teamWicketsHasChanged;
            var anythingHasChanged = playerInningsHasChanged || matchInningsHasChanged;

            if (anythingHasChanged)
            {
                if (playerInningsHasChanged)
                {
                    var possibleBatters = DatabaseFixture.TestData.PlayerIdentities.Where(x => x.Team!.TeamId == modifiedMatch.MatchInnings[0].BattingTeam!.Team!.TeamId);
                    var possibleFielders = DatabaseFixture.TestData.PlayerIdentities.Where(x => x.Team!.TeamId == modifiedMatch.MatchInnings[0].BowlingTeam!.Team!.TeamId);

                    if (batterHasChanged) { modifiedPlayerInnings.Batter = possibleBatters.First(x => x.PlayerIdentityId != modifiedPlayerInnings.Batter!.PlayerIdentityId); }
                    if (dismissalTypeHasChanged) { modifiedPlayerInnings.DismissalType = modifiedPlayerInnings.DismissalType == DismissalType.Caught ? DismissalType.CaughtAndBowled : DismissalType.Caught; }
                    if (fielderHasChanged) { modifiedPlayerInnings.DismissedBy = possibleFielders.First(x => x.PlayerIdentityId != modifiedPlayerInnings.DismissedBy?.PlayerIdentityId); }
                    if (bowlerHasChanged) { modifiedPlayerInnings.Bowler = possibleFielders.First(x => x.PlayerIdentityId != modifiedPlayerInnings.Bowler?.PlayerIdentityId); }
                    if (runsScoredHasChanged) { modifiedPlayerInnings.RunsScored = modifiedPlayerInnings.RunsScored.HasValue ? modifiedPlayerInnings.RunsScored + 1 : 60; }
                    if (ballsFacedHasChanged) { modifiedPlayerInnings.BallsFaced = modifiedPlayerInnings.BallsFaced.HasValue ? modifiedPlayerInnings.BallsFaced + 1 : 70; }
                }

                if (byesHasChanged) { modifiedMatchInnings.Byes = modifiedMatchInnings.Byes.HasValue ? modifiedMatchInnings.Byes + 1 : 10; }
                if (widesHasChanged) { modifiedMatchInnings.Wides = modifiedMatchInnings.Wides.HasValue ? modifiedMatchInnings.Wides + 1 : 9; }
                if (noBallsHasChanged) { modifiedMatchInnings.NoBalls = modifiedMatchInnings.NoBalls.HasValue ? modifiedMatchInnings.NoBalls + 1 : 7; }
                if (bonusHasChanged) { modifiedMatchInnings.BonusOrPenaltyRuns = modifiedMatchInnings.BonusOrPenaltyRuns.HasValue ? modifiedMatchInnings.BonusOrPenaltyRuns + 1 : 5; }
                if (teamRunsHasChanged) { modifiedMatchInnings.Runs = modifiedMatchInnings.Runs.HasValue ? modifiedMatchInnings.Runs + 1 : 110; }
                if (teamWicketsHasChanged) { modifiedMatchInnings.Wickets = modifiedMatchInnings.Wickets.HasValue ? modifiedMatchInnings.Wickets + 1 : 9; }
            }

            _ = await Repository.UpdateBattingScorecard(modifiedMatch, modifiedMatchInnings.MatchInningsId!.Value, MemberKey, MemberName).ConfigureAwait(false);

            StatisticsRepository.Verify(x => x.UpdateBowlingFigures(It.Is<MatchInnings>(mi => mi.MatchInningsId == modifiedMatchInnings.MatchInningsId), MemberKey, MemberName, It.IsAny<IDbTransaction>()), bowlerHasChanged ? Times.Once() : Times.Never());
            StatisticsRepository.Verify(x => x.UpdatePlayerStatistics(It.IsAny<IEnumerable<PlayerInMatchStatisticsRecord>>(), It.IsAny<IDbTransaction>()), anythingHasChanged ? Times.Once() : Times.Never());
        }

        [Fact]
        public async Task UpdateBattingScorecard_deletes_obsolete_player_removed_as_a_batter()
        {
            // This should take place async to avoid timeouts updating the match. Consider what would happen if the player were used again before the async update.
            var repository = CreateRepository(new SqlServerStatisticsRepository(PlayerRepository));

            // Find a player identity who we only record as having batted once
            var matchInnings = DatabaseFixture.TestData.Matches.SelectMany(m => m.MatchInnings);
            var identityOnlyRecordedAsBattingOnce = DatabaseFixture.TestData.PlayerIdentities.First(
                    x => matchInnings.SelectMany(mi => mi.PlayerInnings.Where(pi => pi.Batter?.PlayerIdentityId == x.PlayerIdentityId)).Count() == 1 &&
                                                       !matchInnings.Any(mi => mi.PlayerInnings.Any(pi => pi.DismissedBy?.PlayerIdentityId == x.PlayerIdentityId ||
                                                                                                          pi.Bowler?.PlayerIdentityId == x.PlayerIdentityId)) &&
                                                       !matchInnings.Any(mi => mi.OversBowled.Any(pi => pi.Bowler?.PlayerIdentityId == x.PlayerIdentityId)) &&
                                                       !DatabaseFixture.TestData.Matches.SelectMany(x => x.Awards).Any(aw => aw.PlayerIdentity?.PlayerIdentityId == x.PlayerIdentityId)
                                                    );

            // Copy the match where that identity batted
            var match = CloneValidMatch(DatabaseFixture.TestData.Matches.Single(m => m.MatchInnings.Any(mi => mi.PlayerInnings.Any(pi => pi.Batter?.PlayerIdentityId == identityOnlyRecordedAsBattingOnce.PlayerIdentityId))));

            // Remove the innings from the copy
            var innings = match.MatchInnings.Single(mi => mi.PlayerInnings.Any(o => o.Batter?.PlayerIdentityId == identityOnlyRecordedAsBattingOnce.PlayerIdentityId));
            var playerInnings = innings.PlayerInnings.Single(o => o.Batter?.PlayerIdentityId == identityOnlyRecordedAsBattingOnce.PlayerIdentityId);
            innings.PlayerInnings.Remove(playerInnings);

            // Act
            var result = await repository.UpdateBattingScorecard(
                    match,
                    innings.MatchInningsId!.Value,
                    MemberKey,
                    MemberName);
            await PlayerRepository.ProcessAsyncUpdatesForPlayers();

            // Assert
            using (var connection = DatabaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                var savedPlayerIdentity = await connection.QuerySingleOrDefaultAsync<Guid?>(
                   $@"SELECT PlayerIdentityId FROM {Tables.PlayerIdentity} WHERE PlayerIdentityId = @PlayerIdentityId",
                   new { identityOnlyRecordedAsBattingOnce.PlayerIdentityId }).ConfigureAwait(false);

                Assert.Null(savedPlayerIdentity);

                var savedPlayer = await connection.QuerySingleOrDefaultAsync<Guid?>(
                   $@"SELECT PlayerId FROM {Tables.Player} WHERE PlayerId = @PlayerId",
                   new { identityOnlyRecordedAsBattingOnce.Player!.PlayerId }).ConfigureAwait(false);

                Assert.Null(savedPlayer);
            }
        }

        [Fact]
        public async Task UpdateBattingScorecard_deletes_obsolete_player_removed_as_a_fielder()
        {
            // This should take place async to avoid timeouts updating the match. Consider what would happen if the player were used again before the async update.
            var repository = CreateRepository(new SqlServerStatisticsRepository(PlayerRepository));

            // Find a player identity who we only record as having fielded once
            var matchInnings = DatabaseFixture.TestData.Matches.SelectMany(m => m.MatchInnings);
            var identityOnlyRecordedAsFieldingOnce = DatabaseFixture.TestData.PlayerIdentities.First(
                    x => matchInnings.SelectMany(mi => mi.PlayerInnings.Where(pi => pi.DismissedBy?.PlayerIdentityId == x.PlayerIdentityId)).Count() == 1 &&
                                                       !matchInnings.Any(mi => mi.PlayerInnings.Any(pi => pi.Batter?.PlayerIdentityId == x.PlayerIdentityId ||
                                                                                                          pi.Bowler?.PlayerIdentityId == x.PlayerIdentityId)) &&
                                                       !matchInnings.Any(mi => mi.OversBowled.Any(pi => pi.Bowler?.PlayerIdentityId == x.PlayerIdentityId)) &&
                                                       !DatabaseFixture.TestData.Matches.SelectMany(x => x.Awards).Any(aw => aw.PlayerIdentity?.PlayerIdentityId == x.PlayerIdentityId)
                                                    );

            // Copy the match where that identity fielded
            var match = CloneValidMatch(DatabaseFixture.TestData.Matches.Single(m => m.MatchInnings.Any(mi => mi.PlayerInnings.Any(pi => pi.DismissedBy?.PlayerIdentityId == identityOnlyRecordedAsFieldingOnce.PlayerIdentityId))));

            // Remove the innings from the copy
            var innings = match.MatchInnings.Single(mi => mi.PlayerInnings.Any(o => o.DismissedBy?.PlayerIdentityId == identityOnlyRecordedAsFieldingOnce.PlayerIdentityId));
            var playerInnings = innings.PlayerInnings.Single(o => o.DismissedBy?.PlayerIdentityId == identityOnlyRecordedAsFieldingOnce.PlayerIdentityId);
            innings.PlayerInnings.Remove(playerInnings);

            // Act
            var result = await repository.UpdateBattingScorecard(
                    match,
                    innings.MatchInningsId!.Value,
                    MemberKey,
                    MemberName);
            await PlayerRepository.ProcessAsyncUpdatesForPlayers();

            // Assert
            using (var connection = DatabaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                var savedPlayerIdentity = await connection.QuerySingleOrDefaultAsync<Guid?>(
                   $@"SELECT PlayerIdentityId FROM {Tables.PlayerIdentity} WHERE PlayerIdentityId = @PlayerIdentityId",
                   new { identityOnlyRecordedAsFieldingOnce.PlayerIdentityId }).ConfigureAwait(false);

                Assert.Null(savedPlayerIdentity);

                var savedPlayer = await connection.QuerySingleOrDefaultAsync<Guid?>(
                   $@"SELECT PlayerId FROM {Tables.Player} WHERE PlayerId = @PlayerId",
                   new { identityOnlyRecordedAsFieldingOnce.Player!.PlayerId }).ConfigureAwait(false);

                Assert.Null(savedPlayer);
            }
        }

        [Fact]
        public async Task UpdateBattingScorecard_deletes_obsolete_player_removed_as_a_bowler()
        {
            // This should take place async to avoid timeouts updating the match. Consider what would happen if the player were used again before the async update.
            var repository = CreateRepository(new SqlServerStatisticsRepository(PlayerRepository));

            // Find a player identity who we only record as having taken a wicket once
            var matchInnings = DatabaseFixture.TestData.Matches.SelectMany(m => m.MatchInnings);
            var identitiesOnlyRecordedAsTakingAWicketOnce = DatabaseFixture.TestData.PlayerIdentities.Where(
                    x => matchInnings.SelectMany(mi => mi.PlayerInnings.Where(pi => pi.Bowler?.PlayerIdentityId == x.PlayerIdentityId)).Count() == 1 &&
                                                       !matchInnings.Any(mi => mi.PlayerInnings.Any(pi => pi.Batter?.PlayerIdentityId == x.PlayerIdentityId ||
                                                                                                          pi.DismissedBy?.PlayerIdentityId == x.PlayerIdentityId)) &&
                                                       !matchInnings.Any(mi => mi.OversBowled.Any(pi => pi.Bowler?.PlayerIdentityId == x.PlayerIdentityId)) &&
                                                       !DatabaseFixture.TestData.Matches.SelectMany(x => x.Awards).Any(aw => aw.PlayerIdentity?.PlayerIdentityId == x.PlayerIdentityId)
                                                    );

            foreach (var identityOnlyRecordedAsTakingAWicketOnce in identitiesOnlyRecordedAsTakingAWicketOnce)
            {
                // Copy the match where that identity took a wicket
                var match = CloneValidMatch(DatabaseFixture.TestData.Matches.Single(m => m.MatchInnings.Any(mi => mi.PlayerInnings.Any(pi => pi.Bowler?.PlayerIdentityId == identityOnlyRecordedAsTakingAWicketOnce.PlayerIdentityId))));

                // Remove the record of the identity from the copy
                var innings = match.MatchInnings.Single(mi => mi.PlayerInnings.Any(o => o.Bowler?.PlayerIdentityId == identityOnlyRecordedAsTakingAWicketOnce.PlayerIdentityId));
                var playerInnings = innings.PlayerInnings.Single(o => o.Bowler?.PlayerIdentityId == identityOnlyRecordedAsTakingAWicketOnce.PlayerIdentityId);
                innings.PlayerInnings.Remove(playerInnings);

                var bowlingFigures = innings.BowlingFigures.Single(bf => bf.Bowler!.PlayerIdentityId == identityOnlyRecordedAsTakingAWicketOnce.PlayerIdentityId);
                innings.BowlingFigures.Remove(bowlingFigures);

                // Act
                var result = await repository.UpdateBattingScorecard(
                        match,
                        innings.MatchInningsId!.Value,
                        MemberKey,
                        MemberName);
                await PlayerRepository.ProcessAsyncUpdatesForPlayers();

                // Assert
                using (var connection = DatabaseFixture.ConnectionFactory.CreateDatabaseConnection())
                {
                    var savedPlayerIdentity = await connection.QuerySingleOrDefaultAsync<Guid?>(
                       $@"SELECT PlayerIdentityId FROM {Tables.PlayerIdentity} WHERE PlayerIdentityId = @PlayerIdentityId",
                       new { identityOnlyRecordedAsTakingAWicketOnce.PlayerIdentityId }).ConfigureAwait(false);

                    Assert.Null(savedPlayerIdentity);

                    var savedPlayer = await connection.QuerySingleOrDefaultAsync<Guid?>(
                       $@"SELECT PlayerId FROM {Tables.Player} WHERE PlayerId = @PlayerId",
                       new { identityOnlyRecordedAsTakingAWicketOnce.Player!.PlayerId }).ConfigureAwait(false);

                    Assert.Null(savedPlayer);
                }
            }
        }
    }
}
