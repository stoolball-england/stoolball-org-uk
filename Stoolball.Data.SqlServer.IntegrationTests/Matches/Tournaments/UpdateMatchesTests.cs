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
            throw new NotImplementedException();
        }

        [Fact]
        public async Task Matches_with_a_MatchId_have_order_in_tournament_updated_to_match_passed_collection()
        {
            throw new NotImplementedException();
        }

        [Fact]
        public async Task Matches_without_a_MatchId_are_created_and_added_to_tournament()
        {
            throw new NotImplementedException();

            // VERIFY
            //MatchType = MatchType.GroupMatch,
            //                    Tournament = tournament,
            //                    PlayerType = tournament.PlayerType,
            //                    PlayersPerTeam = tournament.PlayersPerTeam,
            //                    MatchLocation = tournament.TournamentLocation,
            //                    OrderInTournament = i + 1,
            //                    StartTime = tournament.StartTime.AddMinutes(45 * i),
            //                    StartTimeIsKnown = false,
            // PlayingAsTeamName is set to passed team name
            // First team in a match (if present) is set to the home team
            // Second team in a match (if present) is set to the away team
        }

        [Fact]
        public async Task Audits_and_logs()
        {
            throw new NotImplementedException();
        }
    }
}
