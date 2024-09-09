using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;
using AngleSharp.Css.Dom;
using Dapper;
using Ganss.Xss;
using Moq;
using Stoolball.Competitions;
using Stoolball.Data.Abstractions;
using Stoolball.Data.SqlServer.IntegrationTests.Fixtures;
using Stoolball.Logging;
using Stoolball.Matches;
using Stoolball.MatchLocations;
using Stoolball.Routing;
using Stoolball.Teams;
using Stoolball.Testing;
using Xunit;
using static Stoolball.Constants;

namespace Stoolball.Data.SqlServer.IntegrationTests.Matches
{
    [Collection(IntegrationTestConstants.TestDataIntegrationTestCollection)]
    public class SqlServerTournamentRepositoryTests : IDisposable
    {
        private readonly SqlServerTestDataFixture _databaseFixture;
        private readonly TransactionScope _scope;
        private readonly Mock<IAuditRepository> _auditRepository = new();
        private readonly Mock<ILogger<SqlServerTournamentRepository>> _logger = new();
        private readonly Mock<IRouteGenerator> _routeGenerator = new();
        private readonly Mock<IRedirectsRepository> _redirectsRepository = new();
        private readonly Mock<ITeamRepository> _teamRepository = new();
        private readonly Mock<IMatchRepository> _matchRepository = new();
        private readonly Mock<IHtmlSanitizer> _htmlSanitizer = new();
        private readonly Mock<IStoolballEntityCopier> _copier = new();

        public SqlServerTournamentRepositoryTests(SqlServerTestDataFixture databaseFixture)
        {
            _databaseFixture = databaseFixture ?? throw new ArgumentNullException(nameof(databaseFixture));
            _scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);

            _htmlSanitizer.Setup(x => x.AllowedTags).Returns(new HashSet<string>());
            _htmlSanitizer.Setup(x => x.AllowedAttributes).Returns(new HashSet<string>());
            _htmlSanitizer.Setup(x => x.AllowedCssProperties).Returns(new HashSet<string>());
            _htmlSanitizer.Setup(x => x.AllowedAtRules).Returns(new HashSet<CssRuleType>());
        }

