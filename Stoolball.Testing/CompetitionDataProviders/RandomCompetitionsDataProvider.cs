namespace Stoolball.Testing.CompetitionDataProviders
{
    /// <summary>
    /// Creates ten random competitions with one season each, alternating between full details and random details.
    /// Seasons with full details have two teams chosen at random from the teams in the test data.
    /// </summary>
    internal class RandomCompetitionsDataProvider(Randomiser _randomiser, CompetitionFactory _competitionFactory, SeasonFactory _seasonFactory) : BaseCompetitionDataProvider
    {
        private readonly Faker<Competition> _competitionFaker = _competitionFactory.CreateFaker();

        internal override IEnumerable<Competition> CreateCompetitions(TestData readOnlyTestData)
        {
            if (readOnlyTestData.Teams.Count < 2) { throw new ArgumentException($"{nameof(readOnlyTestData.Teams)} must contain at least two teams"); }

            var competitions = new List<Competition>();
            for (var i = 0; i < 10; i++)
            {
                if (_randomiser.IsEven(i))
                {
                    var competition = _competitionFactory.CreateCompetitionWithFullDetails();
                    competitions.Add(competition);

                    var team1 = readOnlyTestData.Teams[_randomiser.PositiveIntegerLessThan(readOnlyTestData.Teams.Count)];
                    Team team2;
                    do
                    {
                        team2 = readOnlyTestData.Teams[_randomiser.PositiveIntegerLessThan(readOnlyTestData.Teams.Count)];
                    }
                    while (team2.TeamId == team1.TeamId);

                    var existingSummerSeasonsForCompetition = competition.Seasons.Where(x => x.FromYear == x.UntilYear).Select(x => x.FromYear);
                    var newSummerSeason = DateTime.Now.Year - i;
                    while (existingSummerSeasonsForCompetition.Contains(newSummerSeason))
                    {
                        newSummerSeason--;
                    }

                    competition.Seasons.Add(_seasonFactory.CreateSeasonWithFullDetails(competition, newSummerSeason, newSummerSeason, team1, team2));
                }
                else
                {
                    var competition = _competitionFaker.Generate();
                    competitions.Add(competition);
                    competition.Seasons.Add(_seasonFactory.CreateFaker(competition, DateTime.Now.Year - i, DateTime.Now.Year - i).Generate());
                }
            }
            return competitions;
        }
    }
}
