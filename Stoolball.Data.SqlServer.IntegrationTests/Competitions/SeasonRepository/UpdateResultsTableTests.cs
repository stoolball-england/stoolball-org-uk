namespace Stoolball.Data.SqlServer.IntegrationTests.Competitions.SeasonRepository
{
    [Collection(IntegrationTestConstants.TestDataIntegrationTestCollection)]
    public class UpdateResultsTableTests : SqlServerSeasonRepositoryTestsBase, IDisposable
    {
        public UpdateResultsTableTests(SqlServerTestDataFixture databaseFixture) : base(databaseFixture) { }

        [Fact]
        public async Task Throws_ArgumentNullException_if_season_is_null()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(async () => await Repository.UpdateResultsTable(null!, MemberKey, MemberName));
        }

        [Fact]
        public async Task Throws_ArgumentNullException_if_memberKey_is_default_Guid()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(async () => await Repository.UpdateResultsTable(DatabaseFixture.TestData.Seasons.First(), default, MemberName));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task Throws_ArgumentNullException_if_memberName_is_null_or_whitespace(string? memberName)
        {
            await Assert.ThrowsAsync<ArgumentNullException>(async () => await Repository.UpdateResultsTable(DatabaseFixture.TestData.Seasons.First(), MemberKey, memberName!));
        }

        [Fact]
        public async Task Updates_results_table_options()
        {
            var season = EntityCopier.CreateAuditableCopy(DatabaseFixture.TestData.Seasons.First())!;
            season.ResultsTableType = season.ResultsTableType == ResultsTableType.LeagueTable ? ResultsTableType.NonLeagueResultsTable : ResultsTableType.LeagueTable;
            season.EnableRunsScored = !season.EnableRunsScored;
            season.EnableRunsConceded = !season.EnableRunsConceded;

            var result = await Repository.UpdateResultsTable(season, MemberKey, MemberName).ConfigureAwait(false);

            Assert.Equal(season.ResultsTableType, result.ResultsTableType);
            Assert.Equal(season.EnableRunsScored, result.EnableRunsScored);
            Assert.Equal(season.EnableRunsConceded, result.EnableRunsConceded);

            using (var connection = DatabaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                var savedSeason = await connection.QuerySingleOrDefaultAsync<
                    (ResultsTableType ResultsTableType, bool EnableRunsScored, bool EnableRunsConceded)>(
                    @$"SELECT ResultsTableType, EnableRunsScored, EnableRunsConceded
                      FROM {Tables.Season}
                      WHERE SeasonId = @SeasonId",
                    new { season.SeasonId }).ConfigureAwait(false);

                Assert.Equal(season.ResultsTableType, savedSeason.ResultsTableType);
                Assert.Equal(season.EnableRunsScored, savedSeason.EnableRunsScored);
                Assert.Equal(season.EnableRunsConceded, savedSeason.EnableRunsConceded);
            }
        }

        [Fact]
        public async Task Updates_points_rules()
        {
            var season = EntityCopier.CreateAuditableCopy(DatabaseFixture.TestData.Seasons.First(s => s.PointsRules.Any()))!;
            foreach (var rule in season.PointsRules)
            {
                rule.HomePoints += 1;
                rule.AwayPoints += 1;
            }

            var result = await Repository.UpdateResultsTable(season, MemberKey, MemberName).ConfigureAwait(false);

            foreach (var rule in season.PointsRules)
            {
                var updatedRule = result.PointsRules.SingleOrDefault(r => r.PointsRuleId == rule.PointsRuleId);
                Assert.NotNull(updatedRule);
                Assert.Equal(rule.HomePoints, updatedRule!.HomePoints);
                Assert.Equal(rule.AwayPoints, updatedRule!.AwayPoints);
            }

            using (var connection = DatabaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                var savedRules = await connection.QueryAsync<
                    (Guid PointsRuleId, int HomePoints, int AwayPoints)>(
                    @$"SELECT PointsRuleId, HomePoints, AwayPoints
                      FROM {Tables.PointsRule}
                      WHERE SeasonId = @SeasonId",
                    new { season.SeasonId }).ConfigureAwait(false);

                foreach (var rule in season.PointsRules)
                {
                    Assert.Contains(savedRules, r => r.PointsRuleId == rule.PointsRuleId);
                    var updatedRule = savedRules.Single(r => r.PointsRuleId == rule.PointsRuleId);
                    Assert.Equal(rule.HomePoints, updatedRule.HomePoints);
                    Assert.Equal(rule.AwayPoints, updatedRule.AwayPoints);
                }
            }
        }

        [Fact]
        public async Task Audits_and_logs()
        {
            var season = DatabaseFixture.TestData.Seasons.First();

            _ = await Repository.UpdateResultsTable(season, MemberKey, MemberName);

            AuditRepository.Verify(x => x.CreateAudit(It.Is<AuditRecord>(x => x.EntityUri == season.EntityUri), It.IsAny<IDbTransaction>()), Times.Once);
            Logger.Verify(x => x.Info(LoggingTemplates.Updated,
                                       It.Is<Season>(x => x.Competition!.CompetitionId == season.Competition!.CompetitionId
                                                                        && x.FromYear == season.FromYear
                                                                        && x.UntilYear == season.UntilYear),
                                       MemberName, MemberKey, typeof(SqlServerSeasonRepository), nameof(SqlServerSeasonRepository.UpdateResultsTable)));
        }
    }
}