        private SqlServerTournamentRepository CreateRepository()
        {
            return new SqlServerTournamentRepository(
                _databaseFixture.ConnectionFactory,
                new DapperWrapper(),
                _auditRepository.Object,
                _logger.Object,
                _routeGenerator.Object,
                _redirectsRepository.Object,
                _teamRepository.Object,
                _matchRepository.Object,
                _htmlSanitizer.Object,
                _copier.Object);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Create_tournament_inserts_basic_fields(bool hasLocation)
        {
            var tournament = new Tournament
            {
                TournamentName = "Tournament name",
                TournamentLocation = hasLocation ? new MatchLocation { MatchLocationId = _databaseFixture.TestData.MatchLocations.First().MatchLocationId } : null,
                PlayerType = PlayerType.JuniorBoys,
                PlayersPerTeam = 10,
                QualificationType = TournamentQualificationType.OpenTournament,
                StartTime = new DateTimeOffset(2022, 8, 1, 12, 30, 0, TimeSpan.FromHours(1)), // deliberately British Summer Time
                StartTimeIsKnown = true,
                TournamentNotes = "<h1>h1 not allowed but <strong>is allowed</strong></h1>",
                MemberKey = Guid.NewGuid()
            };

            var expectedRoute = "/tournaments/example-tournament";
            _routeGenerator.Setup(x => x.GenerateUniqueRoute("/tournaments", tournament.TournamentName + " " + tournament.StartTime.Date.ToString("dMMMyyyy", CultureInfo.CurrentCulture), NoiseWords.TournamentRoute, It.IsAny<Func<string, Task<int>>>())).Returns(Task.FromResult(expectedRoute));

            SetupEntityCopierMock(tournament, tournament, tournament);
            var sanitizedNotes = tournament.TournamentNotes.Replace("<h1>", string.Empty).Replace("</h1>", string.Empty);
            _htmlSanitizer.Setup(x => x.Sanitize(tournament.TournamentNotes, string.Empty, null)).Returns(sanitizedNotes);

            var repo = CreateRepository();

            var createdTournament = await repo.CreateTournament(tournament, tournament.MemberKey.Value, "Member name").ConfigureAwait(false);

            using (var connection = _databaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                var result = await connection.QuerySingleOrDefaultAsync<Tournament>(@$"
                        SELECT TournamentName, PlayerType, PlayersPerTeam, QualificationType, StartTime, StartTimeIsKnown, TournamentNotes, TournamentRoute, MemberKey 
                        FROM {Tables.Tournament} WHERE TournamentId = @TournamentId", new { createdTournament.TournamentId });

                Assert.NotNull(result);
                Assert.Equal(tournament.TournamentName, result.TournamentName);
                Assert.Equal(tournament.PlayerType.ToString(), result.PlayerType.ToString());
                Assert.Equal(tournament.PlayersPerTeam, result.PlayersPerTeam);
                Assert.Equal(tournament.QualificationType.ToString(), result.QualificationType.ToString());
                Assert.Equal(tournament.StartTime.UtcDateTime, result.StartTime.UtcDateTime);
                Assert.Equal(tournament.StartTimeIsKnown, result.StartTimeIsKnown);
                Assert.Equal(sanitizedNotes, result.TournamentNotes);
                Assert.Equal(expectedRoute, result.TournamentRoute);
                Assert.Equal(tournament.MemberKey, result.MemberKey);

                var matchLocationId = await connection.ExecuteScalarAsync<Guid?>(@$"SELECT MatchLocationId FROM {Tables.Tournament} WHERE TournamentId = @TournamentId", new { createdTournament.TournamentId });
                if (hasLocation)
                {
                    Assert.Equal(tournament.TournamentLocation!.MatchLocationId, matchLocationId);
                }
                else
                {
                    Assert.Null(result.TournamentLocation);
                }
            }

        }

        [Fact]
        public async Task Create_tournament_inserts_teams()
        {
            var tournament = new Tournament
            {
                TournamentName = "Example tournament",
                StartTime = DateTime.Now.AddDays(1),
                TournamentRoute = "/tournaments/example-tournament",
                Teams = _databaseFixture.TestData.Teams.Take(2).Select(x => new TeamInTournament { Team = new Team { TeamId = x.TeamId }, TeamRole = TournamentTeamRole.Confirmed, PlayingAsTeamName = x.TeamName + "xxx" }).ToList()
            };

            _routeGenerator.Setup(x => x.GenerateUniqueRoute("/tournaments", It.IsAny<string>(), NoiseWords.TournamentRoute, It.IsAny<Func<string, Task<int>>>())).Returns(Task.FromResult(tournament.TournamentRoute));

            SetupEntityCopierMock(tournament, tournament, tournament);

            var repo = CreateRepository();

            var createdTournament = await repo.CreateTournament(tournament, Guid.NewGuid(), "Member name");

            using (var connection = _databaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                var results = await connection.QueryAsync<(Guid teamId, string teamRole, string playingAs)?>(@$"
                        SELECT TeamId, TeamRole, PlayingAsTeamName
                        FROM {Tables.TournamentTeam} WHERE TournamentId = @TournamentId", new { createdTournament.TournamentId });

                Assert.Equal(tournament.Teams.Count, results.Count());
                foreach (var team in tournament.Teams)
                {
                    var result = results.SingleOrDefault(x => x?.teamId == team.Team.TeamId);
                    Assert.NotNull(result);

                    Assert.Equal(team.TeamRole.ToString(), result?.teamRole);
                    Assert.Equal(team.PlayingAsTeamName, result?.playingAs);
                }
            }
        }


        [Fact]
        public async Task Create_tournament_inserts_seasons()
        {
            var tournament = new Tournament
            {
                TournamentName = "Example tournament",
                StartTime = DateTime.Now.AddDays(1),
                TournamentRoute = "/tournaments/example-tournament",
                Seasons = _databaseFixture.TestData.Seasons.Take(2).Select(x => new Season { SeasonId = x.SeasonId }).ToList()
            };

            _routeGenerator.Setup(x => x.GenerateUniqueRoute("/tournaments", It.IsAny<string>(), NoiseWords.TournamentRoute, It.IsAny<Func<string, Task<int>>>())).Returns(Task.FromResult(tournament.TournamentRoute));

            SetupEntityCopierMock(tournament, tournament, tournament);

            var repo = CreateRepository();

            var createdTournament = await repo.CreateTournament(tournament, Guid.NewGuid(), "Member name");

            using (var connection = _databaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                var results = await connection.QueryAsync<Guid>(@$"SELECT SeasonId FROM {Tables.TournamentSeason} WHERE TournamentId = @TournamentId", new { createdTournament.TournamentId });

                Assert.Equal(tournament.Seasons.Count, results.Count());
                foreach (var season in tournament.Seasons)
                {
                    Assert.Contains(season.SeasonId!.Value, results);
                }
            }
        }

        [Fact]
        public async Task Create_tournament_should_insert_default_overset_if_overs_specified()
        {
            var tournament = new Tournament
            {
                TournamentName = "Example tournament",
                StartTime = DateTime.Now.AddDays(1),
                DefaultOverSets = new List<OverSet> {
                    new OverSet { OverSetNumber = 1, Overs = 4, BallsPerOver = 8 }
                },
                TournamentRoute = "/tournaments/example-tournament"
            };

            _routeGenerator.Setup(x => x.GenerateUniqueRoute("/tournaments", It.IsAny<string>(), NoiseWords.TournamentRoute, It.IsAny<Func<string, Task<int>>>())).Returns(Task.FromResult(tournament.TournamentRoute));

            SetupEntityCopierMock(tournament, tournament, tournament);

            var repo = CreateRepository();

            var createdTournament = await repo.CreateTournament(tournament, Guid.NewGuid(), "Member name");

            using (var connection = _databaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                var result = await connection.QuerySingleOrDefaultAsync<OverSet>(@$"
                        SELECT OverSetNumber, Overs, BallsPerOver 
                        FROM {Tables.OverSet} WHERE TournamentId = @TournamentId", new { createdTournament.TournamentId });

                Assert.NotNull(result);
                Assert.Equal(tournament.DefaultOverSets[0].OverSetNumber, result.OverSetNumber);
                Assert.Equal(tournament.DefaultOverSets[0].Overs, result.Overs);
                Assert.Equal(tournament.DefaultOverSets[0].BallsPerOver, result.BallsPerOver);
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Update_tournament_updates_basic_fields(bool hasLocation)
        {
            var tournament = _databaseFixture.TestData.TournamentInThePastWithFullDetails!;
            var toBeSaved = new Tournament
            {
                TournamentId = tournament.TournamentId,
                TournamentName = tournament.TournamentName + "xxx",
                TournamentLocation = hasLocation ? new MatchLocation { MatchLocationId = _databaseFixture.TestData.MatchLocations.First(x => x.MatchLocationId != tournament.TournamentLocation.MatchLocationId).MatchLocationId } : null,
                PlayerType = tournament.PlayerType == PlayerType.Ladies ? PlayerType.Mixed : PlayerType.Ladies,
                PlayersPerTeam = tournament.PlayersPerTeam + 2,
                QualificationType = tournament.QualificationType == TournamentQualificationType.OpenTournament ? TournamentQualificationType.ClosedTournament : TournamentQualificationType.OpenTournament,
                StartTime = tournament.StartTime.AddDays(2),
                StartTimeIsKnown = !tournament.StartTimeIsKnown,
                TournamentNotes = tournament.TournamentNotes,
                TournamentRoute = tournament.TournamentRoute
            };

            var expectedRoute = "/tournaments/" + Guid.NewGuid();
            _routeGenerator.Setup(x => x.GenerateUniqueRoute(tournament.TournamentRoute,
                                                             "/tournaments",
                                                             toBeSaved.TournamentName + " " + toBeSaved.StartTime.Date.ToString("dMMMyyyy", CultureInfo.CurrentCulture),
                                                             NoiseWords.TournamentRoute,
                                                             It.IsAny<Func<string, Task<int>>>())).Returns(Task.FromResult(expectedRoute));

            SetupEntityCopierMock(toBeSaved, toBeSaved, toBeSaved);

            var sanitizedNotes = tournament.TournamentNotes + "xxx";
            _htmlSanitizer.Setup(x => x.Sanitize(toBeSaved.TournamentNotes, string.Empty, null)).Returns(sanitizedNotes);

            var repo = CreateRepository();

            var updatedTournament = await repo.UpdateTournament(toBeSaved, Guid.NewGuid(), "Member name");

            using (var connection = _databaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                var result = await connection.QuerySingleOrDefaultAsync<Tournament>(@$"
                        SELECT TournamentName, PlayerType, PlayersPerTeam, QualificationType, StartTime, StartTimeIsKnown, TournamentNotes, TournamentRoute 
                        FROM {Tables.Tournament} WHERE TournamentId = @TournamentId", new { updatedTournament.TournamentId });

                Assert.NotNull(result);
                Assert.Equal(toBeSaved.TournamentName, result.TournamentName);
                Assert.Equal(toBeSaved.PlayerType.ToString(), result.PlayerType.ToString());
                Assert.Equal(toBeSaved.PlayersPerTeam, result.PlayersPerTeam);
                Assert.Equal(toBeSaved.QualificationType.ToString(), result.QualificationType.ToString());
                Assert.Equal(toBeSaved.StartTime.UtcDateTime, result.StartTime.UtcDateTime);
                Assert.Equal(toBeSaved.StartTimeIsKnown, result.StartTimeIsKnown);
                Assert.Equal(sanitizedNotes, result.TournamentNotes);
                Assert.Equal(expectedRoute, result.TournamentRoute);

                var matchLocationId = await connection.ExecuteScalarAsync<Guid?>(@$"SELECT MatchLocationId FROM {Tables.Tournament} WHERE TournamentId = @TournamentId", new { updatedTournament.TournamentId });
                if (hasLocation)
                {
                    Assert.Equal(toBeSaved.TournamentLocation!.MatchLocationId, matchLocationId);
                }
                else
                {
                    Assert.Null(result.TournamentLocation);
                }
            }
        }


        [Fact]
        public async Task Create_tournament_should_not_insert_default_overset_if_overs_not_specified()
        {
            var tournament = new Tournament
            {
                TournamentName = "Example tournament",
                StartTime = DateTime.Now.AddDays(1),
                DefaultOverSets = new List<OverSet>(),
                TournamentRoute = "/tournaments/example-tournament"
            };

            _routeGenerator.Setup(x => x.GenerateUniqueRoute("/tournaments", It.IsAny<string>(), NoiseWords.TournamentRoute, It.IsAny<Func<string, Task<int>>>())).Returns(Task.FromResult(tournament.TournamentRoute));

            SetupEntityCopierMock(tournament, tournament, tournament);

            var repo = CreateRepository();

            var createdTournament = await repo.CreateTournament(tournament, Guid.NewGuid(), "Member name");

            using (var connection = _databaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                var result = await connection.QuerySingleOrDefaultAsync<OverSet>(@$"
                        SELECT OverSetNumber, Overs, BallsPerOver 
                        FROM {Tables.OverSet} WHERE TournamentId = @TournamentId", new { createdTournament.TournamentId });

                Assert.Null(result);
            }
        }

        [Fact]
        public async Task Update_tournament_should_update_existing_default_overset_if_overs_specified()
        {
            var tournament = _databaseFixture.TestData.TournamentInThePastWithFullDetails!;

            var auditable = new Tournament
            {
                TournamentId = tournament.TournamentId,
                TournamentName = "Example tournament",
                StartTime = tournament.StartTime,
                TournamentRoute = tournament.TournamentRoute,
                DefaultOverSets = new List<OverSet>
                {
                    new OverSet {
                        OverSetId = tournament.DefaultOverSets[0].OverSetId,
                        OverSetNumber = tournament.DefaultOverSets[0].OverSetNumber,
                        Overs = tournament.DefaultOverSets[0].Overs*2,
                        BallsPerOver = tournament.DefaultOverSets[0].BallsPerOver*2
                    }
                }
            };

            _routeGenerator.Setup(x => x.GenerateUniqueRoute(tournament.TournamentRoute, "/tournaments", It.IsAny<string>(), NoiseWords.TournamentRoute, It.IsAny<Func<string, Task<int>>>())).Returns(Task.FromResult(tournament.TournamentRoute));

            SetupEntityCopierMock(tournament, auditable, auditable);

            var repo = CreateRepository();

            _ = await repo.UpdateTournament(tournament, Guid.NewGuid(), "Member name");

            using (var connection = _databaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                var result = await connection.QuerySingleOrDefaultAsync<OverSet>(@$"
                        SELECT OverSetNumber, Overs, BallsPerOver 
                        FROM {Tables.OverSet} 
                        WHERE TournamentId = @TournamentId AND OverSetId = @OverSetId", new { tournament.TournamentId, tournament.DefaultOverSets[0].OverSetId });

                Assert.NotNull(result);
                Assert.Equal(auditable.DefaultOverSets[0].OverSetNumber, result.OverSetNumber);
                Assert.Equal(auditable.DefaultOverSets[0].Overs, result.Overs);
                Assert.Equal(auditable.DefaultOverSets[0].BallsPerOver, result.BallsPerOver);
            }
        }

        [Fact]
        public async Task Update_tournament_in_the_future_should_update_existing_match_overset_if_overs_specified()
        {
            var tournament = _databaseFixture.TestData.TournamentInThePastWithFullDetails!;

            var auditable = new Tournament
            {
                TournamentId = tournament.TournamentId,
                TournamentName = "Example tournament",
                StartTime = DateTimeOffset.Now.AddDays(1),
                TournamentRoute = tournament.TournamentRoute,
                DefaultOverSets = new List<OverSet>
                {
                    new OverSet {
                        OverSetId = tournament.DefaultOverSets[0].OverSetId,
                        OverSetNumber = tournament.DefaultOverSets[0].OverSetNumber,
                        Overs = tournament.DefaultOverSets[0].Overs*2,
                        BallsPerOver = tournament.DefaultOverSets[0].BallsPerOver*2
                    }
                }
            };

            _routeGenerator.Setup(x => x.GenerateUniqueRoute(tournament.TournamentRoute, "/tournaments", It.IsAny<string>(), NoiseWords.TournamentRoute, It.IsAny<Func<string, Task<int>>>())).Returns(Task.FromResult(tournament.TournamentRoute));

            SetupEntityCopierMock(tournament, auditable, auditable);

            var repo = CreateRepository();

            _ = await repo.UpdateTournament(tournament, Guid.NewGuid(), "Member name");

            using (var connection = _databaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                foreach (var tournamentMatch in tournament.Matches)
                {
                    var match = _databaseFixture.TestData.Matches.Single(x => x.MatchId == tournamentMatch.MatchId);
                    foreach (var matchInnings in match.MatchInnings)
                    {
                        var result = await connection.QuerySingleOrDefaultAsync<OverSet>(@$"
                            SELECT OverSetNumber, Overs, BallsPerOver 
                            FROM {Tables.OverSet} 
                            WHERE TournamentId IS NULL AND OverSetId = @OverSetId", new { matchInnings.OverSets[0].OverSetId });

                        Assert.NotNull(result);
                        Assert.Equal(auditable.DefaultOverSets[0].OverSetNumber, result.OverSetNumber);
                        Assert.Equal(auditable.DefaultOverSets[0].Overs, result.Overs);
                        Assert.Equal(auditable.DefaultOverSets[0].BallsPerOver, result.BallsPerOver);
                    }
                }
            }
        }

        [Fact]
        public async Task Update_tournament_in_the_past_should_not_update_match_overset_if_overs_specified()
        {
            var tournament = _databaseFixture.TestData.TournamentInThePastWithFullDetails!;

            var auditable = new Tournament
            {
                TournamentId = tournament.TournamentId,
                TournamentName = "Example tournament",
                StartTime = DateTimeOffset.UtcNow.AddDays(-1),
                TournamentRoute = tournament.TournamentRoute,
                DefaultOverSets = new List<OverSet>
                {
                    new OverSet {
                        OverSetId = tournament.DefaultOverSets[0].OverSetId,
                        OverSetNumber = tournament.DefaultOverSets[0].OverSetNumber,
                        Overs = tournament.DefaultOverSets[0].Overs*2,
                        BallsPerOver = tournament.DefaultOverSets[0].BallsPerOver*2
                    }
                }
            };

            _routeGenerator.Setup(x => x.GenerateUniqueRoute(tournament.TournamentRoute, "/tournaments", It.IsAny<string>(), NoiseWords.TournamentRoute, It.IsAny<Func<string, Task<int>>>())).Returns(Task.FromResult(tournament.TournamentRoute));

            SetupEntityCopierMock(tournament, auditable, auditable);

            var repo = CreateRepository();

            _ = await repo.UpdateTournament(tournament, Guid.NewGuid(), "Member name");

            using (var connection = _databaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                foreach (var tournamentMatch in tournament.Matches)
                {
                    var match = _databaseFixture.TestData.Matches.Single(x => x.MatchId == tournamentMatch.MatchId);
                    foreach (var matchInnings in match.MatchInnings)
                    {
                        var result = await connection.QuerySingleOrDefaultAsync<OverSet>(@$"
                            SELECT OverSetNumber, Overs, BallsPerOver 
                            FROM {Tables.OverSet} 
                            WHERE TournamentId IS NULL AND OverSetId = @OverSetId", new { matchInnings.OverSets[0].OverSetId });

                        Assert.NotNull(result);
                        Assert.Equal(matchInnings.OverSets[0].OverSetNumber, result.OverSetNumber);
                        Assert.Equal(matchInnings.OverSets[0].Overs, result.Overs);
                        Assert.Equal(matchInnings.OverSets[0].BallsPerOver, result.BallsPerOver);
                    }
                }
            }
        }

        [Fact]
        public async Task Update_tournament_should_clear_default_overset_if_overs_not_specified()
        {
            var tournament = _databaseFixture.TestData.TournamentInThePastWithFullDetails!;

            var auditable = new Tournament
            {
                TournamentId = tournament.TournamentId,
                TournamentName = "Example tournament",
                StartTime = tournament.StartTime,
                TournamentRoute = tournament.TournamentRoute,
                DefaultOverSets = new List<OverSet>()
            };

            _routeGenerator.Setup(x => x.GenerateUniqueRoute(tournament.TournamentRoute, "/tournaments", It.IsAny<string>(), NoiseWords.TournamentRoute, It.IsAny<Func<string, Task<int>>>())).Returns(Task.FromResult(tournament.TournamentRoute));

            SetupEntityCopierMock(tournament, auditable, auditable);

            var repo = CreateRepository();

            _ = await repo.UpdateTournament(tournament, Guid.NewGuid(), "Member name");

            using (var connection = _databaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                var result = await connection.QuerySingleOrDefaultAsync<OverSet>(@$"
                        SELECT OverSetNumber, Overs, BallsPerOver 
                        FROM {Tables.OverSet} 
                        WHERE TournamentId = @TournamentId", new { tournament.TournamentId });

                Assert.Null(result);
            }
        }

        [Fact]
        public async Task Update_tournament_in_the_future_should_not_clear_match_overset_if_overs_not_specified()
        {
            var tournament = _databaseFixture.TestData.TournamentInThePastWithFullDetails!;

            var auditable = new Tournament
            {
                TournamentId = tournament.TournamentId,
                TournamentName = "Example tournament",
                StartTime = DateTimeOffset.UtcNow.AddDays(1),
                TournamentRoute = tournament.TournamentRoute,
                DefaultOverSets = new List<OverSet>()
            };

            _routeGenerator.Setup(x => x.GenerateUniqueRoute(tournament.TournamentRoute, "/tournaments", It.IsAny<string>(), NoiseWords.TournamentRoute, It.IsAny<Func<string, Task<int>>>())).Returns(Task.FromResult(tournament.TournamentRoute));

            SetupEntityCopierMock(tournament, auditable, auditable);

            var repo = CreateRepository();

            _ = await repo.UpdateTournament(tournament, Guid.NewGuid(), "Member name");

            using (var connection = _databaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                foreach (var tournamentMatch in tournament.Matches)
                {
                    var match = _databaseFixture.TestData.Matches.Single(x => x.MatchId == tournamentMatch.MatchId);
                    foreach (var matchInnings in match.MatchInnings)
                    {
                        var result = await connection.QuerySingleOrDefaultAsync<OverSet>(@$"
                            SELECT OverSetNumber, Overs, BallsPerOver 
                            FROM {Tables.OverSet} 
                            WHERE TournamentId IS NULL AND OverSetId = @OverSetId", new { matchInnings.OverSets[0].OverSetId });

                        Assert.NotNull(result);
                        Assert.Equal(matchInnings.OverSets[0].OverSetNumber, result.OverSetNumber);
                        Assert.Equal(matchInnings.OverSets[0].Overs, result.Overs);
                        Assert.Equal(matchInnings.OverSets[0].BallsPerOver, result.BallsPerOver);
                    }
                }
            }
        }

        [Fact]
        public async Task Update_tournament_updates_matches_from_tournament_details()
        {
            var tournament = _databaseFixture.TestData.TournamentInThePastWithFullDetails!;

            var tournamentWithChanges = new Tournament
            {
                TournamentId = tournament.TournamentId,
                TournamentName = "Example tournament",
                TournamentRoute = tournament.TournamentRoute,
                DefaultOverSets = new List<OverSet>(),
                // Change these fields and check they are copied to matches
                StartTime = tournament.StartTime.AddDays(1),
                PlayersPerTeam = tournament.PlayersPerTeam + 2,
                PlayerType = tournament.PlayerType == PlayerType.Mixed ? PlayerType.Ladies : PlayerType.Mixed,
                TournamentLocation = new MatchLocation { MatchLocationId = _databaseFixture.TestData.MatchLocations.First(x => x.MatchLocationId != tournament.TournamentLocation?.MatchLocationId).MatchLocationId },
            };

            _routeGenerator.Setup(x => x.GenerateUniqueRoute(tournament.TournamentRoute, "/tournaments", It.IsAny<string>(), NoiseWords.TournamentRoute, It.IsAny<Func<string, Task<int>>>())).Returns(Task.FromResult(tournament.TournamentRoute));

            SetupEntityCopierMock(tournament, tournamentWithChanges, tournamentWithChanges);

            var repo = CreateRepository();

            _ = await repo.UpdateTournament(tournament, Guid.NewGuid(), "Member name");

            using (var connection = _databaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                foreach (var tournamentMatch in tournament.Matches)
                {
                    var updatedMatch = await connection.QuerySingleOrDefaultAsync<Stoolball.Matches.Match>($"SELECT PlayerType, PlayersPerTeam, OrderInTournament, StartTime FROM {Tables.Match} WHERE MatchId = @MatchId", tournamentMatch);
                    var updatedMatchLocation = await connection.ExecuteScalarAsync<Guid?>($"SELECT MatchLocationId FROM {Tables.Match} WHERE MatchId = @MatchId", tournamentMatch);

                    Assert.NotNull(updatedMatch);
                    Assert.Equal(tournamentWithChanges.PlayerType, updatedMatch.PlayerType);
                    Assert.Equal(tournamentWithChanges.PlayersPerTeam, updatedMatch.PlayersPerTeam);
                    Assert.Equal(tournamentWithChanges.StartTime.AccurateToTheMinute().AddMinutes(((updatedMatch.OrderInTournament ?? 1) - 1) * 45), updatedMatch.StartTime.AccurateToTheMinute());
                    Assert.Equal(tournamentWithChanges.TournamentLocation.MatchLocationId, updatedMatchLocation);
                }
            }
        }

        [Fact]
        public async Task Delete_tournament_succeeds()
        {
            var auditable = new Tournament { TournamentId = _databaseFixture.TestData.TournamentInThePastWithFullDetails!.TournamentId };
            var redacted = new Tournament { TournamentId = _databaseFixture.TestData.TournamentInThePastWithFullDetails.TournamentId };

            SetupEntityCopierMock(_databaseFixture.TestData.TournamentInThePastWithFullDetails, auditable, redacted);

            var memberKey = Guid.NewGuid();
            var memberName = "Dee Leeter";

            var repo = CreateRepository();

            await repo.DeleteTournament(_databaseFixture.TestData.TournamentInThePastWithFullDetails, memberKey, memberName);

            using (var connection = _databaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                var result = await connection.QuerySingleOrDefaultAsync<Guid?>($"SELECT TournamentId FROM {Tables.Tournament} WHERE TournamentId = @TournamentId", new { _databaseFixture.TestData.TournamentInThePastWithFullDetails.TournamentId });
                Assert.Null(result);
            }
        }

        private void SetupEntityCopierMock(Tournament original, Tournament auditable, Tournament redacted)
        {
            _copier.Setup(x => x.CreateAuditableCopy(original)).Returns(auditable);
            _copier.Setup(x => x.CreateRedactedCopy(auditable)).Returns(redacted);
        }

        public void Dispose() => _scope.Dispose();
    }
}
