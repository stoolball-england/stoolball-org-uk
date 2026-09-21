namespace Stoolball.Testing.TeamDataProviders
{
    /// <summary>
    /// Creates a pool of random teams of 8 players each, where some players have more than one identity.
    /// </summary>
    internal class RandomTeamsDataProvider(Randomiser _randomiser, TeamFactory _teamFactory, PlayerFactory _playerFactory) : BaseTeamDataProvider
    {
        private readonly Faker<Team> _basicTeamFaker = _teamFactory.CreateBasicTeamFaker();
        private readonly Faker<Team> _detailedTeamFaker = _teamFactory.CreateDetailedTeamFaker();

        internal override IEnumerable<(Team team, List<PlayerIdentity> identities)> CreateTeams(TestData readOnlyTestData)
        {
            var poolOfTeams = new List<(Team team, List<PlayerIdentity> identities)>();
            for (var i = 0; i < 5; i++)
            {
                var team = _randomiser.IsEven(i) ? _detailedTeamFaker.Generate() : _basicTeamFaker.Generate();
                poolOfTeams.Add((team, _playerFactory.CreatePlayerIdentityFaker(team).Generate(8)));
            }

            ShareSomePlayersBetweenIdentities(poolOfTeams);

            return poolOfTeams;
        }

        /// <summary>
        /// Randomly assign at least two players from each team a second identity - one on the same team, one on a different team.
        /// This ensure we always have lots of teams with multiple identities for the same player for both scenarios.
        /// </summary>
        private void ShareSomePlayersBetweenIdentities(List<(Team team, List<PlayerIdentity> identities)> teamsWithIdentities)
        {
            foreach (var (team, playerIdentities) in teamsWithIdentities)
            {
                // On the same team
                var player1 = playerIdentities[_randomiser.PositiveIntegerLessThan(playerIdentities.Count)];
                PlayerIdentity player2;
                do
                {
                    player2 = playerIdentities[_randomiser.PositiveIntegerLessThan(playerIdentities.Count)];
                } while (player1.PlayerIdentityId == player2.PlayerIdentityId);
                player2.Player = player1.Player;

                // On a different team
                var player3 = playerIdentities[_randomiser.PositiveIntegerLessThan(playerIdentities.Count)];
                (Team? targetTeam, List<PlayerIdentity>? targetIdentities) = (null, null);
                do
                {
                    (targetTeam, targetIdentities) = teamsWithIdentities[_randomiser.PositiveIntegerLessThan(teamsWithIdentities.Count)];
                } while (targetTeam.TeamId == team.TeamId);
                var player4 = targetIdentities[_randomiser.PositiveIntegerLessThan(targetIdentities.Count)];
                player4.Player = player3.Player;
            }

            var allIdentities = teamsWithIdentities.SelectMany(x => x.identities);
            foreach (var player in allIdentities.Select(x => x.Player).OfType<Player>())
            {
                player.PlayerIdentities = new PlayerIdentityList(allIdentities.Where(x => x.Player?.PlayerId == player.PlayerId));
            }
        }
    }
}
