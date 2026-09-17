namespace Stoolball.Testing.MatchDataProviders
{
    internal class TeamScoresButNoPlayerData : BaseMatchDataProvider
    {
        private readonly Randomiser _randomiser;
        private readonly MatchFactory _matchFactory;
        private readonly Faker<Team> _teamFaker;

        public TeamScoresButNoPlayerData(Randomiser randomiser, MatchFactory matchFactory, TeamFactory teamFactory)
        {
            _randomiser = randomiser ?? throw new ArgumentNullException(nameof(randomiser));
            _matchFactory = matchFactory ?? throw new ArgumentNullException(nameof(matchFactory));
            _teamFaker = teamFactory.CreateBasicTeamFaker();
        }

        internal override IEnumerable<Match> CreateMatches(TestData readOnlyTestData)
        {
            var teams = _teamFaker.Generate(2);

            // No player identities are given to either team, so the match has team scores but no scorecard - this must still be included in team score averages.
            var match = _matchFactory.CreateMatchBetween(teams[0], new List<PlayerIdentity>(), teams[1], new List<PlayerIdentity>(), _randomiser.FiftyFiftyChance(), readOnlyTestData, nameof(TeamScoresButNoPlayerData));

            match.MatchInnings[0].Byes = 3;
            match.MatchInnings[0].Wides = 5;
            match.MatchInnings[0].NoBalls = 7;
            match.MatchInnings[0].BonusOrPenaltyRuns = 1;
            match.MatchInnings[0].Runs = 123;
            match.MatchInnings[0].Wickets = 6;

            match.MatchInnings[1].Byes = 2;
            match.MatchInnings[1].Wides = 4;
            match.MatchInnings[1].NoBalls = 6;
            match.MatchInnings[1].BonusOrPenaltyRuns = 2;
            match.MatchInnings[1].Runs = 144;
            match.MatchInnings[1].Wickets = 3;

            return new[] { match };
        }
    }
}
