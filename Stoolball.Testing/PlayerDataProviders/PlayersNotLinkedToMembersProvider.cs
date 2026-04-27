namespace Stoolball.Testing.PlayerDataProviders
{
    internal class PlayersNotLinkedToMembersProvider(TeamFactory _teamFactory, PlayerFactory _playerFactory) : BasePlayerDataProvider
    {
        private readonly Faker<Team> _teamFaker = _teamFactory.CreateFaker();
        private readonly Faker<Player> _playerFaker = _playerFactory.CreatePlayerFaker();

        internal override IEnumerable<Player> CreatePlayers(TestData readOnlyTestData)
        {
            var team = _teamFaker.Generate();
            var playerIdentityFaker = _playerFactory.CreatePlayerIdentityFaker(team);

            // player with a single identity
            var playerWithSingleIdentity = _playerFaker.Generate();
            playerWithSingleIdentity.PlayerIdentities.Add(playerIdentityFaker.Generate());
            playerWithSingleIdentity.PlayerIdentities[0].Player = playerWithSingleIdentity;
            playerWithSingleIdentity.PlayerIdentities[0].LinkedBy = PlayerIdentityLinkedBy.DefaultIdentity;

            // player with two identities both linked by team, on the same team, not linked to member
            var playerWithTwoIdentitiesLinkedByTeam = CreatePlayerWithMultipleIdentities(playerIdentityFaker, 2, PlayerIdentityLinkedBy.Team, team);
            var playerWithTwoIdentitiesLinkedByAdmin = CreatePlayerWithMultipleIdentities(playerIdentityFaker, 2, PlayerIdentityLinkedBy.StoolballEngland, team);
            var playerWithThreeIdentitiesLinkedByTeam = CreatePlayerWithMultipleIdentities(playerIdentityFaker, 3, PlayerIdentityLinkedBy.Team, team);

            return [playerWithSingleIdentity, playerWithTwoIdentitiesLinkedByTeam, playerWithTwoIdentitiesLinkedByAdmin, playerWithThreeIdentitiesLinkedByTeam];
        }

        private Player CreatePlayerWithMultipleIdentities(Faker<PlayerIdentity> playerIdentityFaker, int howManyIdentities, PlayerIdentityLinkedBy linkedBy, Team team)
        {
            var player = _playerFaker.Generate();
            player.PlayerIdentities.AddRange(playerIdentityFaker.Generate(howManyIdentities));

            foreach (var identity in player.PlayerIdentities)
            {
                identity.Player = player;
                identity.LinkedBy = linkedBy;
            }

            return player;
        }
    }
}
