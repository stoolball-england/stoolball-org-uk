namespace Stoolball.Testing.MatchDataProviders
{
    internal class FiveWicketHaul : BaseMatchDataProvider
    {
        private readonly MatchFactory _matchFactory;
        private readonly PlayerFactory _playerFactory;
        private readonly Faker<Team> _teamFaker;

        public FiveWicketHaul(MatchFactory matchFactory, TeamFactory teamFactory, PlayerFactory playerFactory)
        {
            _matchFactory = matchFactory ?? throw new ArgumentNullException(nameof(matchFactory));
            _playerFactory = playerFactory ?? throw new ArgumentNullException(nameof(playerFactory));
            _teamFaker = teamFactory.CreateFaker();
        }

        internal override IEnumerable<Match> CreateMatches(TestData readOnlyTestData)
        {
            var match = _matchFactory.CreateMatchInThePast(false, readOnlyTestData, nameof(FiveWicketHaul));

            var teams = _teamFaker.Generate(2);
            match.Teams.Add(new TeamInMatch { MatchTeamId = Guid.NewGuid(), Team = teams[0], TeamRole = TeamRole.Home, PlayingAsTeamName = teams[0].TeamName });
            match.Teams.Add(new TeamInMatch { MatchTeamId = Guid.NewGuid(), Team = teams[1], TeamRole = TeamRole.Away, PlayingAsTeamName = teams[1].TeamName });

            match.MatchInnings[0].BattingMatchTeamId = match.Teams[0].MatchTeamId;
            match.MatchInnings[0].BattingTeam = match.Teams[0];
            match.MatchInnings[0].BowlingMatchTeamId = match.Teams[1].MatchTeamId;
            match.MatchInnings[0].BowlingTeam = match.Teams[1];

            match.MatchInnings[1].BattingMatchTeamId = match.Teams[1].MatchTeamId;
            match.MatchInnings[1].BattingTeam = match.Teams[1];
            match.MatchInnings[1].BowlingMatchTeamId = match.Teams[0].MatchTeamId;
            match.MatchInnings[1].BowlingTeam = match.Teams[0];

            var battingTeamPlayerFaker = _playerFactory.CreatePlayerIdentityFaker(match.Teams[0].Team!);
            var bowlerWithFiveWicketHaul = _playerFactory.CreatePlayerIdentityFaker(match.Teams[1].Team!).Generate();

            for (var i = 0; i < 5; i++)
            {
                match.MatchInnings[0].PlayerInnings.Add(new PlayerInnings
                {
                    PlayerInningsId = Guid.NewGuid(),
                    BattingPosition = i + 1,
                    Batter = battingTeamPlayerFaker.Generate(),
                    DismissalType = DismissalType.Bowled,
                    Bowler = bowlerWithFiveWicketHaul,
                    RunsScored = 0,
                    BallsFaced = 1
                });
            }

            return new[] { match };
        }
    }
}
