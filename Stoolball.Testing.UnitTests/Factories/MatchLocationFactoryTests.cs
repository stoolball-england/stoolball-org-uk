using System.Linq;
using Stoolball.Teams;
using Stoolball.Testing.Factories;
using Xunit;

namespace Stoolball.Testing.UnitTests.Factories
{
    public class MatchLocationFactoryTests
    {
        private readonly MatchLocationFactory _matchLocationFactory = new();
        private readonly TeamFactory _teamFactory = new(new CompetitionFactory(new SeasonFactory(new OverSetFactory())), new SeasonFactory(new OverSetFactory()), new MatchLocationFactory());

        [Fact]
        public void Match_location_with_full_details_has_active_transient_and_inactive_teams()
        {
            var matchLocation = _matchLocationFactory.CreateMatchLocationWithFullDetails(_teamFactory.CreateFaker());

            Assert.Equal(4, matchLocation.Teams.Count);
            Assert.Contains(matchLocation.Teams, x => x.TeamType == TeamType.Transient);
            Assert.Contains(matchLocation.Teams, x => x.UntilYear.HasValue);
            Assert.Equal(2, matchLocation.Teams.Count(x => !x.UntilYear.HasValue && x.TeamType != TeamType.Transient));
            Assert.All(matchLocation.Teams, x => Assert.Contains(matchLocation, x.MatchLocations));
        }
    }
}
