namespace Stoolball.Testing.PlayerDataProviders
{
    internal class PlayersLinkedToMembersOnSameTeamAsPlayersNotLinkedToMembersProvider(TeamFactory _teamFactory, PlayerFactory _playerFactory) : BasePlayerDataProvider
    {
        private readonly Faker<Team> _teamFaker = _teamFactory.CreateFaker();

        internal override IEnumerable<Player> CreatePlayers(TestData readOnlyTestData)
        {
            var team = _teamFaker.Generate();

            // range of players on the same team, with differing number of identities, some linked by member and some not
            var playerWithOneIdentityLinkedByMember = CreatePlayer(1, team, true);
            var playerWithOneIdentityNotLinkedByMember = CreatePlayer(1, team, false);
            var playerWithTwoIdentitiesLinkedByMember = CreatePlayer(2, team, true);
            var playerWithTwoIdentitiesNotLinkedByMember = CreatePlayer(2, team, false);

            var playerWithTwoIdentitiesOnDifferentTeamsLinkedByMember = CreatePlayer(2, team, true);
            playerWithTwoIdentitiesOnDifferentTeamsLinkedByMember.PlayerIdentities[1].Team = _teamFaker.Generate();

            return [playerWithOneIdentityLinkedByMember,
                    playerWithOneIdentityNotLinkedByMember,
                    playerWithTwoIdentitiesLinkedByMember,
                    playerWithTwoIdentitiesNotLinkedByMember,
                    playerWithTwoIdentitiesOnDifferentTeamsLinkedByMember];
        }

        private Player CreatePlayer(int identities, Team team, bool isLinkedToMember)
        {
            var playerIdentities = _playerFactory.CreatePlayerIdentityFaker(team).Generate(identities);
            var player = playerIdentities[0].Player!;
            if (isLinkedToMember)
            {
                player.MemberKey = Guid.NewGuid();
            }

            var linkedBy = isLinkedToMember ? PlayerIdentityLinkedBy.Member : PlayerIdentityLinkedBy.Team;
            playerIdentities[0].LinkedBy = linkedBy;

            for (var i = 1; i < playerIdentities.Count; i++)
            {
                playerIdentities[i].Player = player;
                playerIdentities[i].LinkedBy = linkedBy;
                player.PlayerIdentities.Add(playerIdentities[i]);
            }

            return player;
        }
    }
}
