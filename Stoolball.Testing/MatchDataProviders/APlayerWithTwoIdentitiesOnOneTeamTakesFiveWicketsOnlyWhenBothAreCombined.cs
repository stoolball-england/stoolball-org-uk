using System;
using System.Collections.Generic;
using System.Linq;
using Stoolball.Matches;
using Stoolball.Statistics;
using Stoolball.Teams;

namespace Stoolball.Testing.MatchDataProviders
{
    internal class APlayerWithTwoIdentitiesOnOneTeamTakesFiveWicketsOnlyWhenBothAreCombined : BaseMatchDataProvider
    {
        private readonly Randomiser _randomiser;
        private readonly MatchFactory _matchFactory;
        private readonly IBowlingFiguresCalculator _bowlingFiguresCalculator;

        public APlayerWithTwoIdentitiesOnOneTeamTakesFiveWicketsOnlyWhenBothAreCombined(Randomiser randomiser, MatchFactory matchFactory, IBowlingFiguresCalculator bowlingFiguresCalculator)
        {
            _randomiser = randomiser ?? throw new ArgumentNullException(nameof(randomiser));
            _matchFactory = matchFactory ?? throw new ArgumentNullException(nameof(matchFactory));
            _bowlingFiguresCalculator = bowlingFiguresCalculator ?? throw new ArgumentNullException(nameof(bowlingFiguresCalculator));
        }

        internal override IEnumerable<Match> CreateMatches(TestData readOnlyTestData)
        {
            if (!readOnlyTestData.Players.Any()) { throw new ArgumentException($"{nameof(readOnlyTestData.Players)} cannot be empty"); }

            var anyPlayerWithMultipleIdentitiesOnOneTeam = readOnlyTestData.Players.First(p =>
                p.PlayerIdentities.Any(
                    identity => p.PlayerIdentities.Any(
                        otherIdentity => identity.PlayerIdentityId.HasValue && otherIdentity.PlayerIdentityId.HasValue &&
                                         identity.Team?.TeamId != null && otherIdentity.Team?.TeamId != null &&
                                         otherIdentity.PlayerIdentityId != identity.PlayerIdentityId &&
                                         otherIdentity.Team.TeamId == identity.Team.TeamId
                    )
                )
            );

            var teamWhereThatPlayerHasMultipleIdentities = anyPlayerWithMultipleIdentitiesOnOneTeam.PlayerIdentities
                .Select(x => x.Team)
                .OfType<Team>()
                .First(x => anyPlayerWithMultipleIdentitiesOnOneTeam.PlayerIdentities.Count(pi => pi.Team?.TeamId == x.TeamId) > 1);

            var playerIdentitiesThatWillTakeWickets = anyPlayerWithMultipleIdentitiesOnOneTeam.PlayerIdentities.Where(pi => pi.Team?.TeamId == teamWhereThatPlayerHasMultipleIdentities.TeamId).ToList();

            var differentTeamWithAtLeast5PlayerIdentities = readOnlyTestData.Teams.First(x =>
                    x.TeamId != null && x.TeamId != teamWhereThatPlayerHasMultipleIdentities.TeamId &&
                    readOnlyTestData.PlayerIdentities.Count(pi => pi.Team?.TeamId == x.TeamId) >= 5
                    );

            var playerIdentitiesWhoseWicketsWillBeTaken = readOnlyTestData.PlayerIdentities.Where(x => x.Team?.TeamId == differentTeamWithAtLeast5PlayerIdentities.TeamId).Take(5).ToList();

            var match = _matchFactory.CreateMatchBetween(teamWhereThatPlayerHasMultipleIdentities, new List<PlayerIdentity>(), differentTeamWithAtLeast5PlayerIdentities, new List<PlayerIdentity>(), _randomiser.FiftyFiftyChance(), readOnlyTestData, nameof(APlayerWithTwoIdentitiesOnOneTeamTakesFiveWicketsOnlyWhenBothAreCombined));
            match.StartTime = DateTimeOffset.UtcNow.AccurateToTheMinute().AddDays(-10).UtcToUkTime();

            var firstBowlingInningsForPlayerWithMultipleIdentities = match.MatchInnings.First(x => x.BowlingTeam?.Team?.TeamId == teamWhereThatPlayerHasMultipleIdentities.TeamId);

            for (var i = 0; i < playerIdentitiesWhoseWicketsWillBeTaken.Count; i++)
            {
                firstBowlingInningsForPlayerWithMultipleIdentities.PlayerInnings.Add(
                    new PlayerInnings
                    {
                        PlayerInningsId = Guid.NewGuid(),
                        BattingPosition = i + 1,
                        Batter = playerIdentitiesWhoseWicketsWillBeTaken[i],
                        DismissalType = DismissalType.Bowled,
                        Bowler = playerIdentitiesThatWillTakeWickets[_randomiser.IsEven(i) ? 0 : 1],
                        RunsScored = _randomiser.Between(0, 100),
                        BallsFaced = _randomiser.Between(1, 100),
                    });
            }

            firstBowlingInningsForPlayerWithMultipleIdentities.BowlingFigures = _bowlingFiguresCalculator.CalculateBowlingFigures(firstBowlingInningsForPlayerWithMultipleIdentities);

            return new[] { match };
        }
    }
}
