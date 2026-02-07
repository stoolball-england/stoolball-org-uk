using System.Linq;
using Stoolball.Testing.Fakers;
using Stoolball.Testing.PlayerDataProviders;

namespace Stoolball.Testing.CompetitionDataProviders
{
    internal class CompetitionWithTeamsAndOverSetsInSeasonProvider(IFakerFactory<Competition> _competitionFakerFactory,
                                                        IFakerFactory<Season> _seasonFakerFactory,
                                                        IFakerFactory<Team> _teamFakerFactory,
                                                        IFakerFactory<OverSet> _overSetFakerFactory) : BaseCompetitionDataProvider
    {
        internal override IEnumerable<Competition> CreateCompetitions(TestData readOnlyTestData)
        {
            var competition = _competitionFakerFactory.Create().Generate();
            var season = _seasonFakerFactory.Create().Generate();
            season.Competition = competition;
            competition.Seasons.Add(season);

            var overSets = _overSetFakerFactory.Create().Generate(4);
            season.DefaultOverSets.AddRange(overSets);

            var teams = _teamFakerFactory.Create().Generate(3);
            season.Teams.AddRange(teams.Select(t => new TeamInSeason { Team = t, Season = season }));

            // Ensure one team withdrew from the season
            season.Teams[0].WithdrawnDate = new DateTimeOffset(new DateTime(season.FromYear, 6, 1), TimeSpan.Zero);

            // Ensure one team was in its last ever season
            season.Teams[1].Team!.UntilYear = season.FromYear;

            return [competition];
        }
    }
}
