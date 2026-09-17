namespace Stoolball.Testing.MatchDataProviders
{
    /// <summary>
    /// Ensures there's always a match to test where someone plays for both teams in different matches
    /// (eg they later transfer, or turn out for a different team in a cup competition).
    /// </summary>
    internal class DifferentTeamsWhereSomeonePlaysOnBothTeams : BaseMatchDataProvider
    {
        private readonly Randomiser _randomiser;
        private readonly MatchFactory _matchFactory;
        private readonly PlayerFactory _playerFactory;
        private readonly Faker<Team> _teamFaker;

        public DifferentTeamsWhereSomeonePlaysOnBothTeams(Randomiser randomiser, MatchFactory matchFactory, TeamFactory teamFactory, PlayerFactory playerFactory)
        {
            _randomiser = randomiser ?? throw new ArgumentNullException(nameof(randomiser));
            _matchFactory = matchFactory ?? throw new ArgumentNullException(nameof(matchFactory));
            _playerFactory = playerFactory ?? throw new ArgumentNullException(nameof(playerFactory));
            _teamFaker = teamFactory.CreateBasicTeamFaker();
        }

        internal override IEnumerable<Match> CreateMatches(TestData readOnlyTestData)
        {
            var teams = _teamFaker.Generate(2);

            // One player has an identity on each team.
            var identityOnTeam0 = _playerFactory.CreatePlayerIdentityFaker(teams[0]).Generate();
            var identityOnTeam1 = _playerFactory.CreatePlayerIdentityFaker(teams[1]).Generate();
            identityOnTeam1.Player = identityOnTeam0.Player;

            var team0Players = _playerFactory.CreatePlayerIdentityFaker(teams[0]).Generate(6);
            team0Players.Add(identityOnTeam0);

            var team1Players = _playerFactory.CreatePlayerIdentityFaker(teams[1]).Generate(6);
            team1Players.Add(identityOnTeam1);

            // Create a match between those teams, so the player is recorded as a batter for each team by virtue of being in both player pools.
            var match = _matchFactory.CreateMatchBetween(teams[0], team0Players, teams[1], team1Players, _randomiser.FiftyFiftyChance(), readOnlyTestData, nameof(DifferentTeamsWhereSomeonePlaysOnBothTeams));

            // Ensure they take a wicket too, so someone swaps sides during the innings (eg a batter is loaned as a fielder and takes a wicket).
            var wicketTaken = match.MatchInnings[0].PlayerInnings.First();
            wicketTaken.DismissalType = DismissalType.CaughtAndBowled;
            wicketTaken.Bowler = match.MatchInnings[0].BowlingTeam!.Team!.TeamId == teams[0].TeamId ? identityOnTeam0 : identityOnTeam1;

            return new[] { match };
        }
    }
}
