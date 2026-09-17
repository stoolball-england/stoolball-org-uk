namespace Stoolball.Testing.PlayerDataProviders
{
    internal class PlayersLinkedToMembersProvider(TeamFactory _teamFactory, PlayerFactory _playerFactory) : BasePlayerDataProvider
    {
        private readonly Faker<Team> _teamFaker = _teamFactory.CreateBasicTeamFaker();

        internal override IEnumerable<Player> CreatePlayers(TestData readOnlyTestData)
        {
            var team = _teamFaker.Generate();

            // two players with only one identity, on the same team, and both linked to a member
            var playerIdentityFaker = _playerFactory.CreatePlayerIdentityFaker(team);
            var identities = playerIdentityFaker.Generate(3);
            var players = new List<Player>();

            for (var i = 0; i < 2; i++)
            {
                identities[i].LinkedBy = PlayerIdentityLinkedBy.Member;
                identities[i].Player!.MemberKey = Guid.NewGuid();
                players.Add(identities[i].Player!);
            }

            // another player on the same team, with two identities both linked by member
            var twoIdentitiesLinkedByMember = playerIdentityFaker.Generate(2);
            var playerWithTwoIdentitiesLinkedByMember = twoIdentitiesLinkedByMember[0].Player!;
            playerWithTwoIdentitiesLinkedByMember.MemberKey = Guid.NewGuid();

            twoIdentitiesLinkedByMember[0].LinkedBy = PlayerIdentityLinkedBy.Member;
            twoIdentitiesLinkedByMember[1].Player = playerWithTwoIdentitiesLinkedByMember;
            twoIdentitiesLinkedByMember[1].LinkedBy = PlayerIdentityLinkedBy.Member;
            playerWithTwoIdentitiesLinkedByMember.PlayerIdentities.Add(twoIdentitiesLinkedByMember[1]);
            players.Add(playerWithTwoIdentitiesLinkedByMember);

            // another player on the same team, not linked to a member
            var playerWithoutMember = identities[2].Player!;
            players.Add(playerWithoutMember);

            // another player on the same team, with two identities but only one linked by member
            var twoIdentitiesOneLinkedByMember = playerIdentityFaker.Generate(2);
            var playerWithTwoIdentitiesOneLinkedByMember = twoIdentitiesOneLinkedByMember[0].Player!;
            playerWithTwoIdentitiesOneLinkedByMember.MemberKey = Guid.NewGuid();

            twoIdentitiesOneLinkedByMember[0].LinkedBy = PlayerIdentityLinkedBy.Member;

            twoIdentitiesOneLinkedByMember[1].Player = playerWithTwoIdentitiesOneLinkedByMember;
            twoIdentitiesOneLinkedByMember[1].LinkedBy = PlayerIdentityLinkedBy.Team;
            playerWithTwoIdentitiesOneLinkedByMember.PlayerIdentities.Add(twoIdentitiesOneLinkedByMember[1]);

            players.Add(playerWithTwoIdentitiesOneLinkedByMember);

            return players;
        }
    }
}
