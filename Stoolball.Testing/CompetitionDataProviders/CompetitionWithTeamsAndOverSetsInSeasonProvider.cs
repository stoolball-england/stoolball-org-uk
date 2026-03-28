using Stoolball.Testing.PlayerDataProviders;

namespace Stoolball.Testing.CompetitionDataProviders
{
    internal class CompetitionWithTeamsAndOverSetsInSeasonProvider(CompetitionFactory _competitionFactory,
                                                        SeasonFactory _seasonFactory,
                                                        TeamFactory _teamFactory,
                                                        OverSetFactory _overSetFactory) : BaseCompetitionDataProvider
    {
        internal override IEnumerable<Competition> CreateCompetitions(TestData readOnlyTestData)
        {
            var competition = _competitionFactory.CreateFaker().Generate();
            var season = _seasonFactory.CreateFaker(competition).Generate();
            competition.Seasons.Add(season);

            var overSets = _overSetFactory.CreateFaker().Generate(4);
            season.DefaultOverSets.AddRange(overSets);

            var teams = _teamFactory.CreateFaker().Generate(3);
            season.Teams.AddRange(teams.Select(t => new TeamInSeason { Team = t, Season = season }));

            // Ensure one team withdrew from the season
            season.Teams[0].WithdrawnDate = new DateTimeOffset(new DateTime(season.FromYear, 6, 1), TimeSpan.Zero);

            // Ensure one team was in its last ever season
            season.Teams[1].Team!.UntilYear = season.FromYear;

            return [competition];
        }
    }
}
