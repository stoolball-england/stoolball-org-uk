using Stoolball.Testing;

namespace Stoolball.Data.SqlServer.IntegrationTests.Matches.Tournaments
{
    [Collection(IntegrationTestConstants.TestDataIntegrationTestCollection)]
    public class UpdateMatchesTests : TournamentRepositoryTestsBase
    {
        public UpdateMatchesTests(SqlServerTestDataFixture databaseFixture) : base(databaseFixture) { }

        [Fact]
        public async Task Throws_ArgumentNullException_if_tournament_is_null()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(
                      async () => await Repository.UpdateMatches(
                          null!,
                          MemberKey,
                          MemberUsername,
                          MemberName));
        }

        [Fact]
        public async Task Throws_ArgumentNullException_if_memberKey_is_default_Guid()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(
                async () => await Repository.UpdateMatches(
                    DatabaseFixture.TestData.Tournaments.First(),
                    default,
                    MemberUsername,
                    MemberName));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task Throws_ArgumentNullException_if_memberUsername_is_null_or_whitespace(string? memberUsername)
        {
            await Assert.ThrowsAsync<ArgumentNullException>(
                  async () => await Repository.UpdateMatches(
                      DatabaseFixture.TestData.Tournaments.First(),
                      MemberKey,
                      memberUsername!,
                      MemberName));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task Throws_ArgumentNullException_if_memberName_is_null_or_whitespace(string? memberName)
        {
            await Assert.ThrowsAsync<ArgumentNullException>(
                async () => await Repository.UpdateMatches(
                    DatabaseFixture.TestData.Tournaments.First(),
                    MemberKey,
                    MemberUsername,
                    memberName!));
        }

        [Fact]
        public async Task Deletes_matches_not_in_passed_collection()
        {
            var tournament = Copier.CreateAuditableCopy(DatabaseFixture.TestData.Tournaments.First(t => t.Matches.Count > 1))!;

            var matchToDelete = tournament.Matches[^1];
            tournament.Matches.Remove(matchToDelete);

            var result = await Repository.UpdateMatches(tournament, MemberKey, MemberUsername, MemberName).ConfigureAwait(false);

            Assert.NotNull(result);
            Assert.Equal(tournament.Matches.Count, result.Matches.Count);
            foreach (var expectedMatch in tournament.Matches.Select(m => m.MatchId))
            {
                var resultMatch = result.Matches.SingleOrDefault(m => m.MatchId == expectedMatch);
                Assert.NotNull(resultMatch);
            }

            using (var connection = DatabaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                var retainedMatchCount = await connection.QuerySingleAsync<int>($"SELECT COUNT(*) FROM {Tables.Match} WHERE MatchId IN @MatchIds", new { MatchIds = tournament.Matches.Select(m => m.MatchId) }).ConfigureAwait(false);
                Assert.Equal(tournament.Matches.Count, retainedMatchCount);

                var deletedMatchCount = await connection.QuerySingleAsync<int>($"SELECT COUNT(*) FROM {Tables.Match} WHERE MatchId = @MatchId", new { matchToDelete.MatchId }).ConfigureAwait(false);
                Assert.Equal(0, deletedMatchCount);
            }
        }

        [Fact]
        public async Task Matches_with_a_MatchId_have_order_in_tournament_updated_to_match_passed_collection()
        {
            var tournament = Copier.CreateAuditableCopy(DatabaseFixture.TestData.Tournaments.First(t => t.Matches.Count > 3))!;

            tournament.Matches.Reverse();
            var result = await Repository.UpdateMatches(tournament, MemberKey, MemberUsername, MemberName).ConfigureAwait(false);

            Assert.NotNull(result);
            Assert.Equal(tournament.Matches.Count, result.Matches.Count);
            for (var i = 0; i < tournament.Matches.Count; i++)
            {
                Assert.Equal(tournament.Matches[i].MatchId, result.Matches[i].MatchId);
            }

            using (var connection = DatabaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                for (var i = 0; i < tournament.Matches.Count; i++)
                {
                    var orderInTournament = await connection.QuerySingleAsync<int>($"SELECT OrderInTournament FROM {Tables.Match} WHERE MatchId = @MatchId", new { tournament.Matches[i].MatchId }).ConfigureAwait(false);
                    Assert.Equal(i + 1, orderInTournament);
                }
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Matches_without_a_MatchId_are_created_and_added_to_tournament(bool matchHasTeams)
        {
            var tournament = Copier.CreateAuditableCopy(DatabaseFixture.TestData.Tournaments.First(t => t.Teams.Count >= 2))!;
            var currentMatchIds = tournament.Matches.Select(m => m.MatchId).ToList();

            var matchToAdd = new MatchInTournament
            {
                MatchName = "This should be ignored",
                Teams = matchHasTeams ? tournament.Teams.Take(2).ToList() : []
            };

            tournament.Matches.Add(matchToAdd);

            RouteGenerator.Setup(x => x.GenerateRoute("/matches", It.IsAny<string>(), NoiseWords.MatchRoute)).Returns(tournament.TournamentRoute + $"/matches/{Guid.NewGuid()}");

            var result = await Repository.UpdateMatches(tournament, MemberKey, MemberUsername, MemberName).ConfigureAwait(false);

            Assert.NotNull(result);
            Assert.Equal(tournament.Matches.Count, result.Matches.Count);

            var addedMatch = result.Matches.SingleOrDefault(m => !currentMatchIds.Contains(m.MatchId));
            Assert.NotNull(addedMatch?.MatchId);
            Assert.NotEqual(matchToAdd.MatchName, addedMatch!.MatchName);

            using (var connection = DatabaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                var savedMatch = await connection.QuerySingleAsync<(Guid? TournamentId, string? MatchType, string? PlayerType, int? PlayersPerTeam, Guid? MatchLocationId,
                                                                    int OrderInTournament, DateTimeOffset StartTime, bool StartTimeIsKnown)>(
                    $"SELECT TournamentId, MatchType, PlayerType, PlayersPerTeam, MatchLocationId, OrderInTournament, StartTime, StartTimeIsKnown FROM {Tables.Match} WHERE MatchId = @MatchId",
                    new { addedMatch.MatchId }).ConfigureAwait(false);

                Assert.Equal(tournament.TournamentId, savedMatch.TournamentId);
                Assert.Equal(MatchType.GroupMatch.ToString(), savedMatch.MatchType);
                Assert.Equal(tournament.PlayerType.ToString(), savedMatch.PlayerType);
                Assert.Equal(tournament.PlayersPerTeam, savedMatch.PlayersPerTeam);
                Assert.Equal(tournament.TournamentLocation?.MatchLocationId, savedMatch.MatchLocationId);
                Assert.Equal(currentMatchIds.Count + 1, savedMatch.OrderInTournament);
                Assert.Equal(tournament.StartTime.AddMinutes(45 * (savedMatch.OrderInTournament - 1)).AccurateToTheMinute(), savedMatch.StartTime.AccurateToTheMinute());
                Assert.False(savedMatch.StartTimeIsKnown);

                var savedTeamsInMatch = await connection.QueryAsync<(Guid TeamId, string PlayingAsTeamName, string TeamRole)>($"SELECT TeamId, PlayingAsTeamName, TeamRole FROM {Tables.MatchTeam} WHERE MatchId = @MatchId",
                    new { addedMatch.MatchId }).ConfigureAwait(false);

                if (matchHasTeams)
                {
                    Assert.Equal(2, savedTeamsInMatch.Count());

                    var firstSavedTeam = savedTeamsInMatch.SingleOrDefault(t => t.TeamId == matchToAdd.Teams[0].Team!.TeamId);
                    Assert.Equal(matchToAdd.Teams[0].Team!.TeamName, firstSavedTeam.PlayingAsTeamName);
                    Assert.Equal(TeamRole.Home.ToString(), firstSavedTeam.TeamRole);

                    var secondSavedTeam = savedTeamsInMatch.SingleOrDefault(t => t.TeamId == matchToAdd.Teams[1].Team!.TeamId);
                    Assert.Equal(matchToAdd.Teams[1].Team!.TeamName, secondSavedTeam.PlayingAsTeamName);
                    Assert.Equal(TeamRole.Away.ToString(), secondSavedTeam.TeamRole);
                }
                else
                {
                    Assert.Empty(savedTeamsInMatch);
                }
            }
        }

        [Fact]
        public async Task Audits_and_logs()
        {
            var tournament = Copier.CreateAuditableCopy(DatabaseFixture.TestData.Tournaments.First())!;

            _ = await Repository.UpdateMatches(tournament, MemberKey, MemberUsername, MemberName).ConfigureAwait(false);

            AuditRepository.Verify(x => x.CreateAudit(It.Is<AuditRecord>(a => a.Action == AuditAction.Update), It.IsAny<IDbTransaction>()), Times.Once);
            Logger.Verify(x => x.Info(LoggingTemplates.Updated, It.Is<Tournament>(t => t.TournamentId == tournament.TournamentId), MemberName, MemberKey, typeof(SqlServerTournamentRepository), nameof(SqlServerTournamentRepository.UpdateMatches)));
        }
    }
}
