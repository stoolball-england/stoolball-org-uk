using System;
using Stoolball.Teams;
using Xunit;

namespace Stoolball.UnitTests.Teams
{
    public class TeamEqualityComparerTests
    {
        [Fact]
        public void Compares_TeamId_if_present()
        {
            var teamA = new Team { TeamId = Guid.NewGuid(), TeamRoute = "/teams/team-a" };
            var teamB = new Team { TeamId = teamA.TeamId, TeamRoute = "/teams/team-b" };
            var comparer = new TeamEqualityComparer();

            var resultA = comparer.Equals(teamA, teamB);

            teamB.TeamId = Guid.NewGuid();
            var resultB = comparer.Equals(teamA, teamB);

            Assert.True(resultA);
            Assert.False(resultB);
        }

        [Fact]
        public void Compares_TeamRoute_if_no_TeamId()
        {
            var teamA = new Team { TeamId = null, TeamRoute = "/teams/team-a" };
            var teamB = new Team { TeamId = null, TeamRoute = teamA.TeamRoute };
            var comparer = new TeamEqualityComparer();

            var resultA = comparer.Equals(teamA, teamB);

            teamB.TeamRoute = "/teams/team-b";
            var resultB = comparer.Equals(teamA, teamB);

            Assert.True(resultA);
            Assert.False(resultB);
        }

        [Fact]
        public void Returns_false_if_no_TeamId_or_TeamRoute()
        {
            var teamA = new Team();
            var teamB = new Team();
            var comparer = new TeamEqualityComparer();

            var result = comparer.Equals(teamA, teamB);

            Assert.False(result);
        }
    }
}
