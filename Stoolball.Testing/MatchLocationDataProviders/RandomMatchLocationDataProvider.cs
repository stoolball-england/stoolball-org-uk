namespace Stoolball.Testing.MatchLocationDataProviders
{
    internal class RandomMatchLocationDataProvider : BaseMatchLocationDataProvider
    {
        private readonly Randomiser _randomiser;
        private readonly MatchLocationFactory _matchLocationFactory;
        private readonly Faker<Team> _basicTeamFaker;
        private readonly Faker<MatchLocation> _matchLocationFaker;

        internal RandomMatchLocationDataProvider(Randomiser randomiser, MatchLocationFactory matchLocationFactory, TeamFactory teamFactory)
        {
            _randomiser = randomiser ?? throw new ArgumentNullException(nameof(randomiser));
            _matchLocationFactory = matchLocationFactory ?? throw new ArgumentNullException(nameof(matchLocationFactory));
            _basicTeamFaker = teamFactory?.CreateBasicTeamFaker() ?? throw new ArgumentNullException(nameof(teamFactory));
            _matchLocationFaker = matchLocationFactory.CreateFaker();
        }

        internal override IEnumerable<MatchLocation> CreateMatchLocations(TestData readOnlyTestData)
        {
            var matchLocations = new List<MatchLocation>();
            for (var i = 0; i < 10; i++)
            {
                matchLocations.Add(_randomiser.IsEven(i) ? _matchLocationFactory.CreateMatchLocationWithFullDetails(_basicTeamFaker) : _matchLocationFaker.Generate());
            }
            return matchLocations;
        }
    }
}
