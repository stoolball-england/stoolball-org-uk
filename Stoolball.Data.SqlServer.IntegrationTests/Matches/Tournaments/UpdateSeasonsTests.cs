namespace Stoolball.Data.SqlServer.IntegrationTests.Matches.Tournaments
{
    [Collection(IntegrationTestConstants.TestDataIntegrationTestCollection)]
    public class UpdateSeasonsTests : TournamentRepositoryTestsBase
    {
        public UpdateSeasonsTests(SqlServerTestDataFixture databaseFixture) : base(databaseFixture) { }

        [Fact]
        public async Task Throws_ArgumentNullException_if_tournament_is_null()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(
                async () => await Repository.UpdateSeasons(
                    null!,
                    MemberKey,
                    MemberName).ConfigureAwait(false)
            ).ConfigureAwait(false);
        }

        [Fact]
        public async Task Throws_ArgumentNullException_if_memberKey_is_default_Guid()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(
                async () => await Repository.UpdateSeasons(
                    DatabaseFixture.TestData.Tournaments.First(),
                    default,
                    MemberName));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task Throws_ArgumentNullException_if_memberName_is_null_or_whitespace(string? memberName)
        {
            await Assert.ThrowsAsync<ArgumentNullException>(
                async () => await Repository.UpdateSeasons(
                    DatabaseFixture.TestData.Tournaments.First(),
                    MemberKey,
                    memberName!).ConfigureAwait(false)
            ).ConfigureAwait(false);
        }

        [Fact]
        public async Task Seasons_added_to_passed_collection_are_added_to_tournament()
        {
            var tournament = Copier.CreateAuditableCopy(DatabaseFixture.TestData.Tournaments.First(t => t.StartTime >= DateTimeOffset.UtcNow && t.Seasons.Any()))!;
            var addedSeason = DatabaseFixture.TestData.Seasons.First(s => !tournament.Seasons.Any(ts => ts.SeasonId == s.SeasonId));
            tournament.Seasons.Add(addedSeason);

            var result = await Repository.UpdateSeasons(tournament, MemberKey, MemberName).ConfigureAwait(false);

            Assert.NotNull(result);
            Assert.NotNull(result.TournamentId);
            Assert.NotEqual(Guid.Empty, result.TournamentId);
            Assert.Equal(tournament.Seasons.Count, result.Seasons.Count);
            foreach (var season in tournament.Seasons)
            {
                Assert.Contains(result.Seasons, s => s.SeasonId == season.SeasonId);
            }

            using (var connection = DatabaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                var seasonsFound = await connection.QueryAsync<Guid>($"SELECT SeasonId FROM {Tables.TournamentSeason} WHERE TournamentId = @TournamentId",
                    new { tournament.TournamentId }).ConfigureAwait(false);

                Assert.Equal(tournament.Seasons.Count, seasonsFound.Count());
                foreach (var seasonId in seasonsFound)
                {
                    Assert.Contains(tournament.Seasons, t => t.SeasonId == seasonId);
                }
            }
        }


        [Fact]
        public async Task Seasons_removed_from_passed_collection_are_removed_from_tournament_including_matches_and_statistics()
        {
            var tournament = Copier.CreateAuditableCopy(DatabaseFixture.TestData.Tournaments.First(t => t.StartTime < DateTimeOffset.UtcNow && t.Seasons.Any() && t.Matches.Any()))!;
            var removedSeason = tournament.Seasons.First();
            tournament.Seasons.Remove(removedSeason);

            var result = await Repository.UpdateSeasons(tournament, MemberKey, MemberName).ConfigureAwait(false);

            Assert.NotNull(result);
            Assert.NotNull(result.TournamentId);
            Assert.NotEqual(Guid.Empty, result.TournamentId);
            Assert.Equal(tournament.Seasons.Count, result.Seasons.Count);
            foreach (var season in tournament.Seasons)
            {
                Assert.Contains(result.Seasons, s => s.SeasonId == season.SeasonId);
            }
            Assert.DoesNotContain(result.Seasons, s => s.SeasonId == removedSeason.SeasonId);

            using (var connection = DatabaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                var seasonsFound = await connection.QueryAsync<Guid>($"SELECT SeasonId FROM {Tables.TournamentSeason} WHERE TournamentId = @TournamentId",
                    new { tournament.TournamentId }).ConfigureAwait(false);

                Assert.Equal(tournament.Seasons.Count, seasonsFound.Count());
                foreach (var seasonId in seasonsFound)
                {
                    Assert.Contains(tournament.Seasons, t => t.SeasonId == seasonId);
                }
                Assert.DoesNotContain(seasonsFound, seasonId => seasonId == removedSeason.SeasonId);

                var matchesFound = await connection.QueryAsync<Guid>($"SELECT MatchId FROM {Tables.Match} WHERE TournamentId = @TournamentId AND SeasonId = @SeasonId",
                    new { tournament.TournamentId, removedSeason.SeasonId }).ConfigureAwait(false);
                Assert.Empty(matchesFound); // Precaution: Season should never have been added to match in the first place

                var statisticsFound = await connection.QueryAsync<Guid>($"SELECT MatchId FROM {Tables.PlayerInMatchStatistics} WHERE TournamentId = @TournamentId AND SeasonId = @SeasonId",
                    new { tournament.TournamentId, removedSeason.SeasonId }).ConfigureAwait(false);
                Assert.Empty(statisticsFound); // Precaution: Season should never have been added to match in the first place
            }
        }
    }
}
