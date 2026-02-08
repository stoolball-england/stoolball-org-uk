using Stoolball.MatchLocations;
using Stoolball.Teams;

namespace Stoolball.Data.SqlServer.IntegrationTests.Matches.Tournaments
{
    [Collection(IntegrationTestConstants.TestDataIntegrationTestCollection)]
    public class CreateTournamentTests : TournamentRepositoryTestsBase
    {
        private const string EXPECTED_ROUTE = "/tournaments/example-tournament";

        public CreateTournamentTests(SqlServerTestDataFixture databaseFixture) : base(databaseFixture)
        {
            RouteGenerator.Setup(x => x.GenerateUniqueRoute("/tournaments", It.IsAny<string>(), NoiseWords.TournamentRoute, It.IsAny<Func<string, Task<int>>>())).Returns(Task.FromResult(EXPECTED_ROUTE));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Inserts_basic_fields(bool hasLocation)
        {
            var tournament = new Tournament
            {
                TournamentName = "Tournament name",
                TournamentLocation = hasLocation ? new MatchLocation { MatchLocationId = DatabaseFixture.TestData.MatchLocations.First().MatchLocationId } : null,
                PlayerType = PlayerType.JuniorBoys,
                PlayersPerTeam = 10,
                QualificationType = TournamentQualificationType.OpenTournament,
                StartTime = new DateTimeOffset(2022, 8, 1, 12, 30, 0, TimeSpan.FromHours(1)), // deliberately British Summer Time
                StartTimeIsKnown = true,
                TournamentNotes = "<h1>h1 not allowed but <strong>is allowed</strong></h1>",
                MemberKey = Guid.NewGuid()
            };

            var sanitizedNotes = tournament.TournamentNotes.Replace("<h1>", string.Empty).Replace("</h1>", string.Empty);
            HtmlSanitizer.Setup(x => x.Sanitize(tournament.TournamentNotes, string.Empty, null)).Returns(sanitizedNotes);

            var createdTournament = await Repository.CreateTournament(tournament, tournament.MemberKey.Value, MemberName).ConfigureAwait(false);

            Assert.NotNull(createdTournament?.TournamentId);
            Assert.NotEqual(default, createdTournament!.TournamentId);
            Assert.Equal(tournament.TournamentName, createdTournament.TournamentName);
            Assert.Equal(tournament.PlayerType.ToString(), createdTournament.PlayerType.ToString());
            Assert.Equal(tournament.PlayersPerTeam, createdTournament.PlayersPerTeam);
            Assert.Equal(tournament.QualificationType.ToString(), createdTournament.QualificationType.ToString());
            Assert.Equal(tournament.StartTime.UtcDateTime, createdTournament.StartTime.UtcDateTime);
            Assert.Equal(tournament.StartTimeIsKnown, createdTournament.StartTimeIsKnown);
            Assert.Equal(sanitizedNotes, createdTournament.TournamentNotes);
            Assert.Equal(EXPECTED_ROUTE, createdTournament.TournamentRoute);
            Assert.Equal(tournament.MemberKey, createdTournament.MemberKey);
            if (hasLocation)
            {
                Assert.Equal(tournament.TournamentLocation!.MatchLocationId, createdTournament.TournamentLocation?.MatchLocationId);
            }
            else
            {
                Assert.Null(createdTournament.TournamentLocation);
            }

            using (var connection = DatabaseFixture.ConnectionFactory.CreateDatabaseConnection())
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
                Assert.Equal(EXPECTED_ROUTE, result.TournamentRoute);
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
        public async Task Inserts_teams()
        {
            var tournament = new Tournament
            {
                TournamentName = "Example tournament",
                StartTime = DateTime.Now.AddDays(1),
                TournamentRoute = EXPECTED_ROUTE,
                Teams = DatabaseFixture.TestData.Teams.Take(2).Select(x => new TeamInTournament { Team = new Team { TeamId = x.TeamId }, TeamRole = TournamentTeamRole.Confirmed, PlayingAsTeamName = x.TeamName + "xxx" }).ToList()
            };

            var createdTournament = await Repository.CreateTournament(tournament, MemberKey, MemberName);

            Assert.NotNull(createdTournament?.TournamentId);
            Assert.NotEqual(default, createdTournament!.TournamentId);
            Assert.Equal(tournament.Teams.Count, createdTournament.Teams.Count);
            foreach (var team in tournament.Teams)
            {
                Assert.Contains(createdTournament.Teams, t => t.Team?.TeamId == team.Team?.TeamId
                                                      && t.TeamRole == team.TeamRole
                                                      && t.PlayingAsTeamName == team.PlayingAsTeamName);
            }

            using (var connection = DatabaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                var results = await connection.QueryAsync<(Guid teamId, string teamRole, string playingAs)?>(@$"
                        SELECT TeamId, TeamRole, PlayingAsTeamName
                        FROM {Tables.TournamentTeam} WHERE TournamentId = @TournamentId", new { createdTournament.TournamentId });

                Assert.Equal(tournament.Teams.Count, results.Count());
                foreach (var team in tournament.Teams)
                {
                    var result = results.SingleOrDefault(x => x?.teamId == team.Team!.TeamId);
                    Assert.NotNull(result);

                    Assert.Equal(team.TeamRole.ToString(), result?.teamRole);
                    Assert.Equal(team.PlayingAsTeamName, result?.playingAs);
                }
            }
        }


        [Fact]
        public async Task Inserts_seasons()
        {
            var tournament = new Tournament
            {
                TournamentName = "Example tournament",
                StartTime = DateTime.Now.AddDays(1),
                TournamentRoute = EXPECTED_ROUTE,
                Seasons = DatabaseFixture.TestData.Seasons.Take(2).Select(x => new Season { SeasonId = x.SeasonId }).ToList()
            };

            var createdTournament = await Repository.CreateTournament(tournament, MemberKey, MemberName);

            Assert.NotNull(createdTournament?.TournamentId);
            Assert.NotEqual(default, createdTournament!.TournamentId);
            Assert.Equal(tournament.Seasons.Count, createdTournament.Seasons.Count);
            foreach (var season in tournament.Seasons)
            {
                Assert.Contains(createdTournament.Seasons, s => s.SeasonId == season.SeasonId);
            }

            using (var connection = DatabaseFixture.ConnectionFactory.CreateDatabaseConnection())
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
        public async Task Should_insert_default_overset_if_overs_specified()
        {
            var tournament = new Tournament
            {
                TournamentName = "Example tournament",
                StartTime = DateTime.Now.AddDays(1),
                DefaultOverSets = new List<OverSet> {
                    new OverSet { OverSetNumber = 1, Overs = 4, BallsPerOver = 8 }
                },
                TournamentRoute = EXPECTED_ROUTE
            };

            var createdTournament = await Repository.CreateTournament(tournament, MemberKey, MemberName);

            Assert.NotNull(createdTournament?.TournamentId);
            Assert.NotEqual(default, createdTournament!.TournamentId);
            Assert.Equal(tournament.DefaultOverSets.Count, createdTournament.DefaultOverSets.Count);
            Assert.Contains(createdTournament.DefaultOverSets,
                                o => o.OverSetNumber == tournament.DefaultOverSets[0].OverSetNumber
                             && o.Overs == tournament.DefaultOverSets[0].Overs
                             && o.BallsPerOver == tournament.DefaultOverSets[0].BallsPerOver);

            using (var connection = DatabaseFixture.ConnectionFactory.CreateDatabaseConnection())
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
        public async Task Should_not_insert_default_overset_if_overs_not_specified()
        {
            var tournament = new Tournament
            {
                TournamentName = "Example tournament",
                StartTime = DateTime.Now.AddDays(1),
                DefaultOverSets = new List<OverSet>(),
                TournamentRoute = EXPECTED_ROUTE
            };

            var createdTournament = await Repository.CreateTournament(tournament, MemberKey, MemberName);

            Assert.NotNull(createdTournament?.TournamentId);
            Assert.NotEqual(default, createdTournament!.TournamentId);
            Assert.Empty(createdTournament.DefaultOverSets);

            using (var connection = DatabaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                var result = await connection.QuerySingleOrDefaultAsync<OverSet>(@$"
                        SELECT OverSetNumber, Overs, BallsPerOver 
                        FROM {Tables.OverSet} WHERE TournamentId = @TournamentId", new { createdTournament.TournamentId });

                Assert.Null(result);
            }
        }
    }
}
