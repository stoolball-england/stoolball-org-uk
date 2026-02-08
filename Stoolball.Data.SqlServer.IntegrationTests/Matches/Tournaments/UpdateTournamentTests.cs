using System.Globalization;
using Stoolball.MatchLocations;
using Stoolball.Teams;
using Stoolball.Testing;

namespace Stoolball.Data.SqlServer.IntegrationTests.Matches.Tournaments
{
    [Collection(IntegrationTestConstants.TestDataIntegrationTestCollection)]
    public class UpdateTournamentTests : TournamentRepositoryTestsBase
    {
        public UpdateTournamentTests(SqlServerTestDataFixture databaseFixture) : base(databaseFixture)
        {
            RouteGenerator.Setup(x => x.GenerateUniqueRoute(It.IsAny<string>(),
                                                            "/tournaments",
                                                            It.IsAny<string>(),
                                                            NoiseWords.TournamentRoute,
                                                            It.IsAny<Func<string, Task<int>>>())
            )
            .Returns<string, string, string, IEnumerable<string>, Func<string, Task<int>>>((route, _, _, _, _) => Task.FromResult(route));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Updates_basic_fields(bool hasLocation)
        {
            var tournament = Copier.CreateAuditableCopy(DatabaseFixture.TestData.TournamentInThePastWithFullDetails!)!;
            tournament.TournamentName = tournament.TournamentName + "xxx";
            tournament.TournamentLocation = hasLocation ? new MatchLocation { MatchLocationId = DatabaseFixture.TestData.MatchLocations.First(x => x.MatchLocationId != tournament.TournamentLocation.MatchLocationId).MatchLocationId } : null;
            tournament.PlayerType = tournament.PlayerType == PlayerType.Ladies ? PlayerType.Mixed : PlayerType.Ladies;
            tournament.PlayersPerTeam = tournament.PlayersPerTeam + 2;
            tournament.QualificationType = tournament.QualificationType == TournamentQualificationType.OpenTournament ? TournamentQualificationType.ClosedTournament : TournamentQualificationType.OpenTournament;
            tournament.StartTime = tournament.StartTime.AddDays(2);
            tournament.StartTimeIsKnown = !tournament.StartTimeIsKnown;
            tournament.TournamentNotes = tournament.TournamentNotes;
            tournament.TournamentRoute = tournament.TournamentRoute;

            var expectedRoute = "/tournaments/" + Guid.NewGuid();
            RouteGenerator.Setup(x => x.GenerateUniqueRoute(tournament.TournamentRoute,
                                                                 "/tournaments",
                                                                 tournament.TournamentName + " " + tournament.StartTime.Date.ToString("dMMMyyyy", CultureInfo.CurrentCulture),
                                                                 NoiseWords.TournamentRoute,
                                                                 It.IsAny<Func<string, Task<int>>>())).Returns(Task.FromResult(expectedRoute));

            var sanitizedNotes = tournament.TournamentNotes + "xxx";
            HtmlSanitizer.Setup(x => x.Sanitize(tournament.TournamentNotes, string.Empty, null)).Returns(sanitizedNotes);

            var updatedTournament = await Repository.UpdateTournament(tournament, MemberKey, MemberName);

            Assert.NotNull(updatedTournament?.TournamentId);
            Assert.NotEqual(default, updatedTournament!.TournamentId);
            Assert.Equal(tournament.TournamentName, updatedTournament.TournamentName);
            Assert.Equal(tournament.PlayerType.ToString(), updatedTournament.PlayerType.ToString());
            Assert.Equal(tournament.PlayersPerTeam, updatedTournament.PlayersPerTeam);
            Assert.Equal(tournament.QualificationType.ToString(), updatedTournament.QualificationType.ToString());
            Assert.Equal(tournament.StartTime.UtcDateTime, updatedTournament.StartTime.UtcDateTime);
            Assert.Equal(tournament.StartTimeIsKnown, updatedTournament.StartTimeIsKnown);
            Assert.Equal(sanitizedNotes, updatedTournament.TournamentNotes);
            Assert.Equal(expectedRoute, updatedTournament.TournamentRoute);
            if (hasLocation)
            {
                Assert.Equal(tournament.TournamentLocation!.MatchLocationId, updatedTournament.TournamentLocation?.MatchLocationId);
            }
            else
            {
                Assert.Null(updatedTournament.TournamentLocation);
            }

            using (var connection = DatabaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                var result = await connection.QuerySingleOrDefaultAsync<Tournament>(@$"
                        SELECT TournamentName, PlayerType, PlayersPerTeam, QualificationType, StartTime, StartTimeIsKnown, TournamentNotes, TournamentRoute 
                        FROM {Tables.Tournament} WHERE TournamentId = @TournamentId", new { updatedTournament.TournamentId });

                Assert.NotNull(result);
                Assert.Equal(tournament.TournamentName, result.TournamentName);
                Assert.Equal(tournament.PlayerType.ToString(), result.PlayerType.ToString());
                Assert.Equal(tournament.PlayersPerTeam, result.PlayersPerTeam);
                Assert.Equal(tournament.QualificationType.ToString(), result.QualificationType.ToString());
                Assert.Equal(tournament.StartTime.UtcDateTime, result.StartTime.UtcDateTime);
                Assert.Equal(tournament.StartTimeIsKnown, result.StartTimeIsKnown);
                Assert.Equal(sanitizedNotes, result.TournamentNotes);
                Assert.Equal(expectedRoute, result.TournamentRoute);

                var matchLocationId = await connection.ExecuteScalarAsync<Guid?>(@$"SELECT MatchLocationId FROM {Tables.Tournament} WHERE TournamentId = @TournamentId", new { updatedTournament.TournamentId });
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
        public async Task Should_update_existing_default_overset_if_overs_specified()
        {
            var tournament = Copier.CreateAuditableCopy(DatabaseFixture.TestData.TournamentInThePastWithFullDetails!)!;
            tournament.DefaultOverSets = new List<OverSet>
                {
                    new OverSet {
                        OverSetId = tournament.DefaultOverSets[0].OverSetId,
                        OverSetNumber = tournament.DefaultOverSets[0].OverSetNumber,
                        Overs = tournament.DefaultOverSets[0].Overs*2,
                        BallsPerOver = tournament.DefaultOverSets[0].BallsPerOver*2
                    }
            };

            var updatedTournament = await Repository.UpdateTournament(tournament, MemberKey, MemberName);

            Assert.NotNull(updatedTournament?.TournamentId);
            Assert.NotEqual(default, updatedTournament!.TournamentId);
            Assert.Equal(tournament.DefaultOverSets.Count, updatedTournament.DefaultOverSets.Count);
            Assert.Contains(updatedTournament.DefaultOverSets,
                                o => o.OverSetId == tournament.DefaultOverSets[0].OverSetId
                                  && o.OverSetNumber == tournament.DefaultOverSets[0].OverSetNumber
                                  && o.Overs == tournament.DefaultOverSets[0].Overs
                                  && o.BallsPerOver == tournament.DefaultOverSets[0].BallsPerOver);

            using (var connection = DatabaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                var result = await connection.QuerySingleOrDefaultAsync<OverSet>(@$"
                        SELECT OverSetNumber, Overs, BallsPerOver 
                        FROM {Tables.OverSet} 
                        WHERE TournamentId = @TournamentId AND OverSetId = @OverSetId", new { tournament.TournamentId, tournament.DefaultOverSets[0].OverSetId });

                Assert.NotNull(result);
                Assert.Equal(tournament.DefaultOverSets[0].OverSetNumber, result.OverSetNumber);
                Assert.Equal(tournament.DefaultOverSets[0].Overs, result.Overs);
                Assert.Equal(tournament.DefaultOverSets[0].BallsPerOver, result.BallsPerOver);
            }
        }

        [Fact]
        public async Task Update_tournament_in_the_future_should_update_existing_match_overset_if_overs_specified()
        {
            var tournament = Copier.CreateAuditableCopy(DatabaseFixture.TestData.TournamentInThePastWithFullDetails!)!;
            tournament.StartTime = DateTimeOffset.Now.AddDays(1);
            tournament.DefaultOverSets = new List<OverSet>
                {
                    new OverSet {
                        OverSetId = tournament.DefaultOverSets[0].OverSetId,
                        OverSetNumber = tournament.DefaultOverSets[0].OverSetNumber,
                        Overs = tournament.DefaultOverSets[0].Overs*2,
                        BallsPerOver = tournament.DefaultOverSets[0].BallsPerOver*2
                    }
            };

            _ = await Repository.UpdateTournament(tournament, MemberKey, MemberName);

            using (var connection = DatabaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                foreach (var tournamentMatch in tournament.Matches)
                {
                    var match = DatabaseFixture.TestData.Matches.Single(x => x.MatchId == tournamentMatch.MatchId);
                    foreach (var matchInnings in match.MatchInnings)
                    {
                        var result = await connection.QuerySingleOrDefaultAsync<OverSet>(@$"
                            SELECT OverSetNumber, Overs, BallsPerOver 
                            FROM {Tables.OverSet} 
                            WHERE TournamentId IS NULL AND OverSetId = @OverSetId", new { matchInnings.OverSets[0].OverSetId });

                        Assert.NotNull(result);
                        Assert.Equal(tournament.DefaultOverSets[0].OverSetNumber, result.OverSetNumber);
                        Assert.Equal(tournament.DefaultOverSets[0].Overs, result.Overs);
                        Assert.Equal(tournament.DefaultOverSets[0].BallsPerOver, result.BallsPerOver);
                    }
                }
            }
        }

        [Fact]
        public async Task Update_tournament_in_the_past_should_not_update_match_overset_if_overs_specified()
        {
            var tournament = Copier.CreateAuditableCopy(DatabaseFixture.TestData.TournamentInThePastWithFullDetails!)!;
            tournament.DefaultOverSets = new List<OverSet>
                {
                    new OverSet {
                        OverSetId = tournament.DefaultOverSets[0].OverSetId,
                        OverSetNumber = tournament.DefaultOverSets[0].OverSetNumber,
                        Overs = tournament.DefaultOverSets[0].Overs*2,
                        BallsPerOver = tournament.DefaultOverSets[0].BallsPerOver*2
                    }
            };

            _ = await Repository.UpdateTournament(tournament, MemberKey, MemberName);

            using (var connection = DatabaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                foreach (var tournamentMatch in tournament.Matches)
                {
                    var match = DatabaseFixture.TestData.Matches.Single(x => x.MatchId == tournamentMatch.MatchId);
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
        public async Task Should_clear_default_overset_if_overs_not_specified()
        {
            var tournament = Copier.CreateAuditableCopy(DatabaseFixture.TestData.TournamentInThePastWithFullDetails!)!;
            tournament.DefaultOverSets = new List<OverSet>();

            var updatedTournament = await Repository.UpdateTournament(tournament, MemberKey, MemberName);

            Assert.NotNull(updatedTournament?.TournamentId);
            Assert.NotEqual(default, updatedTournament!.TournamentId);
            Assert.Empty(updatedTournament.DefaultOverSets);

            using (var connection = DatabaseFixture.ConnectionFactory.CreateDatabaseConnection())
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
            var tournament = Copier.CreateAuditableCopy(DatabaseFixture.TestData.TournamentInThePastWithFullDetails!)!;
            tournament.StartTime = DateTimeOffset.UtcNow.AddDays(1);
            tournament.DefaultOverSets = new List<OverSet>();

            _ = await Repository.UpdateTournament(tournament, MemberKey, MemberName);

            using (var connection = DatabaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                foreach (var tournamentMatch in tournament.Matches)
                {
                    var match = DatabaseFixture.TestData.Matches.Single(x => x.MatchId == tournamentMatch.MatchId);
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
        public async Task Updates_matches_from_tournament_details()
        {
            var tournament = Copier.CreateAuditableCopy(DatabaseFixture.TestData.TournamentInThePastWithFullDetails!)!;
            // Change these fields and check they are copied to matches
            tournament.StartTime = tournament.StartTime.AddDays(1);
            tournament.PlayersPerTeam = tournament.PlayersPerTeam + 2;
            tournament.PlayerType = tournament.PlayerType == PlayerType.Mixed ? PlayerType.Ladies : PlayerType.Mixed;
            tournament.TournamentLocation = new MatchLocation { MatchLocationId = DatabaseFixture.TestData.MatchLocations.First(x => x.MatchLocationId != tournament.TournamentLocation?.MatchLocationId).MatchLocationId };

            _ = await Repository.UpdateTournament(tournament, MemberKey, MemberName);

            using (var connection = DatabaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                foreach (var tournamentMatch in tournament.Matches)
                {
                    var updatedMatch = await connection.QuerySingleOrDefaultAsync<Stoolball.Matches.Match>($"SELECT PlayerType, PlayersPerTeam, OrderInTournament, StartTime FROM {Tables.Match} WHERE MatchId = @MatchId", tournamentMatch);
                    var updatedMatchLocation = await connection.ExecuteScalarAsync<Guid?>($"SELECT MatchLocationId FROM {Tables.Match} WHERE MatchId = @MatchId", tournamentMatch);

                    Assert.NotNull(updatedMatch);
                    Assert.Equal(tournament.PlayerType, updatedMatch.PlayerType);
                    Assert.Equal(tournament.PlayersPerTeam, updatedMatch.PlayersPerTeam);
                    Assert.Equal(tournament.StartTime.AccurateToTheMinute().AddMinutes(((updatedMatch.OrderInTournament ?? 1) - 1) * 45), updatedMatch.StartTime.AccurateToTheMinute());
                    Assert.Equal(tournament.TournamentLocation.MatchLocationId, updatedMatchLocation);
                }
            }
        }
    }
}
