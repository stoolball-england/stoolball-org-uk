using System.Linq;
using Stoolball.Teams;
using Stoolball.Testing.Factories;
using Xunit;

namespace Stoolball.Testing.UnitTests.Factories
{
    public class ClubFactoryTests
    {
        private readonly ClubFactory _clubFactory = new(new TeamFactory(new CompetitionFactory(new SeasonFactory(new OverSetFactory())), new SeasonFactory(new OverSetFactory()), new MatchLocationFactory()));

        [Fact]
        public void Club_with_teams_has_one_inactive_and_two_active_teams()
        {
            var club = _clubFactory.CreateClubWithTeams();

            Assert.Equal(3, club.Teams.Count);
            Assert.Single(club.Teams, x => x.UntilYear.HasValue);
            Assert.Equal(2, club.Teams.Count(x => !x.UntilYear.HasValue));
            Assert.All(club.Teams, x => Assert.Same(club, x.Club));
        }
    }
}
