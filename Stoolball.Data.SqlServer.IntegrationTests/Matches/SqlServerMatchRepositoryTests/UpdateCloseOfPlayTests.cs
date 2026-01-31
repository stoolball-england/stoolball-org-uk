using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Moq;
using Stoolball.Awards;
using Stoolball.Data.SqlServer.IntegrationTests.Fixtures;
using Stoolball.Matches;
using Stoolball.Statistics;
using Stoolball.Teams;
using Xunit;

namespace Stoolball.Data.SqlServer.IntegrationTests.Matches.SqlServerMatchRepositoryTests
{
    [Collection(IntegrationTestConstants.TestDataIntegrationTestCollection)]
    public class UpdateCloseOfPlayTests : MatchRepositoryTestsBase, IDisposable
    {
        private const string UPDATED_MATCH_NAME = "Updated match name";

        public UpdateCloseOfPlayTests(SqlServerTestDataFixture databaseFixture) : base(databaseFixture)
        {
            MatchNameBuilder.Setup(x => x.BuildMatchName(It.IsAny<Stoolball.Matches.Match>())).Returns(UPDATED_MATCH_NAME);
        }

        [Fact]
        public async Task UpdateCloseOfPlay_throws_MatchNotFoundException_for_match_id_that_does_not_exist()
        {
            await Assert.ThrowsAsync<MatchNotFoundException>(
                async () => await Repository.UpdateCloseOfPlay(new Stoolball.Matches.Match
                {
                    MatchId = Guid.NewGuid(),
                    Awards = new List<MatchAward>
                    {
                        new MatchAward {
                            Award = new Award { AwardName = StatisticsConstants.PLAYER_OF_THE_MATCH_AWARD },
                            PlayerIdentity = new PlayerIdentity{ Team = new Team{ TeamId = Guid.NewGuid() } }
                        }
                    }
                },
                 MemberKey,
                 MemberName
                ).ConfigureAwait(false)).ConfigureAwait(false);
        }

