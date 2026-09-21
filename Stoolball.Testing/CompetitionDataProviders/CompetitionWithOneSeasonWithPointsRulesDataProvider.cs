namespace Stoolball.Testing.CompetitionDataProviders
{
    internal class CompetitionWithOneSeasonWithPointsRulesDataProvider(CompetitionFactory _competitionFactory, SeasonFactory _seasonFactory, TeamFactory _teamFactory) : BaseCompetitionDataProvider
    {
        internal override IEnumerable<Competition> CreateCompetitions(TestData readOnlyTestData)
        {
            var competition = _competitionFactory.CreateFaker().Generate();
            var teamFaker = _teamFactory.CreateBasicTeamFaker();
            var team1 = teamFaker.Generate();
            var team2 = teamFaker.Generate();

            competition.Seasons.Add(_seasonFactory.CreateSeasonWithFullDetails(competition, 2020, 2020, team1, team2));

            return [competition];
        }
    }
}
