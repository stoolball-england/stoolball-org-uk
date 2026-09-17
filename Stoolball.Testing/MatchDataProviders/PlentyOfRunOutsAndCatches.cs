namespace Stoolball.Testing.MatchDataProviders
{
    /// <summary>
    /// Guarantees at least eight distinct players have a run-out, and at least eight distinct players have a catch,
    /// regardless of how the unseeded <see cref="Randomiser"/> happens to shape the rest of the data this run.
    /// Tests such as Read_most_runouts_returns_results_equal_to_max_with_max_results_filter and its catches
    /// equivalent need a real 5th and 6th placed player to force a tie between, so there must always be at least
    /// six players with a non-zero total for each statistic.
    /// </summary>
    internal class PlentyOfRunOutsAndCatches : BaseMatchDataProvider
    {
        private const int PlayersCreditedPerStatistic = 8;

        private readonly MatchFactory _matchFactory;
        private readonly PlayerFactory _playerFactory;
        private readonly Faker<Team> _teamFaker;

        public PlentyOfRunOutsAndCatches(MatchFactory matchFactory, TeamFactory teamFactory, PlayerFactory playerFactory)
        {
            _matchFactory = matchFactory ?? throw new ArgumentNullException(nameof(matchFactory));
            _playerFactory = playerFactory ?? throw new ArgumentNullException(nameof(playerFactory));
            _teamFaker = teamFactory.CreateBasicTeamFaker();
        }

        internal override IEnumerable<Match> CreateMatches(TestData readOnlyTestData)
        {
            var match = _matchFactory.CreateMatchInThePast(false, readOnlyTestData, nameof(PlentyOfRunOutsAndCatches));

            var teams = _teamFaker.Generate(2);
            match.Teams.Add(new TeamInMatch { MatchTeamId = Guid.NewGuid(), Team = teams[0], TeamRole = TeamRole.Home, PlayingAsTeamName = teams[0].TeamName });
            match.Teams.Add(new TeamInMatch { MatchTeamId = Guid.NewGuid(), Team = teams[1], TeamRole = TeamRole.Away, PlayingAsTeamName = teams[1].TeamName });

            // Innings 0: home team bats, away team fields and is credited with a run-out for each of several distinct players.
            match.MatchInnings[0].BattingMatchTeamId = match.Teams[0].MatchTeamId;
            match.MatchInnings[0].BattingTeam = match.Teams[0];
            match.MatchInnings[0].BowlingMatchTeamId = match.Teams[1].MatchTeamId;
            match.MatchInnings[0].BowlingTeam = match.Teams[1];

            var firstInningsBatterFaker = _playerFactory.CreatePlayerIdentityFaker(match.Teams[0].Team!);
            var runOutFielderFaker = _playerFactory.CreatePlayerIdentityFaker(match.Teams[1].Team!);

            for (var i = 0; i < PlayersCreditedPerStatistic; i++)
            {
                match.MatchInnings[0].PlayerInnings.Add(new PlayerInnings
                {
                    PlayerInningsId = Guid.NewGuid(),
                    BattingPosition = i + 1,
                    Batter = firstInningsBatterFaker.Generate(),
                    DismissalType = DismissalType.RunOut,
                    DismissedBy = runOutFielderFaker.Generate(),
                    RunsScored = 0,
                    BallsFaced = 1
                });
            }

            // Innings 1: away team bats, home team fields and is credited with a catch for each of several distinct players.
            match.MatchInnings[1].BattingMatchTeamId = match.Teams[1].MatchTeamId;
            match.MatchInnings[1].BattingTeam = match.Teams[1];
            match.MatchInnings[1].BowlingMatchTeamId = match.Teams[0].MatchTeamId;
            match.MatchInnings[1].BowlingTeam = match.Teams[0];

            var secondInningsBatterFaker = _playerFactory.CreatePlayerIdentityFaker(match.Teams[1].Team!);
            var catchFielderFaker = _playerFactory.CreatePlayerIdentityFaker(match.Teams[0].Team!);

            for (var i = 0; i < PlayersCreditedPerStatistic; i++)
            {
                match.MatchInnings[1].PlayerInnings.Add(new PlayerInnings
                {
                    PlayerInningsId = Guid.NewGuid(),
                    BattingPosition = i + 1,
                    Batter = secondInningsBatterFaker.Generate(),
                    DismissalType = DismissalType.Caught,
                    DismissedBy = catchFielderFaker.Generate(),
                    RunsScored = 0,
                    BallsFaced = 1
                });
            }

            return new[] { match };
        }
    }
}