        [Fact]
        public async Task UpdateCloseOfPlay_throws_AwardNotFoundException_for_match_award_award_name_that_does_not_exist()
        {
            await Assert.ThrowsAsync<AwardNotFoundException>(
                async () => await Repository.UpdateCloseOfPlay(new Stoolball.Matches.Match
                {
                    MatchId = DatabaseFixture.TestData.Matches[0].MatchId,
                    Awards = new List<MatchAward>
                    {
                        new MatchAward {
                            Award = new Award { AwardName = Guid.NewGuid().ToString() },
                            PlayerIdentity = new PlayerIdentity{ Team = new Team{ TeamId = Guid.NewGuid() } }
                        }
                    }
                },
                 MemberKey,
                 MemberName
                ).ConfigureAwait(false)).ConfigureAwait(false);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task UpdateCloseOfPlay_updates_or_preserves_match_name_depending_on_saved_setting(bool updateMatchName)
        {
            var matchToUpdate = DatabaseFixture.TestData.Matches.First(x => x.UpdateMatchNameAutomatically == updateMatchName);
            var nameBefore = matchToUpdate.MatchName;

            var result = await Repository.UpdateCloseOfPlay(matchToUpdate, MemberKey, MemberName).ConfigureAwait(false);

            Assert.Equal(updateMatchName ? UPDATED_MATCH_NAME : nameBefore, result.MatchName);

            using (var connection = DatabaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                var savedMatchName = await connection.QuerySingleAsync<string>(
                    $"SELECT MatchName FROM {Tables.Match} WHERE MatchId = @MatchId",
                    new
                    {
                        matchToUpdate.MatchId
                    }).ConfigureAwait(false);

                Assert.Equal(updateMatchName ? UPDATED_MATCH_NAME : nameBefore, savedMatchName);
            }
        }

        [Fact]
        public async Task UpdateCloseOfPlay_saves_match_result()
        {
            var modifiedMatch = CloneValidMatch(DatabaseFixture.TestData.Matches.First(x => x.UpdateMatchNameAutomatically == false));
            var matchResultBefore = modifiedMatch.MatchResultType;
            var matchResultAfter = matchResultBefore == MatchResultType.HomeWin ? MatchResultType.AwayWin : MatchResultType.HomeWin;
            modifiedMatch.MatchResultType = matchResultAfter;

            var result = await Repository.UpdateCloseOfPlay(modifiedMatch, MemberKey, MemberName).ConfigureAwait(false);

            Assert.Equal(matchResultAfter, result.MatchResultType);

            using (var connection = DatabaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                var savedMatchResult = await connection.QuerySingleAsync<string>(
                    $"SELECT MatchResultType FROM {Tables.Match} WHERE MatchId = @MatchId",
                    new
                    {
                        modifiedMatch.MatchId
                    }).ConfigureAwait(false);

                Assert.Equal(matchResultAfter, Enum.Parse(typeof(MatchResultType), savedMatchResult));
            }
        }

        [Fact]
        public async Task UpdateCloseOfPlay_updates_existing_award()
        {
            // If you change the award or the player identity, that's probably a different award. Only changing the reason is definitely the same award.
            var modifiedMatch = CloneValidMatch(DatabaseFixture.TestData.Matches.First(x => x.Awards.Count > 0));
            var modifiedAward = modifiedMatch.Awards[0];
            modifiedAward.Reason += Guid.NewGuid().ToString();

            var result = await Repository.UpdateCloseOfPlay(modifiedMatch, MemberKey, MemberName).ConfigureAwait(false);

            Assert.Equal(modifiedMatch.Awards.Count, result.Awards.Count);
            Assert.Equal(modifiedAward.AwardedToId, result.Awards[0].AwardedToId);
            Assert.Equal(modifiedAward.Reason, result.Awards[0].Reason);

            using (var connection = DatabaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                var savedMatchAward = await connection.QuerySingleOrDefaultAsync<Guid?>(
                    @$"SELECT AwardedToId FROM {Tables.AwardedTo} ma 
                       WHERE AwardedToId = @AwardedToId
                       AND MatchId = @MatchId
                       AND AwardId = @AwardId
                       AND PlayerIdentityId = @PlayerIdentityId
                       AND Reason = @Reason",
                    new
                    {
                        modifiedAward.AwardedToId,
                        modifiedMatch.MatchId,
                        modifiedAward.Award!.AwardId,
                        modifiedAward.PlayerIdentity!.PlayerIdentityId,
                        modifiedAward.Reason
                    }).ConfigureAwait(false);

                Assert.NotNull(savedMatchAward);
            }

        }

        [Fact]
        public async Task UpdateCloseOfPlay_adds_new_award()
        {
            var matchToUpdate = CloneValidMatch(DatabaseFixture.TestData.MatchInThePastWithFullDetails!);
            AddOneNewAward(matchToUpdate);
            var newAward = matchToUpdate.Awards.Last();

            var result = await Repository.UpdateCloseOfPlay(matchToUpdate, MemberKey, MemberName).ConfigureAwait(false);

            foreach (var award in matchToUpdate.Awards)
            {
                Assert.Single(result.Awards.Where(aw => aw.PlayerIdentity?.PlayerIdentityId == award.PlayerIdentity!.PlayerIdentityId && aw.Reason == award.Reason));
            }

            using (var connection = DatabaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                var savedAwardId = await connection.QuerySingleOrDefaultAsync<Guid?>(
                        $@"SELECT ma.AwardedToId
                               FROM {Tables.AwardedTo} ma
                               WHERE ma.MatchId = @MatchId AND ma.PlayerIdentityId = @PlayerIdentityId AND ma.Reason = @reason",
                        new
                        {
                            matchToUpdate.MatchId,
                            newAward.PlayerIdentity!.PlayerIdentityId,
                            newAward.Reason
                        }).ConfigureAwait(false);

                Assert.NotNull(savedAwardId);
            }
        }

        private void AddOneNewAward(Stoolball.Matches.Match match)
        {
            match.Awards.Add(
                    new MatchAward
                    {
                        PlayerIdentity = DatabaseFixture.TestData.PlayerIdentities.First(pi => match.Teams
                                                .Where(t => t.Team != null).Select(t => t.Team!.TeamId)
                                                .Contains(pi.Team?.TeamId)),
                        Award = match.Awards[0].Award,
                        Reason = "A good reason " + Guid.NewGuid()
                    }
                );
        }

        [Fact]
        public async Task UpdateCloseOfPlay_deletes_removed_award()
        {
            var matchToUpdate = DatabaseFixture.TestData.Matches.First(m => m.Awards.Count > 1);
            var copyOfMatch = CloneValidMatch(matchToUpdate);
            var awardToRemove = copyOfMatch.Awards[0];
            copyOfMatch.Awards.Remove(awardToRemove);

            var result = await Repository.UpdateCloseOfPlay(copyOfMatch, MemberKey, MemberName).ConfigureAwait(false);

            Assert.Equal(copyOfMatch.Awards.Count, result.Awards.Count);

            using (var connection = DatabaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                var totalAwardsForMatch = await connection.QuerySingleAsync<int>(
                        $"SELECT COUNT(ma.AwardedToId) FROM {Tables.AwardedTo} ma WHERE ma.MatchId = @MatchId",
                        new { matchToUpdate.MatchId }
                    ).ConfigureAwait(false);

                Assert.Equal(copyOfMatch.Awards.Count, totalAwardsForMatch);

                var removedAward = await connection.QuerySingleOrDefaultAsync<Guid?>(
                        $"SELECT ma.AwardedToId FROM {Tables.AwardedTo} ma WHERE ma.MatchId = @MatchId AND ma.PlayerIdentityId = @PlayerIdentityId AND ma.Reason = @Reason",
                        new
                        {
                            matchToUpdate.MatchId,
                            awardToRemove.PlayerIdentity?.PlayerIdentityId,
                            awardToRemove.Reason
                        }
                    ).ConfigureAwait(false);

                Assert.Null(removedAward);
            }
        }

        [Fact]
        public async Task UpdateCloseOfPlay_updates_player_statistics()
        {
            var matchToUpdate = CloneValidMatch(DatabaseFixture.TestData.MatchInThePastWithFullDetails!);
            AddOneNewAward(matchToUpdate);

            _ = await Repository.UpdateCloseOfPlay(matchToUpdate, MemberKey, MemberName).ConfigureAwait(false);

            StatisticsRepository.Verify(x => x.UpdatePlayerStatistics(It.IsAny<IEnumerable<PlayerInMatchStatisticsRecord>>(), It.IsAny<IDbTransaction>()), Times.Once());
        }

        [Fact]
        public async Task UpdateCloseOfPlay_deletes_obsolete_players()
        {
            // This should take place async to avoid timeouts updating the match. Consider what would happen if the player were used again before the async update.
            var repository = CreateRepository(new SqlServerStatisticsRepository(PlayerRepository));

            // Find a player identity who we only record that player (with any identity) as having won an award once
            var awardWinners = DatabaseFixture.TestData.Matches.SelectMany(m => m.Awards.Select(aw => aw.PlayerIdentity).OfType<PlayerIdentity>()).ToList();
            awardWinners = awardWinners.Where(pi => awardWinners.Count(pi2 => pi2.PlayerIdentityId == pi.PlayerIdentityId) == 1).ToList();

            var matchInnings = DatabaseFixture.TestData.Matches.SelectMany(m => m.MatchInnings);
            var playerInnings = matchInnings.SelectMany(x => x.PlayerInnings);
            var identityOnlyRecordedAsWinningOneAward = DatabaseFixture.TestData.PlayerIdentities.First(
                    x => awardWinners.Any(aw => aw.PlayerIdentityId == x.PlayerIdentityId) &&
                        !playerInnings.Any(pi => pi.Batter?.Player?.PlayerId == x.Player?.PlayerId ||
                                                 pi.DismissedBy?.Player?.PlayerId == x.Player?.PlayerId ||
                                                 pi.Bowler?.Player?.PlayerId == x.Player?.PlayerId) &&
                        !matchInnings.Any(mi => mi.OversBowled.Any(pi => pi.Bowler?.Player?.PlayerId == x.Player?.PlayerId)));

            // Copy the match where that identity won its award
            var match = CloneValidMatch(DatabaseFixture.TestData.Matches.Single(m => m.Awards.Any(aw => aw.PlayerIdentity?.PlayerIdentityId == identityOnlyRecordedAsWinningOneAward.PlayerIdentityId)));

            // Remove the award from the copy
            match.Awards.Clear();

            // Act
            var result = await repository.UpdateCloseOfPlay(
                    match,
                    MemberKey,
                    MemberName);
            await PlayerRepository.ProcessAsyncUpdatesForPlayers();

            // Assert
            using (var connection = DatabaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                var savedPlayerIdentity = await connection.QuerySingleOrDefaultAsync<Guid?>(
                   $@"SELECT PlayerIdentityId FROM {Tables.PlayerIdentity} WHERE PlayerIdentityId = @PlayerIdentityId",
                   new { identityOnlyRecordedAsWinningOneAward.PlayerIdentityId }).ConfigureAwait(false);

                Assert.Null(savedPlayerIdentity);

                var savedPlayer = await connection.QuerySingleOrDefaultAsync<Guid?>(
                   $@"SELECT PlayerId FROM {Tables.Player} WHERE PlayerId = @PlayerId",
                   new { identityOnlyRecordedAsWinningOneAward.Player!.PlayerId }).ConfigureAwait(false);

                Assert.Null(savedPlayer);
            }
        }
    }
}
