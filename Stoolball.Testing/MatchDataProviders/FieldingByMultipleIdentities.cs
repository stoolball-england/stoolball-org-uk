namespace Stoolball.Testing.MatchDataProviders
{
    internal class FieldingByMultipleIdentities : BaseMatchDataProvider
    {
        private readonly Randomiser _randomiser;
        private readonly MatchFactory _matchFactory;
        private readonly PlayerFactory _playerFactory;
        private readonly Faker<Team> _teamFaker;

        public FieldingByMultipleIdentities(Randomiser randomiser, MatchFactory matchFactory, TeamFactory teamFactory, PlayerFactory playerFactory)
        {
            _randomiser = randomiser ?? throw new ArgumentNullException(nameof(randomiser));
            _matchFactory = matchFactory ?? throw new ArgumentNullException(nameof(matchFactory));
            _playerFactory = playerFactory ?? throw new ArgumentNullException(nameof(playerFactory));
            _teamFaker = teamFactory.CreateFaker();
        }

        internal override IEnumerable<Match> CreateMatches(TestData readOnlyTestData)
        {
            var teams = _teamFaker.Generate(2);

            // Team 0 bats in the first innings and bowls in the second, so give it a player with two identities to take run-outs under both.
            var fielderWithMultipleIdentities = new List<PlayerIdentity>
            {
                _playerFactory.CreatePlayerIdentityFaker(teams[0]).Generate(),
                _playerFactory.CreatePlayerIdentityFaker(teams[0]).Generate()
            };
            fielderWithMultipleIdentities[1].Player = fielderWithMultipleIdentities[0].Player;

            var team0Players = _playerFactory.CreatePlayerIdentityFaker(teams[0]).Generate(6);
            team0Players.AddRange(fielderWithMultipleIdentities);

            // Team 1 bowls in the first innings, so give it a player with two identities to take catches under both.
            var catcherWithMultipleIdentities = new List<PlayerIdentity>
            {
                _playerFactory.CreatePlayerIdentityFaker(teams[1]).Generate(),
                _playerFactory.CreatePlayerIdentityFaker(teams[1]).Generate()
            };
            catcherWithMultipleIdentities[1].Player = catcherWithMultipleIdentities[0].Player;

            var team1Players = _playerFactory.CreatePlayerIdentityFaker(teams[1]).Generate(6);
            team1Players.AddRange(catcherWithMultipleIdentities);

            var match = _matchFactory.CreateMatchBetween(teams[0], team0Players, teams[1], team1Players, _randomiser.FiftyFiftyChance(), readOnlyTestData, nameof(FieldingByMultipleIdentities));

            // First innings: team 1 fields. The catcher with multiple identities takes catches under both.
            var firstInnings = match.MatchInnings[0];
            for (var i = 0; i < 6; i++)
            {
                if (i % 2 == 0)
                {
                    firstInnings.PlayerInnings[i].DismissalType = DismissalType.Caught;
                    firstInnings.PlayerInnings[i].DismissedBy = catcherWithMultipleIdentities[0];
                    firstInnings.PlayerInnings[i].Bowler = null;
                }
                else
                {
                    firstInnings.PlayerInnings[i].DismissalType = DismissalType.CaughtAndBowled;
                    firstInnings.PlayerInnings[i].DismissedBy = null;
                    firstInnings.PlayerInnings[i].Bowler = catcherWithMultipleIdentities[1];
                }
            }

            // Second innings: team 0 fields. The fielder with multiple identities completes run-outs under both.
            var secondInnings = match.MatchInnings[1];
            for (var i = 0; i < 6; i++)
            {
                secondInnings.PlayerInnings[i].DismissalType = DismissalType.RunOut;
                secondInnings.PlayerInnings[i].DismissedBy = fielderWithMultipleIdentities[i % 2];
                secondInnings.PlayerInnings[i].Bowler = null;
            }

            return new[] { match };
        }
    }
}
