namespace Stoolball.Testing.MatchDataProviders
{
    /// <summary>
    /// Ensures there's always a match with a bowler, linked to a member, who's taken wickets using two different identities.
    /// </summary>
    internal class MatchWithBowlerWithMultipleIdentities : BaseMatchDataProvider
    {
        private readonly MatchFactory _matchFactory;
        private readonly PlayerFactory _playerFactory;
        private readonly Faker<Team> _teamFaker;

        public MatchWithBowlerWithMultipleIdentities(MatchFactory matchFactory, TeamFactory teamFactory, PlayerFactory playerFactory)
        {
            _matchFactory = matchFactory ?? throw new ArgumentNullException(nameof(matchFactory));
            _playerFactory = playerFactory ?? throw new ArgumentNullException(nameof(playerFactory));
            _teamFaker = teamFactory.CreateBasicTeamFaker();
        }

        internal override IEnumerable<Match> CreateMatches(TestData readOnlyTestData)
        {
            var teams = _teamFaker.Generate(2);
            var battingTeam = teams[0];
            var bowlingTeam = teams[1];

            // A player linked to a member, with two identities on the bowling team, who both take wickets.
            var firstIdentity = _playerFactory.CreatePlayerIdentityFaker(bowlingTeam).Generate();
            var secondIdentity = _playerFactory.CreatePlayerIdentityFaker(bowlingTeam).Generate();
            secondIdentity.Player = firstIdentity.Player;
            firstIdentity.Player!.PlayerIdentities.Add(secondIdentity);
            firstIdentity.Player.MemberKey = Guid.NewGuid();
            firstIdentity.LinkedBy = PlayerIdentityLinkedBy.Member;
            secondIdentity.LinkedBy = PlayerIdentityLinkedBy.Member;

            var match = _matchFactory.CreateMatchInThePast(false, readOnlyTestData, nameof(MatchWithBowlerWithMultipleIdentities));

            var battingTeamInMatch = new TeamInMatch { MatchTeamId = Guid.NewGuid(), Team = battingTeam, TeamRole = TeamRole.Home, PlayingAsTeamName = battingTeam.TeamName };
            var bowlingTeamInMatch = new TeamInMatch { MatchTeamId = Guid.NewGuid(), Team = bowlingTeam, TeamRole = TeamRole.Away, PlayingAsTeamName = bowlingTeam.TeamName };
            match.Teams.Add(battingTeamInMatch);
            match.Teams.Add(bowlingTeamInMatch);

            match.MatchInnings[0].BattingMatchTeamId = battingTeamInMatch.MatchTeamId;
            match.MatchInnings[0].BattingTeam = battingTeamInMatch;
            match.MatchInnings[0].BowlingMatchTeamId = bowlingTeamInMatch.MatchTeamId;
            match.MatchInnings[0].BowlingTeam = bowlingTeamInMatch;

            match.MatchInnings[1].BattingMatchTeamId = bowlingTeamInMatch.MatchTeamId;
            match.MatchInnings[1].BattingTeam = bowlingTeamInMatch;
            match.MatchInnings[1].BowlingMatchTeamId = battingTeamInMatch.MatchTeamId;
            match.MatchInnings[1].BowlingTeam = battingTeamInMatch;

            var battingPlayerFaker = _playerFactory.CreatePlayerIdentityFaker(battingTeam);

            for (var i = 0; i < 4; i++)
            {
                match.MatchInnings[0].PlayerInnings.Add(new PlayerInnings
                {
                    PlayerInningsId = Guid.NewGuid(),
                    BattingPosition = i + 1,
                    Batter = battingPlayerFaker.Generate(),
                    DismissalType = DismissalType.Bowled,
                    Bowler = i % 2 == 0 ? firstIdentity : secondIdentity,
                    RunsScored = 0,
                    BallsFaced = 1
                });
            }

            return new[] { match };
        }
    }
}
