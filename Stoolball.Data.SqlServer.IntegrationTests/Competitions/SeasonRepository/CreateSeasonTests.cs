using System.Globalization;
using Bogus;
using Stoolball.Matches;

namespace Stoolball.Data.SqlServer.IntegrationTests.Competitions.SeasonRepository
{
    [Collection(IntegrationTestConstants.TestDataIntegrationTestCollection)]
    public class CreateSeasonTests : SqlServerSeasonRepositoryTestsBase, IDisposable
    {
        public CreateSeasonTests(SqlServerTestDataFixture databaseFixture) : base(databaseFixture) { }

        [Fact]
        public async Task Throws_ArgumentNullException_if_season_is_null()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(async () => await Repository.CreateSeason(null!, MemberKey, MemberName));
        }

        [Fact]
        public async Task Throws_ArgumentException_if_Competition_is_null()
        {
            await Assert.ThrowsAsync<ArgumentException>(async () => await Repository.CreateSeason(new Season(), MemberKey, MemberName));
        }

        [Fact]
        public async Task Throws_ArgumentException_if_CompetitionId_is_null()
        {
            var competition = EntityCopier.CreateAuditableCopy(DatabaseFixture.TestData.Competitions.First());
            competition.CompetitionId = null;
            await Assert.ThrowsAsync<ArgumentException>(async () => await Repository.CreateSeason(new Season { Competition = competition }, MemberKey, MemberName));
        }

