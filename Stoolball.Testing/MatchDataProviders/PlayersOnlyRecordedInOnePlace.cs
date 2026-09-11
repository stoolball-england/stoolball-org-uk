using Stoolball.Awards;

namespace Stoolball.Testing.MatchDataProviders
{
    internal class PlayersOnlyRecordedInOnePlace : BaseMatchDataProvider
    {
        private readonly MatchFactory _matchFactory;
        private readonly PlayerFactory _playerFactory;
        private readonly Award _playerOfTheMatchAward;
        private readonly Faker<Team> _teamFaker;

        public PlayersOnlyRecordedInOnePlace(MatchFactory matchFactory, TeamFactory teamFactory, PlayerFactory playerFactory, Award playerOfTheMatchAward)
        {
            _matchFactory = matchFactory ?? throw new System.ArgumentNullException(nameof(matchFactory));
            _playerFactory = playerFactory ?? throw new ArgumentNullException(nameof(playerFactory));
            _playerOfTheMatchAward = playerOfTheMatchAward ?? throw new ArgumentNullException(nameof(playerOfTheMatchAward));
            _teamFaker = teamFactory.CreateFaker();
        }

        internal override IEnumerable<Match> CreateMatches(TestData readOnlyTestData)
        {
            var match = _matchFactory.CreateMatchInThePast(false, readOnlyTestData, nameof(PlayersOnlyRecordedInOnePlace));

            var teams = _teamFaker.Generate(2);
            match.Teams.Add(new TeamInMatch { MatchTeamId = Guid.NewGuid(), Team = teams[0], TeamRole = TeamRole.Home, PlayingAsTeamName = teams[0].TeamName });
            match.Teams.Add(new TeamInMatch { MatchTeamId = Guid.NewGuid(), Team = teams[1], TeamRole = TeamRole.Away, PlayingAsTeamName = teams[1].TeamName });

            match.MatchInnings[0].BattingMatchTeamId = match.Teams[0].MatchTeamId;
            match.MatchInnings[0].BattingTeam = match.Teams[0];
            match.MatchInnings[0].BowlingMatchTeamId = match.Teams[1].MatchTeamId;
            match.MatchInnings[0].BowlingTeam = match.Teams[1];

            match.MatchInnings[1].BowlingMatchTeamId = match.Teams[0].MatchTeamId;
            match.MatchInnings[1].BowlingTeam = match.Teams[0];
            match.MatchInnings[1].BattingMatchTeamId = match.Teams[1].MatchTeamId;
            match.MatchInnings[1].BattingTeam = match.Teams[1];

            var fieldingTeamPlayerFaker = _playerFactory.CreatePlayerIdentityFaker(match.Teams[1].Team!);
            var bowler = fieldingTeamPlayerFaker.Generate();
            match.MatchInnings[0].OversBowled.Add(new Over
            {
                OverId = Guid.NewGuid(),
                OverNumber = 1,
                OverSet = match.MatchInnings[0].OverSets[0],
                Bowler = bowler,
                BallsBowled = 8,
                RunsConceded = 10
            });

            // When removing a single identity, it's important that code can cope with another player innings that has minimal data.
            var battingTeamPlayerFaker = _playerFactory.CreatePlayerIdentityFaker(match.Teams[0].Team!);
            var batterBefore = battingTeamPlayerFaker.Generate(1).First();
            match.MatchInnings[0].PlayerInnings.Add(new PlayerInnings
            {
                PlayerInningsId = Guid.NewGuid(),
                BattingPosition = 2,
                Batter = batterBefore,
                DismissalType = DismissalType.NotOut,
                RunsScored = 50
            });

            match.MatchInnings[0].PlayerInnings.Add(new PlayerInnings
            {
                PlayerInningsId = Guid.NewGuid(),
                BattingPosition = 1,
                Batter = battingTeamPlayerFaker.Generate(),
                DismissalType = DismissalType.Caught,
                DismissedBy = fieldingTeamPlayerFaker.Generate(),
                Bowler = fieldingTeamPlayerFaker.Generate(),
                RunsScored = 50,
                BallsFaced = 60
            });

            var batterAfter = battingTeamPlayerFaker.Generate();
            batterAfter.Team = match.Teams[0].Team;
            match.MatchInnings[0].PlayerInnings.Add(new PlayerInnings
            {
                PlayerInningsId = Guid.NewGuid(),
                BattingPosition = 2,
                Batter = batterAfter,
                DismissalType = DismissalType.DidNotBat
            });

            var awardWinner = battingTeamPlayerFaker.Generate();
            match.Awards.Add(new MatchAward
            {
                AwardedToId = Guid.NewGuid(),
                Award = _playerOfTheMatchAward,
                PlayerIdentity = awardWinner
            });

            return [match];
        }
    }
}
