using System;
using System.Collections.Generic;
using System.Linq;
using Bogus;
using Stoolball.Statistics;
using Stoolball.Teams;
using Stoolball.Testing.Fakers;
using Stoolball.Testing.PlayerDataProviders;

namespace Stoolball.Testing.TeamDataProviders
{
    internal class PlayersLinkedToMembersProvider(IFakerFactory<Team> teamFakerFactory, IFakerFactory<Player> playerFakerFactory, IFakerFactory<PlayerIdentity> playerIdentityFakerFactory) : BasePlayerDataProvider
    {
        private readonly Faker<Team> _teamFaker = teamFakerFactory.Create();
        private readonly Faker<Player> _playerFaker = playerFakerFactory.Create();
        private readonly Faker<PlayerIdentity> _playerIdentityFaker = playerIdentityFakerFactory.Create();

        internal override IEnumerable<Player> CreatePlayers(TestData readOnlyTestData)
        {
            var team = _teamFaker.Generate(1).Single();

            // two players with only one identity, on the same team, and both linked to a member
            var players = _playerFaker.Generate(2);
            var identities = _playerIdentityFaker.Generate(3);

            for (var i = 0; i < 2; i++)
            {
                players[i].PlayerIdentities.Add(identities[i]);
                identities[i].Player = players[i];
                identities[i].Team = team;
                players[i].MemberKey = Guid.NewGuid();
            }

            // another player on the same team, not linked to a member
            var playerWithoutMember = _playerFaker.Generate(1).Single();
            playerWithoutMember.PlayerIdentities.Add(identities[2]);
            identities[2].Player = playerWithoutMember;
            identities[2].Team = team;
            players.Add(playerWithoutMember);

            return players;
        }
    }
}
