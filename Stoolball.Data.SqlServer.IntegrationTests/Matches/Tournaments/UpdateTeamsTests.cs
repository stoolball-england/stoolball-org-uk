using System.Data.SqlTypes;
using System.Globalization;

namespace Stoolball.Data.SqlServer.IntegrationTests.Matches.Tournaments
{
    [Collection(IntegrationTestConstants.TestDataIntegrationTestCollection)]
    public class UpdateTeamsTests : TournamentRepositoryTestsBase
    {
        public UpdateTeamsTests(SqlServerTestDataFixture databaseFixture) : base(databaseFixture) { }

        [Fact]
        public async Task Throws_ArgumentNullException_if_tournament_is_null()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(
                async () => await Repository.UpdateTeams(
                    null!,
                    MemberKey,
                    MemberUsername,
                    MemberName));
        }

        [Fact]
        public async Task Throws_ArgumentNullException_if_memberKey_is_default_Guid()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(
                async () => await Repository.UpdateTeams(
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
                async () => await Repository.UpdateTeams(
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
                async () => await Repository.UpdateTeams(
                    DatabaseFixture.TestData.Tournaments.First(),
                    MemberKey,
                    MemberUsername,
                    memberName!));
        }

        [Fact]
        public async Task Throws_ArgumentException_if_team_is_null()
        {
            await Assert.ThrowsAsync<ArgumentException>(
                async () => await Repository.UpdateTeams(
                    new Tournament { Teams = [new TeamInTournament { TeamRole = TournamentTeamRole.Confirmed }] },
                    MemberKey,
                    MemberUsername,
                    MemberName));
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        public async Task Throws_ArgumentException_if_MaximumTeamsInTournament_is_less_than_3(int maximum)
        {
            var tournament = Copier.CreateAuditableCopy(DatabaseFixture.TestData.Tournaments.First())!;
            tournament.MaximumTeamsInTournament = maximum;

            await Assert.ThrowsAsync<ArgumentException>(
                async () => await Repository.UpdateTeams(
                    tournament,
                    MemberKey,
                    MemberUsername,
                    MemberName));
        }

        [Theory]
        [InlineData(null, true)]
        [InlineData(3, true)]
        [InlineData(10, true)]
        [InlineData(3, false)]
        public async Task Sets_maximum_teams_and_spaces_in_tournament(int? maximumTeams, bool tournamentHasAllowedNumberOfTeams)
        {

            Func<Tournament, bool> tournamentFilter = t => t.Teams.Count > 0 && (maximumTeams is null || t.Teams.Count <= maximumTeams);
            if (maximumTeams.HasValue && !tournamentHasAllowedNumberOfTeams)
            {
                tournamentFilter = t => t.Teams.Count > maximumTeams;
            }

            var tournament = Copier.CreateAuditableCopy(DatabaseFixture.TestData.Tournaments.First(tournamentFilter))!;
            tournament.MaximumTeamsInTournament = maximumTeams;

            var result = await Repository.UpdateTeams(tournament, MemberKey, MemberUsername, MemberName).ConfigureAwait(false);

            Assert.NotNull(result);
            if (maximumTeams.HasValue)
            {
                Assert.Equal(maximumTeams.Value, result.MaximumTeamsInTournament);
                if (tournamentHasAllowedNumberOfTeams)
                {
                    Assert.Equal(maximumTeams.Value - tournament.Teams.Count, result.SpacesInTournament);
                }
                else
                {
                    Assert.Equal(0, result.SpacesInTournament);
                }
            }
            else
            {
                Assert.Null(result.MaximumTeamsInTournament);
                Assert.Null(result.SpacesInTournament);
            }

            using (var connection = DatabaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                var saved = await connection.QuerySingleOrDefaultAsync<(int? MaximumTeamsInTournament, int? SpacesInTournament)>(
                    $"SELECT MaximumTeamsInTournament, SpacesInTournament FROM {Tables.Tournament} WHERE TournamentId = @TournamentId",
                    new { tournament.TournamentId }).ConfigureAwait(false);

                if (maximumTeams.HasValue)
                {
                    Assert.Equal(maximumTeams.Value, saved.MaximumTeamsInTournament);
                    if (tournamentHasAllowedNumberOfTeams)
                    {
                        Assert.Equal(maximumTeams.Value - tournament.Teams.Count, saved.SpacesInTournament);
                    }
                    else
                    {
                        Assert.Equal(0, saved.SpacesInTournament);
                    }
                }
                else
                {
                    Assert.Null(saved.MaximumTeamsInTournament);
                    Assert.Null(saved.SpacesInTournament);
                }
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Team_added_to_tournament_that_is_not_an_existing_team_is_added_as_transient_team(bool tournamentHasLocation)
        {
            var tournament = Copier.CreateAuditableCopy(DatabaseFixture.TestData.Tournaments.First(t => t.TournamentLocation is not null == tournamentHasLocation))!;
            var teamToAdd = new Team { TeamName = $"New team {Guid.NewGuid()}", TeamRoute = "/should-be-discarded" };
            tournament.Teams.Add(new TeamInTournament { Team = teamToAdd });

            var expectedRoute = $"{tournament.TournamentRoute}-{Guid.NewGuid()}";
            RouteGenerator.Setup(x => x.GenerateUniqueRoute(
               $"{tournament.TournamentRoute}/teams", teamToAdd.TeamName, NoiseWords.TeamRoute, It.IsAny<Func<string, Task<int>>>())).Returns(Task.FromResult(expectedRoute));

            await AssertAddedTeam(tournament, teamToAdd, TeamType.Transient, t =>
            {
                Assert.Equal(expectedRoute, t.TeamRoute);
                Assert.Equal(tournament.PlayerType, t.PlayerType);
                Assert.Equal(tournament.StartTime.Year, t.UntilYear);
                if (tournamentHasLocation)
                {
                    Assert.Single(t.MatchLocations);
                    Assert.Equal(tournament.TournamentLocation!.MatchLocationId, t.MatchLocations[0].MatchLocationId);
                }
                else
                {
                    Assert.Empty(t.MatchLocations);
                }
            });

            AuditRepository.Verify(x => x.CreateAudit(It.Is<AuditRecord>(a => a.Action == AuditAction.Create), It.IsAny<IDbTransaction>()), Times.Once);
            Logger.Verify(x => x.Info(LoggingTemplates.Created, It.Is<TeamInTournament>(tt => tt.Team!.TeamName == teamToAdd.TeamName), MemberName, MemberKey, typeof(SqlServerTournamentRepository), nameof(SqlServerTournamentRepository.UpdateTeams)));
        }

        [Fact]
        public async Task Existing_team_added_to_tournament_is_not_a_transient_team()
        {
            var tournament = Copier.CreateAuditableCopy(DatabaseFixture.TestData.Tournaments.First())!;
            var teamToAdd = DatabaseFixture.TestData.Teams.First(t => t.TeamType == TeamType.Regular);
            tournament.Teams.Add(new TeamInTournament { Team = teamToAdd });

            await AssertAddedTeam(tournament, teamToAdd, TeamType.Regular, t => { }).ConfigureAwait(false);
        }

        private async Task AssertAddedTeam(Tournament tournament, Team teamToAdd, TeamType expectedTeamType, Action<Team> assertTeam)
        {
            var result = await Repository.UpdateTeams(tournament, MemberKey, MemberUsername, MemberName).ConfigureAwait(false);

            Assert.NotNull(result?.Teams);
            Assert.Equal(tournament.Teams.Count, result!.Teams.Count);

            var addedTeam = result.Teams.SingleOrDefault(t => t.Team?.TeamId == teamToAdd.TeamId || t.Team?.TeamName == teamToAdd.TeamName);
            Assert.NotNull(addedTeam?.TournamentTeamId);
            Assert.NotNull(addedTeam!.Team?.TeamId);
            Assert.Equal(TournamentTeamRole.Confirmed, addedTeam.TeamRole);
            Assert.Equal(expectedTeamType, addedTeam.Team!.TeamType);

            assertTeam(addedTeam.Team);

            using (var connection = DatabaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                var savedTeam = await connection.QuerySingleOrDefaultAsync<
                    (Guid TeamId, string TeamRole, string TeamType, string TeamRoute, PlayerType PlayerType, int? UntilYear)>(
                    @$"SELECT tt.TeamId, tt.TeamRole, t.TeamType, t.TeamRoute, t.PlayerType, YEAR(tv.UntilDate) AS UntilYear 
                       FROM {Tables.TournamentTeam} tt INNER JOIN {Tables.Team} t ON tt.TeamId = t.TeamId 
                       INNER JOIN {Tables.TeamVersion} AS tv ON t.TeamId = tv.TeamId
                       WHERE TournamentTeamId = @TournamentTeamId
                       AND tv.TeamVersionId = (SELECT TOP 1 TeamVersionId FROM {Tables.TeamVersion} WHERE TeamId = t.TeamId ORDER BY ISNULL(UntilDate, '{SqlDateTime.MaxValue.Value.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}') DESC)",
                    new { addedTeam.TournamentTeamId }).ConfigureAwait(false);

                var savedLocations = await connection.QueryAsync<Guid>($"SELECT MatchLocationId FROM {Tables.TeamMatchLocation} WHERE TeamId = @TeamId", new { savedTeam.TeamId }).ConfigureAwait(false);

                Assert.Equal(addedTeam.Team.TeamId, savedTeam.TeamId);
                Assert.Equal(TournamentTeamRole.Confirmed.ToString(), savedTeam.TeamRole);
                Assert.Equal(expectedTeamType.ToString(), savedTeam.TeamType);

                assertTeam(new Team
                {
                    TeamRoute = savedTeam.TeamRoute,
                    PlayerType = savedTeam.PlayerType,
                    UntilYear = savedTeam.UntilYear,
                    MatchLocations = savedLocations.Select(ml => new MatchLocation { MatchLocationId = ml }).ToList()
                });
            }
        }

        [Fact]
        public async Task Transient_team_removed_deletes_tournament_data_and_team()
        {
            var tournament = Copier.CreateAuditableCopy(FindTournamentWithMatchDataForTeam(TeamType.Transient));

            throw new NotImplementedException();

            // VERIFY
            //await connection.ExecuteAsync($"UPDATE {Tables.PlayerInMatchStatistics} SET OppositionTeamId = NULL, OppositionTeamName = NULL WHERE OppositionTeamId = @TeamId AND TournamentId = @TournamentId", new { team.TeamId, auditableTournament.TournamentId }, transaction).ConfigureAwait(false);
            //await connection.ExecuteAsync($"DELETE FROM {Tables.PlayerInMatchStatistics} WHERE TeamId = @TeamId AND TournamentId = @TournamentId", new { team.TeamId, auditableTournament.TournamentId }, transaction).ConfigureAwait(false);
            //await connection.ExecuteAsync($"UPDATE {Tables.PlayerInMatchStatistics} SET BowledByPlayerIdentityId = NULL WHERE BowledByPlayerIdentityId IN (SELECT PlayerIdentityId FROM {Tables.PlayerIdentity} WHERE TeamId = @TeamId) AND TournamentId = @TournamentId", new { team.TeamId, auditableTournament.TournamentId }, transaction).ConfigureAwait(false);
            //await connection.ExecuteAsync($"UPDATE {Tables.PlayerInMatchStatistics} SET CaughtByPlayerIdentityId = NULL WHERE CaughtByPlayerIdentityId IN (SELECT PlayerIdentityId FROM {Tables.PlayerIdentity} WHERE TeamId = @TeamId) AND TournamentId = @TournamentId", new { team.TeamId, auditableTournament.TournamentId }, transaction).ConfigureAwait(false);
            //await connection.ExecuteAsync($"UPDATE {Tables.PlayerInMatchStatistics} SET RunOutByPlayerIdentityId = NULL WHERE RunOutByPlayerIdentityId IN (SELECT PlayerIdentityId FROM {Tables.PlayerIdentity} WHERE TeamId = @TeamId) AND TournamentId = @TournamentId", new { team.TeamId, auditableTournament.TournamentId }, transaction).ConfigureAwait(false);
            //await connection.ExecuteAsync($"UPDATE {Tables.PlayerInnings} SET DismissedByPlayerIdentityId = NULL WHERE DismissedByPlayerIdentityId IN (SELECT PlayerIdentityId FROM {Tables.PlayerIdentity} WHERE TeamId = @TeamId) AND MatchInningsId IN (SELECT MatchInningsId FROM {Tables.MatchInnings} WHERE MatchId IN (SELECT MatchId FROM {Tables.Match} WHERE TournamentId = @TournamentId))", new { team.TeamId, auditableTournament.TournamentId }, transaction).ConfigureAwait(false);
            //await connection.ExecuteAsync($"UPDATE {Tables.PlayerInnings} SET BowlerPlayerIdentityId = NULL WHERE BowlerPlayerIdentityId IN (SELECT PlayerIdentityId FROM {Tables.PlayerIdentity} WHERE TeamId = @TeamId) AND MatchInningsId IN (SELECT MatchInningsId FROM {Tables.MatchInnings} WHERE MatchId IN (SELECT MatchId FROM {Tables.Match} WHERE TournamentId = @TournamentId))", new { team.TeamId, auditableTournament.TournamentId }, transaction).ConfigureAwait(false);
            //await connection.ExecuteAsync($"DELETE FROM {Tables.PlayerInnings} WHERE BatterPlayerIdentityId IN (SELECT PlayerIdentityId FROM {Tables.PlayerIdentity} WHERE TeamId = @TeamId) AND MatchInningsId IN (SELECT MatchInningsId FROM {Tables.MatchInnings} WHERE MatchId IN (SELECT MatchId FROM {Tables.Match} WHERE TournamentId = @TournamentId))", new { team.TeamId, auditableTournament.TournamentId }, transaction).ConfigureAwait(false);
            //await connection.ExecuteAsync($"DELETE FROM {Tables.Over} WHERE BowlerPlayerIdentityId IN (SELECT PlayerIdentityId FROM {Tables.PlayerIdentity} WHERE TeamId = @TeamId) AND MatchInningsId IN (SELECT MatchInningsId FROM {Tables.MatchInnings} WHERE MatchId IN (SELECT MatchId FROM {Tables.Match} WHERE TournamentId = @TournamentId))", new { team.TeamId, auditableTournament.TournamentId }, transaction).ConfigureAwait(false);
            //await connection.ExecuteAsync($"DELETE FROM {Tables.BowlingFigures} WHERE BowlerPlayerIdentityId IN (SELECT PlayerIdentityId FROM {Tables.PlayerIdentity} WHERE TeamId = @TeamId) AND MatchInningsId IN (SELECT MatchInningsId FROM {Tables.MatchInnings} WHERE MatchId IN (SELECT MatchId FROM {Tables.Match} WHERE TournamentId = @TournamentId))", new { team.TeamId, auditableTournament.TournamentId }, transaction).ConfigureAwait(false);
            //await connection.ExecuteAsync($"DELETE FROM {Tables.AwardedTo} WHERE PlayerIdentityId IN (SELECT PlayerIdentityId FROM {Tables.PlayerIdentity} WHERE TeamId = @TeamId) AND (TournamentId = @TournamentId OR MatchId IN (SELECT MatchId FROM {Tables.Match} WHERE TournamentId = @TournamentId))", new { team.TeamId, auditableTournament.TournamentId }, transaction).ConfigureAwait(false);
            //await connection.ExecuteAsync($"UPDATE {Tables.MatchInnings} SET BattingMatchTeamId = NULL WHERE BattingMatchTeamId IN (SELECT MatchTeamId FROM {Tables.MatchTeam} WHERE TeamId = @TeamId) AND MatchId IN (SELECT MatchId FROM {Tables.Match} WHERE TournamentId = @TournamentId)", new { team.TeamId, auditableTournament.TournamentId }, transaction).ConfigureAwait(false);
            //await connection.ExecuteAsync($"UPDATE {Tables.MatchInnings} SET BowlingMatchTeamId = NULL WHERE BowlingMatchTeamId IN (SELECT MatchTeamId FROM {Tables.MatchTeam} WHERE TeamId = @TeamId) AND MatchId IN (SELECT MatchId FROM {Tables.Match} WHERE TournamentId = @TournamentId)", new { team.TeamId, auditableTournament.TournamentId }, transaction).ConfigureAwait(false);
            //await connection.ExecuteAsync($"DELETE FROM {Tables.MatchTeam} WHERE TeamId = @TeamId AND MatchId IN (SELECT MatchId FROM {Tables.Match} WHERE TournamentId = @TournamentId)", new { team.TeamId, auditableTournament.TournamentId }, transaction).ConfigureAwait(false);
            //await connection.ExecuteAsync($"DELETE FROM {Tables.TournamentTeam} WHERE TournamentTeamId = @TournamentTeamId", new { team.TournamentTeamId }, transaction).ConfigureAwait(false);

            // AND VERIFY
            //var playerIds = await connection.QueryAsync<Guid>($"SELECT PlayerId FROM {Tables.PlayerIdentity} WHERE TeamId = @TeamId", new { team.TeamId }, transaction).ConfigureAwait(false);
            //await connection.ExecuteAsync($"DELETE FROM {Tables.PlayerIdentity} WHERE TeamId = @TeamId", new { team.TeamId }, transaction).ConfigureAwait(false);
            //await connection.ExecuteAsync($"DELETE FROM {Tables.Player} WHERE PlayerId IN @playerIds", new { playerIds }, transaction).ConfigureAwait(false);
            //await connection.ExecuteAsync($"DELETE FROM {Tables.TeamMatchLocation} WHERE TeamId = @TeamId", new { team.TeamId }, transaction).ConfigureAwait(false);
            //await connection.ExecuteAsync($"DELETE FROM {Tables.TeamVersion} WHERE TeamId = @TeamId", new { team.TeamId }, transaction).ConfigureAwait(false);
            //await connection.ExecuteAsync($"DELETE FROM {Tables.Team} WHERE TeamId = @TeamId", new { team.TeamId }, transaction).ConfigureAwait(false);
        }

        private Tournament FindTournamentWithMatchDataForTeam(TeamType teamType)
        {
            return DatabaseFixture.TestData.Tournaments.First(
                                t => t.StartTime < DateTime.UtcNow
                                  && t.Matches.Any(tm => DatabaseFixture.TestData.Matches.SingleOrDefault(m => m.MatchId == tm.MatchId
                                                                                                            && m.Teams.Any()
                                                                                                            && m.Awards.Any()
                                                                                                            && m.MatchInnings.Any(mi => mi.PlayerInnings.Any()
                                                                                                                                     && mi.OversBowled.Any()
                                                                                                                                     && mi.BowlingFigures.Any()
                                                                                                            )
                                                                                                            ) is not null)
                                  && t.Teams.Any(t => t.Team?.TeamType == teamType));
        }

        [Fact]
        public async Task Existing_team_removed_deletes_tournament_data_but_not_team()
        {
            var tournament = Copier.CreateAuditableCopy(FindTournamentWithMatchDataForTeam(TeamType.Regular));

            throw new NotImplementedException();

            // VERIFY
            //await connection.ExecuteAsync($"UPDATE {Tables.PlayerInMatchStatistics} SET OppositionTeamId = NULL, OppositionTeamName = NULL WHERE OppositionTeamId = @TeamId AND TournamentId = @TournamentId", new { team.TeamId, auditableTournament.TournamentId }, transaction).ConfigureAwait(false);
            //await connection.ExecuteAsync($"DELETE FROM {Tables.PlayerInMatchStatistics} WHERE TeamId = @TeamId AND TournamentId = @TournamentId", new { team.TeamId, auditableTournament.TournamentId }, transaction).ConfigureAwait(false);
            //await connection.ExecuteAsync($"UPDATE {Tables.PlayerInMatchStatistics} SET BowledByPlayerIdentityId = NULL WHERE BowledByPlayerIdentityId IN (SELECT PlayerIdentityId FROM {Tables.PlayerIdentity} WHERE TeamId = @TeamId) AND TournamentId = @TournamentId", new { team.TeamId, auditableTournament.TournamentId }, transaction).ConfigureAwait(false);
            //await connection.ExecuteAsync($"UPDATE {Tables.PlayerInMatchStatistics} SET CaughtByPlayerIdentityId = NULL WHERE CaughtByPlayerIdentityId IN (SELECT PlayerIdentityId FROM {Tables.PlayerIdentity} WHERE TeamId = @TeamId) AND TournamentId = @TournamentId", new { team.TeamId, auditableTournament.TournamentId }, transaction).ConfigureAwait(false);
            //await connection.ExecuteAsync($"UPDATE {Tables.PlayerInMatchStatistics} SET RunOutByPlayerIdentityId = NULL WHERE RunOutByPlayerIdentityId IN (SELECT PlayerIdentityId FROM {Tables.PlayerIdentity} WHERE TeamId = @TeamId) AND TournamentId = @TournamentId", new { team.TeamId, auditableTournament.TournamentId }, transaction).ConfigureAwait(false);
            //await connection.ExecuteAsync($"UPDATE {Tables.PlayerInnings} SET DismissedByPlayerIdentityId = NULL WHERE DismissedByPlayerIdentityId IN (SELECT PlayerIdentityId FROM {Tables.PlayerIdentity} WHERE TeamId = @TeamId) AND MatchInningsId IN (SELECT MatchInningsId FROM {Tables.MatchInnings} WHERE MatchId IN (SELECT MatchId FROM {Tables.Match} WHERE TournamentId = @TournamentId))", new { team.TeamId, auditableTournament.TournamentId }, transaction).ConfigureAwait(false);
            //await connection.ExecuteAsync($"UPDATE {Tables.PlayerInnings} SET BowlerPlayerIdentityId = NULL WHERE BowlerPlayerIdentityId IN (SELECT PlayerIdentityId FROM {Tables.PlayerIdentity} WHERE TeamId = @TeamId) AND MatchInningsId IN (SELECT MatchInningsId FROM {Tables.MatchInnings} WHERE MatchId IN (SELECT MatchId FROM {Tables.Match} WHERE TournamentId = @TournamentId))", new { team.TeamId, auditableTournament.TournamentId }, transaction).ConfigureAwait(false);
            //await connection.ExecuteAsync($"DELETE FROM {Tables.PlayerInnings} WHERE BatterPlayerIdentityId IN (SELECT PlayerIdentityId FROM {Tables.PlayerIdentity} WHERE TeamId = @TeamId) AND MatchInningsId IN (SELECT MatchInningsId FROM {Tables.MatchInnings} WHERE MatchId IN (SELECT MatchId FROM {Tables.Match} WHERE TournamentId = @TournamentId))", new { team.TeamId, auditableTournament.TournamentId }, transaction).ConfigureAwait(false);
            //await connection.ExecuteAsync($"DELETE FROM {Tables.Over} WHERE BowlerPlayerIdentityId IN (SELECT PlayerIdentityId FROM {Tables.PlayerIdentity} WHERE TeamId = @TeamId) AND MatchInningsId IN (SELECT MatchInningsId FROM {Tables.MatchInnings} WHERE MatchId IN (SELECT MatchId FROM {Tables.Match} WHERE TournamentId = @TournamentId))", new { team.TeamId, auditableTournament.TournamentId }, transaction).ConfigureAwait(false);
            //await connection.ExecuteAsync($"DELETE FROM {Tables.BowlingFigures} WHERE BowlerPlayerIdentityId IN (SELECT PlayerIdentityId FROM {Tables.PlayerIdentity} WHERE TeamId = @TeamId) AND MatchInningsId IN (SELECT MatchInningsId FROM {Tables.MatchInnings} WHERE MatchId IN (SELECT MatchId FROM {Tables.Match} WHERE TournamentId = @TournamentId))", new { team.TeamId, auditableTournament.TournamentId }, transaction).ConfigureAwait(false);
            //await connection.ExecuteAsync($"DELETE FROM {Tables.AwardedTo} WHERE PlayerIdentityId IN (SELECT PlayerIdentityId FROM {Tables.PlayerIdentity} WHERE TeamId = @TeamId) AND (TournamentId = @TournamentId OR MatchId IN (SELECT MatchId FROM {Tables.Match} WHERE TournamentId = @TournamentId))", new { team.TeamId, auditableTournament.TournamentId }, transaction).ConfigureAwait(false);
            //await connection.ExecuteAsync($"UPDATE {Tables.MatchInnings} SET BattingMatchTeamId = NULL WHERE BattingMatchTeamId IN (SELECT MatchTeamId FROM {Tables.MatchTeam} WHERE TeamId = @TeamId) AND MatchId IN (SELECT MatchId FROM {Tables.Match} WHERE TournamentId = @TournamentId)", new { team.TeamId, auditableTournament.TournamentId }, transaction).ConfigureAwait(false);
            //await connection.ExecuteAsync($"UPDATE {Tables.MatchInnings} SET BowlingMatchTeamId = NULL WHERE BowlingMatchTeamId IN (SELECT MatchTeamId FROM {Tables.MatchTeam} WHERE TeamId = @TeamId) AND MatchId IN (SELECT MatchId FROM {Tables.Match} WHERE TournamentId = @TournamentId)", new { team.TeamId, auditableTournament.TournamentId }, transaction).ConfigureAwait(false);
            //await connection.ExecuteAsync($"DELETE FROM {Tables.MatchTeam} WHERE TeamId = @TeamId AND MatchId IN (SELECT MatchId FROM {Tables.Match} WHERE TournamentId = @TournamentId)", new { team.TeamId, auditableTournament.TournamentId }, transaction).ConfigureAwait(false);
            //await connection.ExecuteAsync($"DELETE FROM {Tables.TournamentTeam} WHERE TournamentTeamId = @TournamentTeamId", new { team.TournamentTeamId }, transaction).ConfigureAwait(false);
        }

        [Fact]
        public async Task Audits_and_logs()
        {
            var tournament = Copier.CreateAuditableCopy(DatabaseFixture.TestData.Tournaments.First())!;

            var result = await Repository.UpdateTeams(tournament, MemberKey, MemberUsername, MemberName).ConfigureAwait(false);

            AuditRepository.Verify(x => x.CreateAudit(It.Is<AuditRecord>(a => a.Action == AuditAction.Update), It.IsAny<IDbTransaction>()), Times.Once);
            Logger.Verify(x => x.Info(LoggingTemplates.Updated, It.Is<Tournament>(t => t.TournamentId == tournament.TournamentId), MemberName, MemberKey, typeof(SqlServerTournamentRepository), nameof(SqlServerTournamentRepository.UpdateTeams)));

        }
    }
}
