using System;
using System.Collections.Generic;
using System.Linq;
using Bogus;
using Stoolball.Awards;
using Stoolball.Matches;
using Stoolball.Statistics;
using Stoolball.Teams;
using Stoolball.Testing.Fakers;

namespace Stoolball.Testing.MatchDataProviders
{
    internal class PlayersOnlyRecordedInOnePlace : BaseMatchDataProvider
    {
        private readonly MatchFactory _matchFactory;
        private readonly Award _playerOfTheMatchAward;
        private readonly Faker<Team> _teamFaker;
        private readonly Faker<PlayerIdentity> _playerIdentityFaker;

        public PlayersOnlyRecordedInOnePlace(MatchFactory matchFactory, IFakerFactory<Team> teamFakerFactory, IFakerFactory<PlayerIdentity> playerIdentityFakerFactory, Award playerOfTheMatchAward)
        {
            _matchFactory = matchFactory ?? throw new System.ArgumentNullException(nameof(matchFactory));
            _playerOfTheMatchAward = playerOfTheMatchAward ?? throw new ArgumentNullException(nameof(playerOfTheMatchAward));
            _teamFaker = teamFakerFactory.Create();
            _playerIdentityFaker = playerIdentityFakerFactory.Create();
        }

        internal override IEnumerable<Match> CreateMatches(TestData readOnlyTestData)
        {
            var match = _matchFactory.CreateMatchInThePastWithMinimalDetails();

            var teams = _teamFaker.Generate(2);
            match.Teams.Add(new TeamInMatch { MatchTeamId = Guid.NewGuid(), Team = teams[0], TeamRole = TeamRole.Home });
            match.Teams.Add(new TeamInMatch { MatchTeamId = Guid.NewGuid(), Team = teams[1], TeamRole = TeamRole.Away });

            match.MatchInnings[0].BattingMatchTeamId = match.Teams[0].MatchTeamId;
            match.MatchInnings[0].BattingTeam = match.Teams[0];
            match.MatchInnings[0].BowlingMatchTeamId = match.Teams[1].MatchTeamId;
            match.MatchInnings[0].BowlingTeam = match.Teams[1];

            match.MatchInnings[1].BowlingMatchTeamId = match.Teams[0].MatchTeamId;
            match.MatchInnings[1].BowlingTeam = match.Teams[0];
            match.MatchInnings[1].BattingMatchTeamId = match.Teams[1].MatchTeamId;
            match.MatchInnings[1].BattingTeam = match.Teams[1];

            var bowler = _playerIdentityFaker.Generate(1).Single();
            bowler.Team = match.Teams[1].Team;
            match.MatchInnings[0].OversBowled.Add(new Over
            {
                OverId = Guid.NewGuid(),
                OverNumber = 1,
                Bowler = bowler,
                BallsBowled = 8,
                RunsConceded = 10
            });

            // When removing a single identity, it's important that code can cope with another player innings that has minimal data.
            var batterBefore = _playerIdentityFaker.Generate(1).First();
            batterBefore.Team = match.Teams[0].Team;
            match.MatchInnings[0].PlayerInnings.Add(new PlayerInnings
            {
                PlayerInningsId = Guid.NewGuid(),
                BattingPosition = 2,
                Batter = batterBefore,
                DismissalType = DismissalType.NotOut,
                RunsScored = 50
            });

            var batterFielderBowler = _playerIdentityFaker.Generate(3);
            batterFielderBowler[0].Team = match.Teams[0].Team;
            batterFielderBowler[1].Team = match.Teams[1].Team;
            batterFielderBowler[2].Team = match.Teams[1].Team;
            match.MatchInnings[0].PlayerInnings.Add(new PlayerInnings
            {
                PlayerInningsId = Guid.NewGuid(),
                BattingPosition = 1,
                Batter = batterFielderBowler[0],
                DismissalType = DismissalType.Caught,
                DismissedBy = batterFielderBowler[1],
                Bowler = batterFielderBowler[2],
                RunsScored = 50,
                BallsFaced = 60
            });

            var batterAfter = _playerIdentityFaker.Generate(1).First();
            batterAfter.Team = match.Teams[0].Team;
            match.MatchInnings[0].PlayerInnings.Add(new PlayerInnings
            {
                PlayerInningsId = Guid.NewGuid(),
                BattingPosition = 2,
                Batter = batterAfter,
                DismissalType = DismissalType.DidNotBat
            });

            var awardWinner = _playerIdentityFaker.Generate(1)[0];
            awardWinner.Team = match.Teams[0].Team;
            match.Awards.Add(new MatchAward
            {
                AwardedToId = Guid.NewGuid(),
                Award = _playerOfTheMatchAward,
                PlayerIdentity = awardWinner
            });

            return new[] { match };
        }
    }
}
