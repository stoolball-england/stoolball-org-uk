namespace Stoolball.Data.SqlServer.IntegrationTests.Matches.Tournaments
{
    [Collection(IntegrationTestConstants.TestDataIntegrationTestCollection)]
    public class DeleteTournamentTests : TournamentRepositoryTestsBase
    {
        public DeleteTournamentTests(SqlServerTestDataFixture databaseFixture) : base(databaseFixture) { }

        [Fact]
        public async Task Delete_tournament_succeeds()
        {
            var tournament = DatabaseFixture.TestData.TournamentInThePastWithFullDetails!;
            var anyOtherTournament = DatabaseFixture.TestData.Tournaments.First(t => t.TournamentId != tournament.TournamentId);

            await Repository.DeleteTournament(tournament, MemberKey, MemberName);

            using (var connection = DatabaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                var shouldBeDeleted = await connection.QuerySingleOrDefaultAsync<Guid?>($"SELECT TournamentId FROM {Tables.Tournament} WHERE TournamentId = @TournamentId",
                    new { tournament.TournamentId });
                Assert.Null(shouldBeDeleted);

                var shouldBeUnaffected = await connection.QuerySingleOrDefaultAsync<Guid?>($"SELECT TournamentId FROM {Tables.Tournament} WHERE TournamentId = @TournamentId",
                    new { anyOtherTournament.TournamentId });
                Assert.NotNull(shouldBeUnaffected);
            }
        }
    }
}
