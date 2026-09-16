using Stoolball.Testing.PlayerDataProviders;

namespace Stoolball.Testing.CompetitionDataProviders
{
    internal class CompetitionWithOneSeasonDataProvider(CompetitionFactory _competitionFactory, SeasonFactory _seasonFactory) : BaseCompetitionDataProvider
    {
        internal override IEnumerable<Competition> CreateCompetitions(TestData readOnlyTestData)
        {
            var competition = _competitionFactory.CreateFaker().Generate();
            competition.UntilYear = 2021;

            var season = _seasonFactory.CreateFaker(competition, 2020, 2020).Generate();
            competition.Seasons.Add(season);

            return [competition];
        }
    }
}
