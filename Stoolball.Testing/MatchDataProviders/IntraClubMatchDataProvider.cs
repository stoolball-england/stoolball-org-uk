namespace Stoolball.Testing.MatchDataProviders
{
    /// <summary>
    /// Ensures there's always an intra-club match to test, where the same team is on both sides of the match.
    /// </summary>
    internal class IntraClubMatchDataProvider : BaseMatchDataProvider
    {
        private readonly Randomiser _randomiser;
        private readonly MatchFactory _matchFactory;
        private readonly PlayerFactory _playerFactory;
        private readonly Faker<Team> _teamFaker;

        public IntraClubMatchDataProvider(Randomiser randomiser, MatchFactory matchFactory, TeamFactory teamFactory, PlayerFactory playerFactory)
        {
            _randomiser = randomiser ?? throw new ArgumentNullException(nameof(randomiser));
            _matchFactory = matchFactory ?? throw new ArgumentNullException(nameof(matchFactory));
            _playerFactory = playerFactory ?? throw new ArgumentNullException(nameof(playerFactory));
            _teamFaker = teamFactory.CreateBasicTeamFaker();
        }

        internal override IEnumerable<Match> CreateMatches(TestData readOnlyTestData)
        {
            var team = _teamFaker.Generate();
            var players = _playerFactory.CreatePlayerIdentityFaker(team).Generate(11);

            return [_matchFactory.CreateMatchBetween(team, players, team, players, _randomiser.FiftyFiftyChance(), readOnlyTestData, nameof(IntraClubMatchDataProvider))];
        }
    }
}
