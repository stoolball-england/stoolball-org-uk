using Stoolball.Testing.Factories;
using Xunit;

namespace Stoolball.Testing.UnitTests.Factories
{
    public class CompetitionFactoryTests
    {
        private readonly CompetitionFactory _competitionFactory = new(new SeasonFactory(new OverSetFactory()));

        [Fact]
        public void Competition_with_full_details_has_three_seasons_linked_to_the_competition()
        {
            var competition = _competitionFactory.CreateCompetitionWithFullDetails();

            Assert.Equal(3, competition.Seasons.Count);
            Assert.All(competition.Seasons, x => Assert.Same(competition, x.Competition));
        }
    }
}
