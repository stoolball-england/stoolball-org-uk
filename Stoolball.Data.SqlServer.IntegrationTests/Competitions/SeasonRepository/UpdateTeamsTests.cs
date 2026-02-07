namespace Stoolball.Data.SqlServer.IntegrationTests.Competitions.SeasonRepository
{
    [Collection(IntegrationTestConstants.TestDataIntegrationTestCollection)]
    public class UpdateTeamsTests : SqlServerSeasonRepositoryTestsBase, IDisposable
    {
        public UpdateTeamsTests(SqlServerTestDataFixture databaseFixture) : base(databaseFixture) { }

        [Fact]
        public async Task Throws_ArgumentNullException_if_season_is_null()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(async () => await Repository.UpdateTeams(null!, MemberKey, MemberName));
        }

        [Fact]
        public async Task Throws_ArgumentNullException_if_memberKey_is_default_Guid()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(async () => await Repository.UpdateTeams(DatabaseFixture.TestData.Seasons.First(), default, MemberName));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task Throws_ArgumentNullException_if_memberName_is_null_or_whitespace(string? memberName)
        {
            await Assert.ThrowsAsync<ArgumentNullException>(async () => await Repository.UpdateTeams(DatabaseFixture.TestData.Seasons.First(), MemberKey, memberName!));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task Adds_team(bool teamHasWithdrawn)
        {
            var season = EntityCopier.CreateAuditableCopy(DatabaseFixture.TestData.Seasons.First())!;
            var newTeam = DatabaseFixture.TestData.Teams.First(t => !season.Teams.Select(x => x.Team!.TeamId).Contains(t.TeamId));
            var withdrawnDate = teamHasWithdrawn ? DateTimeOffset.UtcNow.Date : (DateTimeOffset?)null;
            season.Teams.Add(new TeamInSeason { Team = newTeam, WithdrawnDate = withdrawnDate });

            var result = await Repository.UpdateTeams(season, MemberKey, MemberName).ConfigureAwait(false);

            Assert.Equal(season.Teams.Count, result.Teams.Count);
            Assert.Contains(result.Teams, t => t.Team!.TeamId == newTeam.TeamId);

            using (var connection = DatabaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                var teamInSeason = await connection.QuerySingleOrDefaultAsync<(Guid? SeasonTeamId, DateTimeOffset? WithdrawnDate)>(
                    @$"SELECT SeasonTeamId, WithdrawnDate
                      FROM {Tables.SeasonTeam}
                      WHERE SeasonId = @SeasonId AND TeamId = @TeamId;",
                    new { season.SeasonId, newTeam.TeamId }).ConfigureAwait(false);

                Assert.NotNull(teamInSeason.SeasonTeamId);
                if (teamHasWithdrawn)
                {
                    Assert.Equal(withdrawnDate, teamInSeason.WithdrawnDate);
                }
                else
                {
                    Assert.Null(teamInSeason.WithdrawnDate);
                }
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Updates_withdrawn_status(bool wasWithdrawn)
        {
            var season = EntityCopier.CreateAuditableCopy(DatabaseFixture.TestData.Seasons.First(s => s.Teams.Any(t => t.WithdrawnDate is not null)))!;
            var affectedTeam = season.Teams.First(t => wasWithdrawn ? t.WithdrawnDate is not null : t.WithdrawnDate is null);
            var expectedWithdrawnDate = wasWithdrawn ? (DateTimeOffset?)null : DateTimeOffset.UtcNow.Date;
            affectedTeam.WithdrawnDate = expectedWithdrawnDate;

            var result = await Repository.UpdateTeams(season, MemberKey, MemberName).ConfigureAwait(false);

            Assert.Equal(season.Teams.Count, result.Teams.Count);
            Assert.Contains(result.Teams, t => t.Team!.TeamId == affectedTeam.Team!.TeamId);
            Assert.Equal(expectedWithdrawnDate, result.Teams.First(t => t.Team!.TeamId == affectedTeam.Team!.TeamId).WithdrawnDate);

            using (var connection = DatabaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                var teamInSeason = await connection.QuerySingleOrDefaultAsync<(Guid? SeasonTeamId, DateTimeOffset? WithdrawnDate)>(
                    @$"SELECT SeasonTeamId, WithdrawnDate
                      FROM {Tables.SeasonTeam}
                      WHERE SeasonId = @SeasonId AND TeamId = @TeamId;",
                    new { season.SeasonId, affectedTeam.Team!.TeamId }).ConfigureAwait(false);

                Assert.NotNull(teamInSeason.SeasonTeamId);
                if (wasWithdrawn)
                {
                    Assert.Null(teamInSeason.WithdrawnDate);
                }
                else
                {
                    Assert.Equal(expectedWithdrawnDate, teamInSeason.WithdrawnDate);
                }
            }
        }

        [Fact]
        public async Task Removes_team()
        {
            var season = EntityCopier.CreateAuditableCopy(DatabaseFixture.TestData.Seasons.First(s => s.Teams.Any()))!;
            var teamToRemove = season.Teams.First();
            season.Teams.Remove(teamToRemove);

            var result = await Repository.UpdateTeams(season, MemberKey, MemberName).ConfigureAwait(false);

            Assert.Equal(season.Teams.Count, result.Teams.Count);
            Assert.DoesNotContain(result.Teams, t => t.Team!.TeamId == teamToRemove.Team!.TeamId);

            using (var connection = DatabaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                var seasonTeamId = await connection.QuerySingleOrDefaultAsync<Guid?>(
                    @$"SELECT SeasonTeamId
                      FROM {Tables.SeasonTeam}
                      WHERE SeasonId = @SeasonId AND TeamId = @TeamId;",
                    new { season.SeasonId, teamToRemove.Team!.TeamId }).ConfigureAwait(false);

                Assert.Null(seasonTeamId);
            }
        }

        [Fact]
        public async Task Audits_and_logs()
        {
            var season = DatabaseFixture.TestData.Seasons.First();

            _ = await Repository.UpdateTeams(season, MemberKey, MemberName);

            AuditRepository.Verify(x => x.CreateAudit(It.Is<AuditRecord>(x => x.EntityUri == season.EntityUri), It.IsAny<IDbTransaction>()), Times.Once);
            Logger.Verify(x => x.Info(LoggingTemplates.Updated,
                                       It.Is<Season>(x => x.Competition!.CompetitionId == season.Competition!.CompetitionId
                                                                        && x.FromYear == season.FromYear
                                                                        && x.UntilYear == season.UntilYear),
                                       MemberName, MemberKey, typeof(SqlServerSeasonRepository), nameof(SqlServerSeasonRepository.UpdateTeams)));
        }
    }
}
