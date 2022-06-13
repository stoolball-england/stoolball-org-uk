using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;
using AngleSharp.Css.Dom;
using Dapper;
using Ganss.XSS;
using Moq;
using Stoolball.Data.SqlServer.IntegrationTests.Fixtures;
using Stoolball.Logging;
using Stoolball.Matches;
using Stoolball.Routing;
using Stoolball.Teams;
using Xunit;
using static Stoolball.Constants;

namespace Stoolball.Data.SqlServer.IntegrationTests.Matches
{
    [Collection(IntegrationTestConstants.TestDataIntegrationTestCollection)]
    public class SqlServerTournamentRepositoryTests : IDisposable
    {
        private readonly SqlServerTestDataFixture _databaseFixture;
        private readonly TransactionScope _scope;

        public SqlServerTournamentRepositoryTests(SqlServerTestDataFixture databaseFixture)
        {
            _databaseFixture = databaseFixture ?? throw new ArgumentNullException(nameof(databaseFixture));
            _scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
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

            var routeGenerator = new Mock<IRouteGenerator>();
            routeGenerator.Setup(x => x.GenerateUniqueRoute("/tournaments", It.IsAny<string>(), NoiseWords.TournamentRoute, It.IsAny<Func<string, Task<int>>>())).Returns(Task.FromResult(tournament.TournamentRoute));

            var repo = new SqlServerTournamentRepository(
                _databaseFixture.ConnectionFactory,
                Mock.Of<IAuditRepository>(),
                Mock.Of<ILogger<SqlServerTournamentRepository>>(),
                routeGenerator.Object,
                Mock.Of<IRedirectsRepository>(),
                Mock.Of<ITeamRepository>(),
                Mock.Of<IMatchRepository>(),
                CreateHtmlSanitizerMock().Object,
                CreateEntityCopierMock(tournament, tournament, tournament).Object);

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

            var routeGenerator = new Mock<IRouteGenerator>();
            routeGenerator.Setup(x => x.GenerateUniqueRoute("/tournaments", It.IsAny<string>(), NoiseWords.TournamentRoute, It.IsAny<Func<string, Task<int>>>())).Returns(Task.FromResult(tournament.TournamentRoute));

            var repo = new SqlServerTournamentRepository(
                _databaseFixture.ConnectionFactory,
                Mock.Of<IAuditRepository>(),
                Mock.Of<ILogger<SqlServerTournamentRepository>>(),
                routeGenerator.Object,
                Mock.Of<IRedirectsRepository>(),
                Mock.Of<ITeamRepository>(),
                Mock.Of<IMatchRepository>(),
                CreateHtmlSanitizerMock().Object,
                CreateEntityCopierMock(tournament, tournament, tournament).Object);

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
            var tournament = _databaseFixture.TestData.TournamentWithFullDetails;

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

            var routeGenerator = new Mock<IRouteGenerator>();
            routeGenerator.Setup(x => x.GenerateUniqueRoute(tournament.TournamentRoute, "/tournaments", It.IsAny<string>(), NoiseWords.TournamentRoute, It.IsAny<Func<string, Task<int>>>())).Returns(Task.FromResult(tournament.TournamentRoute));

            var repo = new SqlServerTournamentRepository(
                _databaseFixture.ConnectionFactory,
                Mock.Of<IAuditRepository>(),
                Mock.Of<ILogger<SqlServerTournamentRepository>>(),
                routeGenerator.Object,
                Mock.Of<IRedirectsRepository>(),
                Mock.Of<ITeamRepository>(),
                Mock.Of<IMatchRepository>(),
                CreateHtmlSanitizerMock().Object,
                CreateEntityCopierMock(tournament, auditable, auditable).Object);

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
            var tournament = _databaseFixture.TestData.TournamentWithFullDetails;

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

            var routeGenerator = new Mock<IRouteGenerator>();
            routeGenerator.Setup(x => x.GenerateUniqueRoute(tournament.TournamentRoute, "/tournaments", It.IsAny<string>(), NoiseWords.TournamentRoute, It.IsAny<Func<string, Task<int>>>())).Returns(Task.FromResult(tournament.TournamentRoute));

            var repo = new SqlServerTournamentRepository(
                _databaseFixture.ConnectionFactory,
                Mock.Of<IAuditRepository>(),
                Mock.Of<ILogger<SqlServerTournamentRepository>>(),
                routeGenerator.Object,
                Mock.Of<IRedirectsRepository>(),
                Mock.Of<ITeamRepository>(),
                Mock.Of<IMatchRepository>(),
                CreateHtmlSanitizerMock().Object,
                CreateEntityCopierMock(tournament, auditable, auditable).Object);

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
            var tournament = _databaseFixture.TestData.TournamentWithFullDetails;

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

            var routeGenerator = new Mock<IRouteGenerator>();
            routeGenerator.Setup(x => x.GenerateUniqueRoute(tournament.TournamentRoute, "/tournaments", It.IsAny<string>(), NoiseWords.TournamentRoute, It.IsAny<Func<string, Task<int>>>())).Returns(Task.FromResult(tournament.TournamentRoute));

            var repo = new SqlServerTournamentRepository(
                _databaseFixture.ConnectionFactory,
                Mock.Of<IAuditRepository>(),
                Mock.Of<ILogger<SqlServerTournamentRepository>>(),
                routeGenerator.Object,
                Mock.Of<IRedirectsRepository>(),
                Mock.Of<ITeamRepository>(),
                Mock.Of<IMatchRepository>(),
                CreateHtmlSanitizerMock().Object,
                CreateEntityCopierMock(tournament, auditable, auditable).Object);

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
            var tournament = _databaseFixture.TestData.TournamentWithFullDetails;

            var auditable = new Tournament
            {
                TournamentId = tournament.TournamentId,
                TournamentName = "Example tournament",
                StartTime = tournament.StartTime,
                TournamentRoute = tournament.TournamentRoute,
                DefaultOverSets = new List<OverSet>()
            };

            var routeGenerator = new Mock<IRouteGenerator>();
            routeGenerator.Setup(x => x.GenerateUniqueRoute(tournament.TournamentRoute, "/tournaments", It.IsAny<string>(), NoiseWords.TournamentRoute, It.IsAny<Func<string, Task<int>>>())).Returns(Task.FromResult(tournament.TournamentRoute));

            var repo = new SqlServerTournamentRepository(
                _databaseFixture.ConnectionFactory,
                Mock.Of<IAuditRepository>(),
                Mock.Of<ILogger<SqlServerTournamentRepository>>(),
                routeGenerator.Object,
                Mock.Of<IRedirectsRepository>(),
                Mock.Of<ITeamRepository>(),
                Mock.Of<IMatchRepository>(),
                CreateHtmlSanitizerMock().Object,
                CreateEntityCopierMock(tournament, auditable, auditable).Object);

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
            var tournament = _databaseFixture.TestData.TournamentWithFullDetails;

            var auditable = new Tournament
            {
                TournamentId = tournament.TournamentId,
                TournamentName = "Example tournament",
                StartTime = DateTimeOffset.UtcNow.AddDays(1),
                TournamentRoute = tournament.TournamentRoute,
                DefaultOverSets = new List<OverSet>()
            };

            var routeGenerator = new Mock<IRouteGenerator>();
            routeGenerator.Setup(x => x.GenerateUniqueRoute(tournament.TournamentRoute, "/tournaments", It.IsAny<string>(), NoiseWords.TournamentRoute, It.IsAny<Func<string, Task<int>>>())).Returns(Task.FromResult(tournament.TournamentRoute));

            var repo = new SqlServerTournamentRepository(
                _databaseFixture.ConnectionFactory,
                Mock.Of<IAuditRepository>(),
                Mock.Of<ILogger<SqlServerTournamentRepository>>(),
                routeGenerator.Object,
                Mock.Of<IRedirectsRepository>(),
                Mock.Of<ITeamRepository>(),
                Mock.Of<IMatchRepository>(),
                CreateHtmlSanitizerMock().Object,
                CreateEntityCopierMock(tournament, auditable, auditable).Object);

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
        public async Task Delete_tournament_succeeds()
        {
            var auditable = new Tournament { TournamentId = _databaseFixture.TestData.TournamentWithFullDetails.TournamentId };
            var redacted = new Tournament { TournamentId = _databaseFixture.TestData.TournamentWithFullDetails.TournamentId };

            var sanitizer = CreateHtmlSanitizerMock();
            var copier = CreateEntityCopierMock(_databaseFixture.TestData.TournamentWithFullDetails, auditable, redacted);

            var memberKey = Guid.NewGuid();
            var memberName = "Dee Leeter";

            var repo = new SqlServerTournamentRepository(
                _databaseFixture.ConnectionFactory,
                Mock.Of<IAuditRepository>(),
                Mock.Of<ILogger<SqlServerTournamentRepository>>(),
                Mock.Of<IRouteGenerator>(),
                Mock.Of<IRedirectsRepository>(),
                Mock.Of<ITeamRepository>(),
                Mock.Of<IMatchRepository>(),
                sanitizer.Object,
                copier.Object);

            await repo.DeleteTournament(_databaseFixture.TestData.TournamentWithFullDetails, memberKey, memberName);

            using (var connection = _databaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                var result = await connection.QuerySingleOrDefaultAsync<Guid?>($"SELECT TournamentId FROM {Tables.Tournament} WHERE TournamentId = @TournamentId", new { _databaseFixture.TestData.TournamentWithFullDetails.TournamentId });
                Assert.Null(result);
            }
        }

        private Mock<IStoolballEntityCopier> CreateEntityCopierMock(Tournament original, Tournament auditable, Tournament redacted)
        {
            var copier = new Mock<IStoolballEntityCopier>();
            copier.Setup(x => x.CreateAuditableCopy(original)).Returns(auditable);
            copier.Setup(x => x.CreateAuditableCopy(auditable)).Returns(redacted);
            return copier;
        }

        private static Mock<IHtmlSanitizer> CreateHtmlSanitizerMock()
        {
            var sanitizer = new Mock<IHtmlSanitizer>();
            sanitizer.Setup(x => x.AllowedTags).Returns(new HashSet<string>());
            sanitizer.Setup(x => x.AllowedAttributes).Returns(new HashSet<string>());
            sanitizer.Setup(x => x.AllowedCssProperties).Returns(new HashSet<string>());
            sanitizer.Setup(x => x.AllowedAtRules).Returns(new HashSet<CssRuleType>());
            return sanitizer;
        }

        public void Dispose() => _scope.Dispose();
    }
}
