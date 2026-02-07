namespace Stoolball.Data.SqlServer.IntegrationTests.Competitions.SeasonRepository
{
    [Collection(IntegrationTestConstants.TestDataIntegrationTestCollection)]
    public class UpdateSeasonTests : SqlServerSeasonRepositoryTestsBase, IDisposable
    {
        public UpdateSeasonTests(SqlServerTestDataFixture databaseFixture) : base(databaseFixture) { }

        [Fact]
        public async Task Throws_ArgumentNullException_if_season_is_null()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(async () => await Repository.UpdateSeason(null!, MemberKey, MemberName));
        }

        [Fact]
        public async Task Throws_ArgumentNullException_if_memberKey_is_default_Guid()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(async () => await Repository.UpdateSeason(DatabaseFixture.TestData.Seasons.First(), default, MemberName));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task Throws_ArgumentNullException_if_memberName_is_null_or_whitespace(string? memberName)
        {
            await Assert.ThrowsAsync<ArgumentNullException>(async () => await Repository.UpdateSeason(DatabaseFixture.TestData.Seasons.First(), MemberKey, memberName!));
        }

        [Fact]
        public async Task Updates_basic_fields()
        {
            var season = EntityCopier.CreateAuditableCopy(DatabaseFixture.TestData.Seasons.First())!;
            season.EnableTournaments = !season.EnableTournaments;
            season.PlayersPerTeam = (season.PlayersPerTeam ?? 11) + 1;
            season.EnableLastPlayerBatsOn = !season.EnableLastPlayerBatsOn;
            season.EnableBonusOrPenaltyRuns = !season.EnableBonusOrPenaltyRuns;

            const string unsanitisedIntroduction = "Unsanitised introduction";
            const string sanitisedIntroduction = "Sanitised introduction";
            const string unsanitisedResults = "Unsanitised results";
            const string sanitisedResults = "Sanitised results";
            season.Introduction = unsanitisedIntroduction;
            season.Results = unsanitisedResults;
            HtmlSanitizer.Setup(x => x.Sanitize(unsanitisedIntroduction, "", null)).Returns(sanitisedIntroduction);
            HtmlSanitizer.Setup(x => x.Sanitize(unsanitisedResults, "", null)).Returns(sanitisedResults);

            var result = await Repository.UpdateSeason(season, MemberKey, MemberName).ConfigureAwait(false);

            Assert.Equal(season.EnableTournaments, result.EnableTournaments);
            Assert.Equal(season.PlayersPerTeam, result.PlayersPerTeam);
            Assert.Equal(season.EnableLastPlayerBatsOn, result.EnableLastPlayerBatsOn);
            Assert.Equal(season.EnableBonusOrPenaltyRuns, result.EnableBonusOrPenaltyRuns);

            using (var connection = DatabaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                var savedSeason = await connection.QuerySingleOrDefaultAsync<
                    (bool EnableTournaments, int? PlayersPerTeam, bool EnableLastPlayerBatsOn, bool EnableBonusOrPenaltyRuns, string? Introduction, string? Results)>(
                    @$"SELECT EnableTournaments, PlayersPerTeam, EnableLastPlayerBatsOn, EnableBonusOrPenaltyRuns, Introduction, Results
                      FROM {Tables.Season}
                      WHERE SeasonId = @SeasonId",
                    new { season.SeasonId }).ConfigureAwait(false);

                Assert.Equal(season.EnableTournaments, savedSeason.EnableTournaments);
                Assert.Equal(season.PlayersPerTeam, savedSeason.PlayersPerTeam);
                Assert.Equal(season.EnableLastPlayerBatsOn, savedSeason.EnableLastPlayerBatsOn);
                Assert.Equal(season.EnableBonusOrPenaltyRuns, savedSeason.EnableBonusOrPenaltyRuns);
                Assert.Equal(sanitisedIntroduction, savedSeason.Introduction);
                Assert.Equal(sanitisedResults, savedSeason.Results);
            }
        }

        [Fact]
        public async Task Updates_DefaultOverSets_with_passed_collection()
        {
            var season = EntityCopier.CreateAuditableCopy(DatabaseFixture.TestData.Seasons.First(s => s.DefaultOverSets.Count > 1))!;
            var originalOverSets = season.DefaultOverSets.Select(o => o.Clone());

            var updatedOverSet = season.DefaultOverSets.First();
            updatedOverSet.Overs = (updatedOverSet.Overs ?? 0) + DatabaseFixture.Randomiser.PositiveIntegerLessThan(4);
            updatedOverSet.BallsPerOver = (updatedOverSet.BallsPerOver ?? 0) + DatabaseFixture.Randomiser.PositiveIntegerLessThan(4);

            var removedOverSet = season.DefaultOverSets[1];
            season.DefaultOverSets.Remove(removedOverSet);

            var addedOverSet = new OverSet { OverSetNumber = season.DefaultOverSets.Count + 1, Overs = 10, BallsPerOver = 12 };
            season.DefaultOverSets.Add(addedOverSet);

            var result = await Repository.UpdateSeason(season, MemberKey, MemberName).ConfigureAwait(false);

            Assert.Equal(season.DefaultOverSets.Count, result.DefaultOverSets.Count);
            Assert.DoesNotContain(result.DefaultOverSets, o => o.OverSetId == removedOverSet.OverSetId);
            Assert.Contains(result.DefaultOverSets, o => o.OverSetId == updatedOverSet.OverSetId
                                                      && o.Overs == updatedOverSet.Overs
                                                      && o.BallsPerOver == updatedOverSet.BallsPerOver);
            Assert.Contains(result.DefaultOverSets, o => o.OverSetId is not null
                                                      && !originalOverSets.Select(os => os.OverSetId).Contains(o.OverSetId)
                                                      && o.Overs == addedOverSet.Overs
                                                      && o.BallsPerOver == addedOverSet.BallsPerOver);

            using (var connection = DatabaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                var savedOverSets = await connection.QueryAsync<(Guid OverSetId, int? Overs, int? BallsPerOver)>(
                    @$"SELECT OverSetId, Overs, BallsPerOver
                      FROM {Tables.OverSet}
                      WHERE SeasonId = @SeasonId",
                    new { season.SeasonId }).ConfigureAwait(false);

                Assert.Equal(season.DefaultOverSets.Count, savedOverSets.Count());
                Assert.DoesNotContain(savedOverSets, o => o.OverSetId == removedOverSet.OverSetId);
                Assert.Contains(savedOverSets, o => o.OverSetId == updatedOverSet.OverSetId
                                                 && o.Overs == updatedOverSet.Overs
                                                 && o.BallsPerOver == updatedOverSet.BallsPerOver);
                Assert.Contains(savedOverSets, o => !originalOverSets.Select(os => os.OverSetId).Contains(o.OverSetId)
                                                 && o.Overs == addedOverSet.Overs
                                                 && o.BallsPerOver == addedOverSet.BallsPerOver);
            }
        }

        [Fact]
        public async Task Updates_match_types()
        {
            var season = EntityCopier.CreateAuditableCopy(DatabaseFixture.TestData.Seasons.First(s => s.MatchTypes.Contains(MatchType.KnockoutMatch)
                                                                                                   && s.MatchTypes.Contains(MatchType.LeagueMatch)
                                                                                                   && !s.MatchTypes.Contains(MatchType.FriendlyMatch)))!;
            var originalMatchTypes = season.MatchTypes.ToArray();

            season.MatchTypes.Remove(MatchType.KnockoutMatch);
            season.MatchTypes.Add(MatchType.FriendlyMatch);

            var result = await Repository.UpdateSeason(season, MemberKey, MemberName);

            Assert.Equal(originalMatchTypes.Length, result.MatchTypes.Count);
            Assert.DoesNotContain(MatchType.KnockoutMatch, result.MatchTypes);
            Assert.Contains(MatchType.LeagueMatch, result.MatchTypes);
            Assert.Contains(MatchType.FriendlyMatch, result.MatchTypes);

            using (var connection = DatabaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                var savedMatchTypes = await connection.QueryAsync<string>(
                    @$"SELECT MatchType
                      FROM {Tables.SeasonMatchType}
                      WHERE SeasonId = @SeasonId",
                    new { season.SeasonId }).ConfigureAwait(false);

                Assert.Equal(originalMatchTypes.Length, savedMatchTypes.Count());
                Assert.DoesNotContain(MatchType.KnockoutMatch.ToString(), savedMatchTypes);
                Assert.Contains(MatchType.LeagueMatch.ToString(), savedMatchTypes);
                Assert.Contains(MatchType.FriendlyMatch.ToString(), savedMatchTypes);
            }
        }

        [Fact]
        public async Task Audits_and_logs()
        {
            var season = DatabaseFixture.TestData.Seasons.First();

            _ = await Repository.UpdateSeason(season, MemberKey, MemberName);

            AuditRepository.Verify(x => x.CreateAudit(It.Is<AuditRecord>(x => x.EntityUri == season.EntityUri), It.IsAny<IDbTransaction>()), Times.Once);
            Logger.Verify(x => x.Info(LoggingTemplates.Updated,
                                       It.Is<Season>(x => x.Competition!.CompetitionId == season.Competition!.CompetitionId
                                                                        && x.FromYear == season.FromYear
                                                                        && x.UntilYear == season.UntilYear),
                                       MemberName, MemberKey, typeof(SqlServerSeasonRepository), nameof(SqlServerSeasonRepository.UpdateSeason)));
        }
    }
}
