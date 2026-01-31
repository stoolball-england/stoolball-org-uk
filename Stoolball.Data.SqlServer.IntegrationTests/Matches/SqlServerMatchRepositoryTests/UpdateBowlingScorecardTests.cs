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
    public class UpdateBowlingScorecardTests : MatchRepositoryTestsBase, IDisposable
    {
        public UpdateBowlingScorecardTests(SqlServerTestDataFixture databaseFixture) : base(databaseFixture) { }

        [Fact]
        public async Task UpdateBowlingScorecard_inserts_new_overs_and_extends_the_final_overset()
        {
            var modifiedMatch = CloneValidMatch(DatabaseFixture.TestData.MatchInThePastWithFullDetails!);
            var modifiedInnings = modifiedMatch.MatchInnings[0];

            var totalOversInOversets = modifiedInnings.OverSets.Sum(o => o.Overs);
            var finalOverSet = modifiedInnings.OverSets.Last();
            var totalOvers = modifiedInnings.OversBowled.Count;
            var oversAdded = new List<Over>();

            do
            {
                AddOneNewBowlingOver(modifiedInnings);
                oversAdded.Add(modifiedInnings.OversBowled.Last());
                totalOvers++;
            }
            while (totalOvers <= totalOversInOversets);

            var result = await Repository.UpdateBowlingScorecard(
                    modifiedMatch,
                    modifiedInnings.MatchInningsId!.Value,
                    MemberKey,
                    MemberName);

            Assert.Equal(modifiedInnings.OversBowled.Count, result.OversBowled.Count);

            using (var connection = DatabaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                Assert.True(oversAdded.Any());
                foreach (var over in oversAdded)
                {
                    var savedOver = await connection.QuerySingleOrDefaultAsync<(Guid? OverSetId, Guid? BowlerId, int? BallsBowled, int? Wides, int? NoBalls, int? RunsConceded)>(
                       $@"SELECT OverSetId, BowlerPlayerIdentityId, BallsBowled, Wides, NoBalls, RunsConceded 
                          FROM {Tables.Over} 
                          WHERE MatchInningsId = @MatchInningsId AND OverNumber = @OverNumber",
                       new { modifiedInnings.MatchInningsId, over.OverNumber }).ConfigureAwait(false);

                    Assert.Equal(over.OverSet!.OverSetId, savedOver.OverSetId);
                    Assert.Equal(over.Bowler!.PlayerIdentityId, savedOver.BowlerId);
                    Assert.Equal(over.BallsBowled, savedOver.BallsBowled);
                    Assert.Equal(over.Wides, savedOver.Wides);
                    Assert.Equal(over.NoBalls, savedOver.NoBalls);
                    Assert.Equal(over.RunsConceded, savedOver.RunsConceded);

                    var oversInOverSet = await connection.QuerySingleOrDefaultAsync<int?>($"SELECT Overs FROM {Tables.OverSet} WHERE OverSetId = @OverSetId",
                        finalOverSet).ConfigureAwait(false);

                    Assert.Equal(finalOverSet.Overs + 1, oversInOverSet);
                }
            }
        }

        [Theory]
        [InlineData(false, false, false, false, false)]
        public async Task UpdateBowlingScorecard_updates_overs_previously_added(bool bowlerHasChanged, bool ballsBowledHasChanged, bool widesHasChanged, bool noBallsHasChanged, bool runsConcededHasChanged)
        {
            var modifiedMatch = CloneValidMatch(DatabaseFixture.TestData.MatchInThePastWithFullDetails!);
            var modifiedInnings = modifiedMatch.MatchInnings[0];
            var modifiedOver = modifiedInnings.OversBowled.Last();
            if (bowlerHasChanged)
            {
                modifiedOver.Bowler = DatabaseFixture.TestData.PlayerIdentities.First(x => x.Team!.TeamId == modifiedInnings.BowlingTeam!.Team!.TeamId && x.PlayerIdentityId != modifiedOver.Bowler?.PlayerIdentityId);
            }
            if (ballsBowledHasChanged) { modifiedOver.BallsBowled = modifiedOver.BallsBowled.HasValue ? modifiedOver.BallsBowled + 1 : 8; }
            if (widesHasChanged) { modifiedOver.Wides = modifiedOver.Wides.HasValue ? modifiedOver.Wides + 1 : 4; }
            if (noBallsHasChanged) { modifiedOver.NoBalls = modifiedOver.NoBalls.HasValue ? modifiedOver.NoBalls + 1 : 5; }
            if (runsConcededHasChanged) { modifiedOver.RunsConceded = modifiedOver.RunsConceded.HasValue ? modifiedOver.RunsConceded + 1 : 12; }

            var result = await Repository.UpdateBowlingScorecard(
                    modifiedMatch,
                    modifiedInnings.MatchInningsId!.Value,
                    MemberKey,
                    MemberName);

            Assert.Equal(modifiedInnings.OversBowled.Count, result.OversBowled.Count);

            using (var connection = DatabaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                var savedOver = await connection.QuerySingleOrDefaultAsync<(int? OverNumber, Guid? OverSetId, Guid? BowlerId, int? BallsBowled, int? Wides, int? NoBalls, int? RunsConceded)>(
                   $"SELECT OverNumber, OverSetId, BowlerPlayerIdentityId, BallsBowled, Wides, NoBalls, RunsConceded FROM {Tables.Over} WHERE OverId = @OverId",
                   new { modifiedOver.OverId }).ConfigureAwait(false);

                Assert.Equal(modifiedOver.OverNumber, savedOver.OverNumber);
                Assert.Equal(modifiedOver.OverSet!.OverSetId, savedOver.OverSetId);
                Assert.Equal(modifiedOver.Bowler!.PlayerIdentityId, savedOver.BowlerId);
                Assert.Equal(modifiedOver.BallsBowled, savedOver.BallsBowled);
                Assert.Equal(modifiedOver.Wides, savedOver.Wides);
                Assert.Equal(modifiedOver.NoBalls, savedOver.NoBalls);
                Assert.Equal(modifiedOver.RunsConceded, savedOver.RunsConceded);
            }
        }

        [Fact]
        public async Task UpdateBowlingScorecard_deletes_overs_removed_from_scorecard()
        {
            var modifiedMatch = CloneValidMatch(DatabaseFixture.TestData.MatchInThePastWithFullDetails!);
            var modifiedInnings = modifiedMatch.MatchInnings[0];

            var overToRemove = modifiedInnings.OversBowled.Last();
            modifiedInnings.OversBowled.Remove(overToRemove);

            var result = await Repository.UpdateBowlingScorecard(
                    modifiedMatch,
                    modifiedInnings.MatchInningsId!.Value,
                    MemberKey,
                    MemberName);

            Assert.Equal(modifiedInnings.OversBowled.Count, result.OversBowled.Count);

            using (var connection = DatabaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                var savedOverId = await connection.QuerySingleOrDefaultAsync<Guid?>(
                    $"SELECT OverId FROM {Tables.Over} WHERE OverId = @OverId",
                     overToRemove).ConfigureAwait(false);

                Assert.Null(savedOverId);
            }
        }

        [Fact]
        public async Task UpdateBowlingScorecard_retains_unchanged_overs()
        {
            var match = DatabaseFixture.TestData.MatchInThePastWithFullDetails!;
            var innings = match.MatchInnings.First(x => x.OversBowled.Count > 0);

            var result = await Repository.UpdateBowlingScorecard(
                    match,
                    innings.MatchInningsId!.Value,
                    MemberKey,
                    MemberName);

            Assert.Equal(innings.OversBowled.Count, result.OversBowled.Count);

            using (var connection = DatabaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                foreach (var over in innings.OversBowled)
                {
                    var savedOver = await connection.QuerySingleOrDefaultAsync<(Guid OverId, int OverNumber, Guid BowlerPlayerIdentityId, int? BallsBowled, int? NoBalls, int? Wides, int? RunsConceded)?>(
                        $"SELECT OverId, OverNumber, BowlerPlayerIdentityId, BallsBowled, NoBalls, Wides, RunsConceded FROM {Tables.Over} WHERE OverId = @OverId",
                        new
                        {
                            over.OverId,
                            over.OverNumber,
                            over.Bowler!.PlayerIdentityId,
                            over.BallsBowled,
                            over.NoBalls,
                            over.Wides,
                            over.RunsConceded
                        }).ConfigureAwait(false);

                    // Don't check OverSetId because that could legitimately change if other overs were added or removed.
                    // For example, two sets of five overs. One over removed from the start and over six becomes over five, in a different set.
                    Assert.NotNull(savedOver);
                    Assert.Equal(over.OverNumber, savedOver.Value.OverNumber);
                    Assert.Equal(over.Bowler.PlayerIdentityId, savedOver.Value.BowlerPlayerIdentityId);
                    Assert.Equal(over.BallsBowled, savedOver.Value.BallsBowled);
                    Assert.Equal(over.NoBalls, savedOver.Value.NoBalls);
                    Assert.Equal(over.Wides, savedOver.Value.Wides);
                    Assert.Equal(over.RunsConceded, savedOver.Value.RunsConceded);
                }
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task UpdateBowlingScorecard_updates_bowling_figures_and_player_statistics_if_bowling_has_changed(bool bowlingHasChanged)
        {
            var modifiedMatch = CloneValidMatch(DatabaseFixture.TestData.MatchInThePastWithFullDetails!);
            var modifiedInnings = modifiedMatch.MatchInnings[0];

            if (bowlingHasChanged)
            {
                AddOneNewBowlingOver(modifiedInnings);
            }

            _ = await Repository.UpdateBowlingScorecard(
                    modifiedMatch,
                    modifiedInnings.MatchInningsId!.Value,
                    MemberKey,
                    MemberName);

            StatisticsRepository.Verify(x => x.UpdateBowlingFigures(It.Is<MatchInnings>(mi => mi.MatchInningsId == modifiedInnings.MatchInningsId), MemberKey, MemberName, It.IsAny<IDbTransaction>()), bowlingHasChanged ? Times.Once() : Times.Never());
            StatisticsRepository.Verify(x => x.UpdatePlayerStatistics(It.IsAny<IEnumerable<PlayerInMatchStatisticsRecord>>(), It.IsAny<IDbTransaction>()), bowlingHasChanged ? Times.Once() : Times.Never());
        }

        [Fact]
        public async Task UpdateBowlingScorecard_deletes_obsolete_players()
        {
            // This should take place async to avoid timeouts updating the match. Consider what would happen if the player were used again before the async update.
            var repository = CreateRepository(new SqlServerStatisticsRepository(PlayerRepository));

            // Find a player identity who we only record as having bowled one over
            var matchInnings = DatabaseFixture.TestData.Matches.SelectMany(m => m.MatchInnings);
            var identityOnlyRecordedAsBowlingOneOver = DatabaseFixture.TestData.PlayerIdentities.First(
                    x => matchInnings.SelectMany(mi => mi.OversBowled.Where(o => o.Bowler?.PlayerIdentityId == x.PlayerIdentityId)).Count() == 1 &&
                                                       !matchInnings.Any(mi => mi.PlayerInnings.Any(pi => pi.Batter?.PlayerIdentityId == x.PlayerIdentityId ||
                                                                                                          pi.DismissedBy?.PlayerIdentityId == x.PlayerIdentityId ||
                                                                                                          pi.Bowler?.PlayerIdentityId == x.PlayerIdentityId)) &&
                                                       !DatabaseFixture.TestData.Matches.SelectMany(x => x.Awards).Any(aw => aw.PlayerIdentity?.PlayerIdentityId == x.PlayerIdentityId)
                                                    );

            // Copy the match where that over was bowled
            var match = CloneValidMatch(DatabaseFixture.TestData.Matches.Single(m => m.MatchInnings.Any(mi => mi.OversBowled.Any(o => o.Bowler?.PlayerIdentityId == identityOnlyRecordedAsBowlingOneOver.PlayerIdentityId))));

            // Remove the over from the copy
            var innings = match.MatchInnings.Single(mi => mi.OversBowled.Any(o => o.Bowler?.PlayerIdentityId == identityOnlyRecordedAsBowlingOneOver.PlayerIdentityId));
            var over = innings.OversBowled.Single(o => o.Bowler?.PlayerIdentityId == identityOnlyRecordedAsBowlingOneOver.PlayerIdentityId);
            innings.OversBowled.Remove(over);

            var bowlingFigures = innings.BowlingFigures.Single(bf => bf.Bowler?.PlayerIdentityId == identityOnlyRecordedAsBowlingOneOver.PlayerIdentityId);
            innings.BowlingFigures.Remove(bowlingFigures);

            // Act
            var result = await repository.UpdateBowlingScorecard(
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
                   new { identityOnlyRecordedAsBowlingOneOver.PlayerIdentityId }).ConfigureAwait(false);

                Assert.Null(savedPlayerIdentity);

                var savedPlayer = await connection.QuerySingleOrDefaultAsync<Guid?>(
                   $@"SELECT PlayerId FROM {Tables.Player} WHERE PlayerId = @PlayerId",
                   new { identityOnlyRecordedAsBowlingOneOver.Player!.PlayerId }).ConfigureAwait(false);

                Assert.Null(savedPlayer);

            }
        }

        private static void AddOneNewBowlingOver(MatchInnings innings)
        {
            var overToAdd = new Over
            {
                OverNumber = innings.OversBowled.Count + 1,
                Bowler = innings.OversBowled[innings.OversBowled.Count - 2].Bowler,
                BallsBowled = 8,
                NoBalls = 2,
                Wides = 3,
                RunsConceded = 14,
                OverSet = innings.OverSets.FirstOrDefault()
            };
            innings.OversBowled.Add(overToAdd);
        }
    }
}
