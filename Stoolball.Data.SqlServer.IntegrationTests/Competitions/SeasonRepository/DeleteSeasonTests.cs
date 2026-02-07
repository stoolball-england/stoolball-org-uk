namespace Stoolball.Data.SqlServer.IntegrationTests.Competitions.SeasonRepository
{
    [Collection(IntegrationTestConstants.TestDataIntegrationTestCollection)]
    public class DeleteSeasonTests : SqlServerSeasonRepositoryTestsBase, IDisposable
    {
        public DeleteSeasonTests(SqlServerTestDataFixture databaseFixture) : base(databaseFixture) { }

        [Fact]
        public async Task DeleteSeasons_throws_ArgumentNullException_if_season_is_null()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(async () => await Repository.DeleteSeasons(null!, MemberKey, MemberName, Mock.Of<IDbTransaction>()));
        }

        [Fact]
        public async Task DeleteSeasons_throws_ArgumentNullException_if_memberKey_is_default_Guid()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(async () => await Repository.DeleteSeasons([DatabaseFixture.TestData.Seasons.First()], default, MemberName, Mock.Of<IDbTransaction>()));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task DeleteSeasons_throws_ArgumentNullException_if_memberName_is_null_or_whitespace(string? memberName)
        {
            await Assert.ThrowsAsync<ArgumentNullException>(async () => await Repository.DeleteSeasons([DatabaseFixture.TestData.Seasons.First()], MemberKey, memberName!, Mock.Of<IDbTransaction>()));
        }

        [Fact]
        public async Task DeleteSeasons_succeeds()
        {
            var seasonsToDelete = new Season[] { DatabaseFixture.TestData.SeasonWithFullDetails!, DatabaseFixture.TestData.SeasonWithMinimalDetails! };

            using (var connection = DatabaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                connection.Open();
                var transaction = connection.BeginTransaction();
                await Repository.DeleteSeasons(seasonsToDelete, MemberKey, MemberName, transaction).ConfigureAwait(false);
            }

            var anyOtherSeasonId = DatabaseFixture.TestData.Seasons.First(s => !seasonsToDelete.Select(x => x.SeasonId!.Value).ToList().Contains(s.SeasonId!.Value)).SeasonId!.Value;

            await AssertDeletedSeasons([DatabaseFixture.TestData.SeasonWithFullDetails!.SeasonId!.Value], anyOtherSeasonId).ConfigureAwait(false);
        }

        [Fact]
        public async Task DeleteSeasons_audits_and_logs()
        {
            var seasons = DatabaseFixture.TestData.Seasons.Take(2).ToList();

            using (var connection = DatabaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                connection.Open();
                var transaction = connection.BeginTransaction();
                await Repository.DeleteSeasons(seasons, MemberKey, MemberName, transaction).ConfigureAwait(false);
            }

            foreach (var season in seasons)
            {
                AuditRepository.Verify(x => x.CreateAudit(It.Is<AuditRecord>(x => x.EntityUri == season.EntityUri), It.IsAny<IDbTransaction>()), Times.Once);
                Logger.Verify(x => x.Info(LoggingTemplates.Deleted,
                                           It.Is<Season>(x => x.Competition!.CompetitionId == season.Competition!.CompetitionId
                                                                            && x.FromYear == season.FromYear
                                                                            && x.UntilYear == season.UntilYear),
                                           MemberName, MemberKey, typeof(SqlServerSeasonRepository), nameof(SqlServerSeasonRepository.DeleteSeasons)));
            }
        }

        [Fact]
        public async Task DeleteSeason_throws_ArgumentNullException_if_season_is_null()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(async () => await Repository.DeleteSeason(null!, MemberKey, MemberName));
        }

        [Fact]
        public async Task DeleteSeason_throws_ArgumentNullException_if_memberKey_is_default_Guid()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(async () => await Repository.DeleteSeason(DatabaseFixture.TestData.Seasons.First(), default, MemberName));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task DeleteSeason_throws_ArgumentNullException_if_memberName_is_null_or_whitespace(string? memberName)
        {
            await Assert.ThrowsAsync<ArgumentNullException>(async () => await Repository.DeleteSeason(DatabaseFixture.TestData.Seasons.First(), MemberKey, memberName!));
        }

        [Fact]
        public async Task DeleteSeason_succeeds()
        {
            await Repository.DeleteSeason(DatabaseFixture.TestData.SeasonWithFullDetails!, MemberKey, MemberName).ConfigureAwait(false);

            var anyOtherSeasonId = DatabaseFixture.TestData.Seasons.First(s => s.SeasonId != DatabaseFixture.TestData.SeasonWithFullDetails!.SeasonId).SeasonId!.Value;

            await AssertDeletedSeasons([DatabaseFixture.TestData.SeasonWithFullDetails!.SeasonId!.Value], anyOtherSeasonId).ConfigureAwait(false);
        }

        private async Task AssertDeletedSeasons(IEnumerable<Guid> deletedSeasonIds, Guid anyOtherSeasonId)
        {
            using (var connection = DatabaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                foreach (var deletedSeasonId in deletedSeasonIds)
                {
                    var deletedResult = await connection.QuerySingleOrDefaultAsync<Guid?>($"SELECT SeasonId FROM {Tables.Season} WHERE SeasonId = @SeasonId", new { SeasonId = deletedSeasonId }).ConfigureAwait(false);
                    Assert.Null(deletedResult);
                }

                var unaffectedResult = await connection.QuerySingleOrDefaultAsync<Guid?>($"SELECT SeasonId FROM {Tables.Season} WHERE SeasonId = @SeasonId", new { SeasonId = anyOtherSeasonId }).ConfigureAwait(false);
                Assert.NotNull(unaffectedResult);
            }
        }

        [Fact]
        public async Task DeleteSeason_audits_and_logs()
        {
            var season = DatabaseFixture.TestData.Seasons.First();

            await Repository.DeleteSeason(season, MemberKey, MemberName).ConfigureAwait(false);

            AuditRepository.Verify(x => x.CreateAudit(It.Is<AuditRecord>(x => x.EntityUri == season.EntityUri), It.IsAny<IDbTransaction>()), Times.Once);
            Logger.Verify(x => x.Info(LoggingTemplates.Deleted,
                                       It.Is<Season>(x => x.Competition!.CompetitionId == season.Competition!.CompetitionId
                                                                        && x.FromYear == season.FromYear
                                                                        && x.UntilYear == season.UntilYear),
                                       MemberName, MemberKey, typeof(SqlServerSeasonRepository), nameof(SqlServerSeasonRepository.DeleteSeasons)));
        }
    }
}
