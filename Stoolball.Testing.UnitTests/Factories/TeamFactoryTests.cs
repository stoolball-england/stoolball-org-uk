using System.Linq;
using Stoolball.Teams;
using Stoolball.Testing.Factories;
using Xunit;

namespace Stoolball.Testing.UnitTests.Factories
{
    public class TeamFactoryTests
    {
        private readonly TeamFactory _teamFactory = new(new CompetitionFactory(new SeasonFactory(new OverSetFactory())), new SeasonFactory(new OverSetFactory()), new MatchLocationFactory());

        [Fact]
        public void Team_with_full_details_has_match_locations_and_seasons_for_a_ladies_competition()
        {
            var team = _teamFactory.CreateTeamWithFullDetails("Example team");

            Assert.Equal(PlayerType.Ladies, team.PlayerType);
            Assert.Equal(2, team.MatchLocations.Count);
            Assert.All(team.MatchLocations, x => Assert.Contains(team, x.Teams));

            Assert.Equal(2, team.Seasons.Count);
            Assert.All(team.Seasons, x => Assert.Same(team, x.Team));
            var competition = team.Seasons.First().Season!.Competition;
            Assert.All(team.Seasons, x => Assert.Same(competition, x.Season!.Competition));
        }
    }
}