        [Fact]
        public async Task Throws_ArgumentNullException_if_memberKey_is_default_Guid()
        {
            var season = new Season { Competition = DatabaseFixture.TestData.Competitions.First() };
            await Assert.ThrowsAsync<ArgumentNullException>(async () => await Repository.CreateSeason(season, default, MemberName));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task Throws_ArgumentNullException_if_memberName_is_null_or_whitespace(string? memberName)
        {
            var season = new Season { Competition = DatabaseFixture.TestData.Competitions.First() };
            await Assert.ThrowsAsync<ArgumentNullException>(async () => await Repository.CreateSeason(season, MemberKey, memberName!));
        }

        [Fact]
        public async Task Saves_basic_fields()
        {
            var competition = DatabaseFixture.TestData.Competitions.First();
            var fromYear = competition.Seasons.Select(s => s.FromYear).Max() + 1;
            const string unsanitisedIntroduction = "Unsanitised introduction";
            const string sanitisedIntroduction = "Sanitised introduction";
            const string unsanitisedResults = "Unsanitised results";
            const string sanitisedResults = "Sanitised results";

            var season = new Season
            {
                Competition = competition,
                FromYear = fromYear,
                UntilYear = fromYear + 1,
                EnableTournaments = DatabaseFixture.Randomiser.FiftyFiftyChance(),
                PlayersPerTeam = DatabaseFixture.Randomiser.Between(8, 11),
                EnableLastPlayerBatsOn = DatabaseFixture.Randomiser.FiftyFiftyChance(),
                EnableBonusOrPenaltyRuns = DatabaseFixture.Randomiser.FiftyFiftyChance(),
                Introduction = unsanitisedIntroduction,
                Results = unsanitisedResults
            };

            HtmlSanitizer.Setup(x => x.Sanitize(unsanitisedIntroduction, "", null)).Returns(sanitisedIntroduction);
            HtmlSanitizer.Setup(x => x.Sanitize(unsanitisedResults, "", null)).Returns(sanitisedResults);

            var result = await Repository.CreateSeason(season, MemberKey, MemberName).ConfigureAwait(false);

            Assert.NotNull(result.SeasonId);
            Assert.NotEqual(default, result.SeasonId);
            Assert.Equal(competition.CompetitionId, result.Competition!.CompetitionId);
            Assert.Equal(fromYear, result.FromYear);
            Assert.Equal(fromYear + 1, result.UntilYear);
            Assert.Equal(season.EnableTournaments, result.EnableTournaments);
            Assert.Equal(season.PlayersPerTeam, result.PlayersPerTeam);
            Assert.Equal(season.EnableLastPlayerBatsOn, result.EnableLastPlayerBatsOn);
            Assert.Equal(season.EnableBonusOrPenaltyRuns, result.EnableBonusOrPenaltyRuns);
            Assert.Equal(sanitisedIntroduction, result.Introduction);
            Assert.Equal(sanitisedResults, result.Results);

            using (var connection = DatabaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                var savedSeason = await connection.QuerySingleOrDefaultAsync<
                    (Guid CompetitionId, int FromYear, int UntilYear, bool EnableTournaments, int? PlayersPerTeam,
                     bool EnableLastPlayerBatsOn, bool EnableBonusOrPenaltyRuns, string? Introduction, string? Results)>(
                    @$"SELECT CompetitionId, FromYear, UntilYear, EnableTournaments, PlayersPerTeam, 
                              EnableLastPlayerBatsOn, EnableBonusOrPenaltyRuns, Introduction, Results
                      FROM {Tables.Season}
                      WHERE SeasonId = @SeasonId",
                    new { result.SeasonId }).ConfigureAwait(false);

                Assert.Equal(competition.CompetitionId, savedSeason.CompetitionId);
                Assert.Equal(fromYear, savedSeason.FromYear);
                Assert.Equal(fromYear + 1, savedSeason.UntilYear);
                Assert.Equal(season.EnableTournaments, savedSeason.EnableTournaments);
                Assert.Equal(season.PlayersPerTeam, savedSeason.PlayersPerTeam);
                Assert.Equal(season.EnableLastPlayerBatsOn, savedSeason.EnableLastPlayerBatsOn);
                Assert.Equal(season.EnableBonusOrPenaltyRuns, savedSeason.EnableBonusOrPenaltyRuns);
                Assert.Equal(sanitisedIntroduction, savedSeason.Introduction);
                Assert.Equal(sanitisedResults, savedSeason.Results);
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Route_is_based_on_competition_and_year(bool sameStartAndEndYear)
        {
            var competition = DatabaseFixture.TestData.Competitions.First();
            var fromYear = competition.Seasons.Select(s => s.FromYear).Max() + 1;
            var expectedRoute = sameStartAndEndYear switch
            {
                true => $"{competition.CompetitionRoute}/{fromYear}",
                false => $"{competition.CompetitionRoute}/{fromYear}-{(fromYear + 1).ToString(CultureInfo.InvariantCulture).Substring(2)}",
            };

            var season = new Season
            {
                Competition = competition,
                FromYear = fromYear,
                UntilYear = sameStartAndEndYear ? fromYear : fromYear + 1,
                SeasonRoute = "/competitions/ignore-this-input"
            };

            var result = await Repository.CreateSeason(season, MemberKey, MemberName).ConfigureAwait(false);

            Assert.NotNull(result.SeasonId);
            Assert.NotEqual(default, result.SeasonId);
            Assert.Equal(expectedRoute, result.SeasonRoute);

            using (var connection = DatabaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                var savedRoute = await connection.QuerySingleOrDefaultAsync<string>(
                    @$"SELECT SeasonRoute
                      FROM {Tables.Season}
                      WHERE SeasonId = @SeasonId",
                    new { result.SeasonId }).ConfigureAwait(false);

                Assert.Equal(expectedRoute, savedRoute);
            }
        }

        [Fact]
        public async Task Copies_results_table_options_from_previous_season()
        {
            Func<Competition, bool> seasonPredicate = x => x.Seasons.Count == 1;
            var competition = EntityCopier.CreateAuditableCopy(DatabaseFixture.TestData.Competitions.First(seasonPredicate));
            var previousSeason = competition.Seasons[0];

            var newSeason = new Season
            {
                Competition = competition,
                FromYear = previousSeason.FromYear + 1,
                UntilYear = previousSeason.UntilYear + 1,

                // These results table options should not be used
                ResultsTableType = previousSeason.ResultsTableType == ResultsTableType.LeagueTable ? ResultsTableType.NonLeagueResultsTable : ResultsTableType.LeagueTable,
                EnableRunsScored = !previousSeason.EnableRunsScored,
                EnableRunsConceded = !previousSeason.EnableRunsConceded
            };

            await AssertResultsTableOptions(newSeason,
                                previousSeason.ResultsTableType,
                                previousSeason.EnableRunsScored,
                                previousSeason.EnableRunsConceded).ConfigureAwait(false);
        }

        [Fact]
        public async Task Sets_results_table_options_to_default_if_no_previous_season()
        {
            var competition = EntityCopier.CreateAuditableCopy(DatabaseFixture.TestData.CompetitionWithNoSeasons);

            var season = new Season
            {
                Competition = competition,
                FromYear = 2024,
                UntilYear = 2024
            };

            await AssertResultsTableOptions(season,
                                            Defaults.ResultsTable.TableType,
                                            Defaults.ResultsTable.EnableRunsScored,
                                            Defaults.ResultsTable.EnableRunsConceded).ConfigureAwait(false);
        }

        private async Task AssertResultsTableOptions(Season season, ResultsTableType expectedTableType, bool expectedRunsScored, bool expectedRunsConceded)
        {
            var result = await Repository.CreateSeason(season, MemberKey, MemberName).ConfigureAwait(false);

            Assert.NotNull(result.SeasonId);
            Assert.Equal(expectedTableType, result.ResultsTableType);
            Assert.Equal(expectedRunsScored, result.EnableRunsScored);
            Assert.Equal(expectedRunsConceded, result.EnableRunsConceded);

            using (var connection = DatabaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                var savedSeason = await connection.QuerySingleOrDefaultAsync<(ResultsTableType TableType, bool EnableRunsScored, bool EnableRunsConceded)>(
                     @$"SELECT ResultsTableType, EnableRunsScored, EnableRunsConceded
                      FROM {Tables.Season}
                      WHERE SeasonId = @SeasonId",
                     new { result.SeasonId }).ConfigureAwait(false);

                Assert.Equal(expectedTableType, savedSeason.TableType);
                Assert.Equal(expectedRunsScored, savedSeason.EnableRunsScored);
                Assert.Equal(expectedRunsConceded, savedSeason.EnableRunsConceded);
            }
        }

        [Fact]
        public async Task DefaultOverSets_set_from_passed_collection()
        {
            var competition = DatabaseFixture.TestData.Competitions.First();
            var fromYear = competition.Seasons.Select(s => s.FromYear).Max() + 1;

            var season = new Season
            {
                Competition = competition,
                FromYear = fromYear,
                UntilYear = fromYear,
                DefaultOverSets = DatabaseFixture.OverSetFakerFactory.Create().GenerateBetween(2, 5)
            };

            var result = await Repository.CreateSeason(season, MemberKey, MemberName).ConfigureAwait(false);

            Assert.NotNull(result.SeasonId);
            Assert.NotEqual(default, result.SeasonId);
            Assert.Equal(season.DefaultOverSets.Count, result.DefaultOverSets.Count);
            foreach (var overset in season.DefaultOverSets)
            {
                Assert.Contains(result.DefaultOverSets, o => o.OverSetNumber == overset.OverSetNumber
                                                          && o.Overs == overset.Overs
                                                          && o.BallsPerOver == overset.BallsPerOver);
            }

            using (var connection = DatabaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                var savedOversets = await connection.QueryAsync<(int OverSetNumber, int Overs, int BallsPerOver)>(
                    @$"SELECT OverSetNumber, Overs, BallsPerOver
                      FROM {Tables.OverSet}
                      WHERE SeasonId = @SeasonId",
                    new { result.SeasonId }).ConfigureAwait(false);

                Assert.Equal(season.DefaultOverSets.Count, savedOversets.Count());
                foreach (var overset in season.DefaultOverSets)
                {
                    Assert.Contains(savedOversets, o => o.OverSetNumber == overset.OverSetNumber
                                                     && o.Overs == overset.Overs
                                                     && o.BallsPerOver == overset.BallsPerOver);
                }
            }
        }

        [Fact]
        public async Task Match_types_set_from_passed_collection()
        {
            var competition = DatabaseFixture.TestData.Competitions.First();
            var fromYear = competition.Seasons.Select(s => s.FromYear).Max() + 1;
            var matchTypes = new List<MatchType>();
            while (matchTypes.Count < 3)
            {
                var candidate = DatabaseFixture.Randomiser.RandomEnum<MatchType>();
                if (!matchTypes.Contains(candidate))
                {
                    matchTypes.Add(candidate);
                }
            }

            var season = new Season
            {
                Competition = competition,
                FromYear = fromYear,
                UntilYear = fromYear,
                MatchTypes = matchTypes
            };

            var result = await Repository.CreateSeason(season, MemberKey, MemberName).ConfigureAwait(false);

            Assert.NotNull(result.SeasonId);
            Assert.NotEqual(default, result.SeasonId);
            Assert.Equal(season.MatchTypes.Count, result.MatchTypes.Count);
            foreach (var matchType in season.MatchTypes)
            {
                Assert.Contains(matchType, result.MatchTypes);
            }

            using (var connection = DatabaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                var savedMatchTypes = await connection.QueryAsync<string>(
                     @$"SELECT MatchType
                      FROM {Tables.SeasonMatchType}
                      WHERE SeasonId = @SeasonId",
                     new { result.SeasonId }).ConfigureAwait(false);

                Assert.Equal(season.MatchTypes.Count, savedMatchTypes.Count());
                foreach (var matchType in season.MatchTypes)
                {
                    Assert.Contains(matchType.ToString(), savedMatchTypes);
                }
            }
        }

        [Fact]
        public async Task Copies_points_rules_from_previous_season()
        {
            Func<Competition, bool> seasonPredicate = x => x.Seasons.Count == 1 && x.Seasons[0].PointsRules.Any();
            var competition = EntityCopier.CreateAuditableCopy(DatabaseFixture.TestData.Competitions.First(seasonPredicate));
            var previousSeason = competition.Seasons[0];

            var newSeason = new Season
            {
                Competition = competition,
                FromYear = previousSeason.FromYear + 1,
                UntilYear = previousSeason.UntilYear + 1,
                PointsRules = previousSeason.PointsRules.Select(rule => rule.Clone()).ToList()
            };

            // These points rules should not be used
            foreach (var rule in newSeason.PointsRules)
            {
                rule.HomePoints = rule.HomePoints + 2;
                rule.AwayPoints = rule.AwayPoints + 2;
            }

            await AssertPointsRules(newSeason, previousSeason.PointsRules).ConfigureAwait(false);

        }

        [Fact]
        public async Task Sets_points_rules_to_default_if_no_previous_season()
        {
            var competition = EntityCopier.CreateAuditableCopy(DatabaseFixture.TestData.CompetitionWithNoSeasons);

            var season = new Season
            {
                Competition = competition,
                FromYear = 2024,
                UntilYear = 2024
            };

            await AssertPointsRules(season, Defaults.PointsRules.ToList()).ConfigureAwait(false);
        }

        private async Task AssertPointsRules(Season seasonToCreate, IList<PointsRule> expectedPointsRules)
        {
            var result = await Repository.CreateSeason(seasonToCreate, MemberKey, MemberName).ConfigureAwait(false);

            Assert.NotNull(result.SeasonId);
            Assert.NotEqual(default, result.SeasonId);
            Assert.Equal(expectedPointsRules.Count, result.PointsRules.Count);
            foreach (var pointsRule in expectedPointsRules)
            {
                Assert.Contains(result.PointsRules, rule => rule.MatchResultType == pointsRule.MatchResultType
                                                         && rule.HomePoints == pointsRule.HomePoints
                                                         && rule.AwayPoints == pointsRule.AwayPoints);
            }

            using (var connection = DatabaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                var savedRules = await connection.QueryAsync<(MatchResultType MatchResultType, int HomePoints, int AwayPoints)>(
                     @$"SELECT MatchResultType, HomePoints, AwayPoints
                      FROM {Tables.PointsRule}
                      WHERE SeasonId = @SeasonId",
                     new { result.SeasonId }).ConfigureAwait(false);

                Assert.Equal(expectedPointsRules.Count, savedRules.Count());
                foreach (var pointsRule in expectedPointsRules)
                {
                    Assert.Contains(savedRules, rule => rule.MatchResultType == pointsRule.MatchResultType
                                                     && rule.HomePoints == pointsRule.HomePoints
                                                     && rule.AwayPoints == pointsRule.AwayPoints);
                }
            }
        }

        [Fact]
        public async Task Copies_teams_from_previous_season_excluding_withdrawn_and_inactive_teams()
        {
            Func<Competition, bool> seasonPredicate = x => x.Seasons.Count == 1
                                                   && x.Seasons[0].Teams.Any(t => t.WithdrawnDate is null)
                                                   && x.Seasons[0].Teams.Any(t => t.WithdrawnDate is not null)
                                                   && x.Seasons[0].Teams.Any(t => t.Team?.UntilYear == x.Seasons[0].FromYear);
            var competition = EntityCopier.CreateAuditableCopy(DatabaseFixture.TestData.Competitions.First(seasonPredicate));
            var previousSeason = competition.Seasons[0];

            var newSeason = new Season
            {
                Competition = competition,
                FromYear = previousSeason.FromYear + 1,
                UntilYear = previousSeason.UntilYear + 1,

                // These teams should be ignored
                Teams = DatabaseFixture.TestData.Teams
                            .Where(t => t.TeamId is not null && !previousSeason.Teams.Select(pt => pt.Team?.TeamId).OfType<Guid>().Contains(t.TeamId.Value))
                            .Take(previousSeason.Teams.Count + 2)
                            .Select(t => new TeamInSeason
                            {
                                Team = t
                            })
                            .ToList()
            };

            var result = await Repository.CreateSeason(newSeason, MemberKey, MemberName).ConfigureAwait(false);

            var expectedTeams = previousSeason.Teams.Where(t => t.WithdrawnDate is null
                                                             && t.Team!.UntilYear is null || t.Team!.UntilYear > newSeason.FromYear).ToList();

            Assert.NotNull(result.SeasonId);
            Assert.NotEqual(default, result.SeasonId);
            Assert.NotEqual(previousSeason.SeasonId, result.SeasonId);
            Assert.Equal(expectedTeams.Count, result.Teams.Count);
            foreach (var previousTeam in expectedTeams)
            {
                Assert.Contains(result.Teams, teamInSeason => teamInSeason.Team?.TeamId == previousTeam.Team?.TeamId
                                                           && teamInSeason.Season?.SeasonId == result.SeasonId
                                                           && teamInSeason.WithdrawnDate is null);
            }

            using (var connection = DatabaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                var savedTeams = await connection.QueryAsync<(Guid TeamId, DateTimeOffset? WithdrawnDate)>(
                     @$"SELECT TeamId, WithdrawnDate
                      FROM {Tables.SeasonTeam}
                      WHERE SeasonId = @SeasonId",
                     new { result.SeasonId }).ConfigureAwait(false);

                Assert.Equal(expectedTeams.Count, savedTeams.Count());
                foreach (var previousTeam in expectedTeams)
                {
                    Assert.Contains(savedTeams, teamInSeason => teamInSeason.TeamId == previousTeam.Team?.TeamId
                                                             && teamInSeason.WithdrawnDate is null);
                }
            }
        }

        [Fact]
        public async Task Audits_and_logs()
        {
            var competition = DatabaseFixture.TestData.Competitions.First();
            var fromYear = competition.Seasons.Select(s => s.FromYear).Max() + 1;
            var season = new Season
            {
                Competition = competition,
                FromYear = fromYear,
                UntilYear = fromYear
            };

            _ = await Repository.CreateSeason(season, MemberKey, MemberName);

            AuditRepository.Verify(x => x.CreateAudit(It.IsAny<AuditRecord>(), It.IsAny<IDbTransaction>()), Times.Once);
            Logger.Verify(x => x.Info(LoggingTemplates.Created,
                                       It.Is<Season>(x => x.Competition!.CompetitionId == season.Competition.CompetitionId
                                                                        && x.FromYear == season.FromYear
                                                                        && x.UntilYear == season.UntilYear),
                                       MemberName, MemberKey, typeof(SqlServerSeasonRepository), nameof(SqlServerSeasonRepository.CreateSeason)));
        }
    }
}
