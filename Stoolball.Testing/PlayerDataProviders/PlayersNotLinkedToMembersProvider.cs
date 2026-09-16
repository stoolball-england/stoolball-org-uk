namespace Stoolball.Testing.PlayerDataProviders
{
    internal class PlayersNotLinkedToMembersProvider(TeamFactory _teamFactory, PlayerFactory _playerFactory) : BasePlayerDataProvider
    {
        private readonly Faker<Team> _teamFaker = _teamFactory.CreateFaker();

        internal override IEnumerable<Player> CreatePlayers(TestData readOnlyTestData)
        {
            var team = _teamFaker.Generate();
            var playerIdentityFaker = _playerFactory.CreatePlayerIdentityFaker(team);

            // player with a single identity
            var singleIdentityForPlayer = playerIdentityFaker.Generate();
            singleIdentityForPlayer.LinkedBy = PlayerIdentityLinkedBy.DefaultIdentity;

            // player with two identities both linked by team, on the same team, not linked to member
            var playerWithTwoIdentitiesLinkedByTeam = CreatePlayerWithMultipleIdentities(playerIdentityFaker, 2, PlayerIdentityLinkedBy.Team);
            var playerWithTwoIdentitiesLinkedByAdmin = CreatePlayerWithMultipleIdentities(playerIdentityFaker, 2, PlayerIdentityLinkedBy.StoolballEngland);
            var playerWithThreeIdentitiesLinkedByTeam = CreatePlayerWithMultipleIdentities(playerIdentityFaker, 3, PlayerIdentityLinkedBy.Team);

            return [singleIdentityForPlayer.Player!, playerWithTwoIdentitiesLinkedByTeam, playerWithTwoIdentitiesLinkedByAdmin, playerWithThreeIdentitiesLinkedByTeam];
        }

        private static Player CreatePlayerWithMultipleIdentities(Faker<PlayerIdentity> playerIdentityFaker, int howManyIdentities, PlayerIdentityLinkedBy linkedBy)
        {
            var identities = playerIdentityFaker.Generate(howManyIdentities);
            var player = identities[0].Player!;
            identities[0].LinkedBy = linkedBy;

            for (var i = 1; i < identities.Count; i++)
            {
                identities[i].Player = player;
                identities[i].LinkedBy = linkedBy;
                player.PlayerIdentities.Add(identities[i]);
            }

            return player;
        }
    }
}
