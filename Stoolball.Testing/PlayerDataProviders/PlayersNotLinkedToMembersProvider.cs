namespace Stoolball.Testing.PlayerDataProviders
{
    internal class PlayersNotLinkedToMembersProvider(TeamFactory teamFactory, PlayerFactory playerFactory) : BasePlayerDataProvider
    {
        private readonly Faker<Team> _teamFaker = teamFactory.CreateFaker();
        private readonly Faker<Player> _playerFaker = playerFactory.CreatePlayerFaker();
        private readonly Faker<PlayerIdentity> _playerIdentityFaker = playerFactory.CreatePlayerIdentityFaker();

        internal override IEnumerable<Player> CreatePlayers(TestData readOnlyTestData)
        {
            var team = _teamFaker.Generate();

            // player with a single identity
            var playerWithSingleIdentity = _playerFaker.Generate();
            playerWithSingleIdentity.PlayerIdentities.Add(_playerIdentityFaker.Generate());
            playerWithSingleIdentity.PlayerIdentities[0].Player = playerWithSingleIdentity;
            playerWithSingleIdentity.PlayerIdentities[0].Team = team;
            playerWithSingleIdentity.PlayerIdentities[0].LinkedBy = PlayerIdentityLinkedBy.DefaultIdentity;

            // player with two identities both linked by team, on the same team, not linked to member
            var playerWithTwoIdentitiesLinkedByTeam = CreatePlayerWithMultipleIdentities(2, PlayerIdentityLinkedBy.Team, team);
            var playerWithTwoIdentitiesLinkedByAdmin = CreatePlayerWithMultipleIdentities(2, PlayerIdentityLinkedBy.StoolballEngland, team);
            var playerWithThreeIdentitiesLinkedByTeam = CreatePlayerWithMultipleIdentities(3, PlayerIdentityLinkedBy.Team, team);

            return [playerWithSingleIdentity, playerWithTwoIdentitiesLinkedByTeam, playerWithTwoIdentitiesLinkedByAdmin, playerWithThreeIdentitiesLinkedByTeam];
        }

        private Player CreatePlayerWithMultipleIdentities(int howManyIdentities, PlayerIdentityLinkedBy linkedBy, Team team)
        {
            var player = _playerFaker.Generate();
            player.PlayerIdentities.AddRange(_playerIdentityFaker.Generate(howManyIdentities));

            foreach (var identity in player.PlayerIdentities)
            {
                identity.Player = player;
                identity.Team = team;
                identity.LinkedBy = linkedBy;
            }

            return player;
        }
    }
}
