using System;
using System.Collections.Generic;
using System.Linq;
using Humanizer;
using Stoolball.Awards;
using Stoolball.Matches;
using Stoolball.Statistics;

namespace Stoolball.Testing.MatchDataProviders
{
    internal class APlayerOnlyWinsAnAwardButHasPlayedOtherMatchesWithADifferentTeam : BaseMatchDataProvider
    {
        private readonly MatchFactory _matchFactory;
        private readonly IBowlingFiguresCalculator _bowlingFiguresCalculator;
        private readonly Award _playerOfTheMatchAward;
        private readonly Randomiser _randomiser;

        internal APlayerOnlyWinsAnAwardButHasPlayedOtherMatchesWithADifferentTeam(Randomiser randomiser, MatchFactory matchFactory, IBowlingFiguresCalculator bowlingFiguresCalculator, Award playerOfTheMatchAward)
        {
            _matchFactory = matchFactory ?? throw new ArgumentNullException(nameof(matchFactory));
            _bowlingFiguresCalculator = bowlingFiguresCalculator ?? throw new ArgumentNullException(nameof(bowlingFiguresCalculator));
            _playerOfTheMatchAward = playerOfTheMatchAward ?? throw new ArgumentNullException(nameof(playerOfTheMatchAward));
            _randomiser = randomiser ?? throw new ArgumentNullException(nameof(randomiser));
        }

        internal override IEnumerable<Match> CreateMatches(TestData readOnlyTestData)
        {
            if (readOnlyTestData.TeamWithFullDetails == null) { throw new ArgumentException($"{nameof(readOnlyTestData.TeamWithFullDetails)} cannot be null"); }
            if (!readOnlyTestData.Players.Any()) { throw new ArgumentException($"{nameof(readOnlyTestData.Players)} cannot be empty"); }
            if (!readOnlyTestData.Matches.Any()) { throw new ArgumentException($"{nameof(readOnlyTestData.Matches)} cannot be empty"); }


            // Create a match for testData.TeamWithFullDetails and any other team.
            // Important to use testData.TeamWithFullDetails because it's used in tests that filter by team.
            var anyOppositionTeam = readOnlyTestData.Teams.First(x => x.TeamId != readOnlyTestData.TeamWithFullDetails.TeamId &&
                                                              readOnlyTestData.PlayerIdentities.Any(pi => pi.Team?.TeamId == x.TeamId));
            var anyPlayerForTheOppositionTeam = readOnlyTestData.PlayerIdentities.First(pi => pi.Team?.TeamId == anyOppositionTeam.TeamId);

            var matchWhereThePlayerUnderTestBattedBowledAndFielded = _matchFactory.CreateMatchBetween(
                readOnlyTestData.TeamWithFullDetails, new List<PlayerIdentity>(),
                anyOppositionTeam, new List<PlayerIdentity>(),
                _randomiser.FiftyFiftyChance(), readOnlyTestData);


            // Create a player with identities on testData.TeamWithFullDetails and any other team.
            var playerUnderTest = new Player
            {
                PlayerId = Guid.NewGuid(),
                PlayerRoute = "/players/player-" + Guid.NewGuid(),
            };
            var someOtherTeamThePlayerBelongsTo = readOnlyTestData.Teams.First(x =>
                                                            x.TeamId != readOnlyTestData.TeamWithFullDetails.TeamId &&
                                                            x.TeamId != anyOppositionTeam?.TeamId);

            var identityOnSomeOtherTeamName = $"Identity A from {nameof(APlayerOnlyWinsAnAwardButHasPlayedOtherMatchesWithADifferentTeam)}";
            var identityOnSomeOtherTeam = new PlayerIdentity
            {
                PlayerIdentityId = Guid.NewGuid(),
                Player = playerUnderTest,
                PlayerIdentityName = identityOnSomeOtherTeamName,
                RouteSegment = identityOnSomeOtherTeamName.Kebaberize(),
                Team = someOtherTeamThePlayerBelongsTo,
            };
            playerUnderTest.PlayerIdentities.Add(identityOnSomeOtherTeam);

            var identityOnTeamWithFullDetailsName = $"Identity B from {nameof(APlayerOnlyWinsAnAwardButHasPlayedOtherMatchesWithADifferentTeam)}";
            var identityOnTeamWithFullDetails = new PlayerIdentity
            {
                PlayerIdentityId = Guid.NewGuid(),
                Player = playerUnderTest,
                PlayerIdentityName = identityOnTeamWithFullDetailsName,
                RouteSegment = identityOnTeamWithFullDetailsName.Kebaberize(),
                Team = readOnlyTestData.TeamWithFullDetails
            };
            playerUnderTest.PlayerIdentities.Add(identityOnTeamWithFullDetails);


            // Make sure the identity that IS on testData.TeamWithFullDetails has batted and taken wickets, catches and run-outs in a match, so that they have averages, economy etc.
            var battingInningsForTeamWithFullDetails = matchWhereThePlayerUnderTestBattedBowledAndFielded.MatchInnings.First(x => x.BattingTeam!.Team!.TeamId == readOnlyTestData.TeamWithFullDetails!.TeamId);
            battingInningsForTeamWithFullDetails.PlayerInnings.Add(new PlayerInnings
            {
                PlayerInningsId = Guid.NewGuid(),
                Batter = identityOnTeamWithFullDetails,
                DismissalType = DismissalType.Bowled,
                RunsScored = 40,
                BallsFaced = 36
            });
            var bowlingInningsForTeamWithFullDetails = matchWhereThePlayerUnderTestBattedBowledAndFielded.MatchInnings.First(x => x.BowlingTeam!.Team!.TeamId == readOnlyTestData.TeamWithFullDetails!.TeamId);
            bowlingInningsForTeamWithFullDetails.PlayerInnings.Add(new PlayerInnings
            {
                PlayerInningsId = Guid.NewGuid(),
                Batter = anyPlayerForTheOppositionTeam,
                DismissalType = DismissalType.CaughtAndBowled,
                Bowler = identityOnTeamWithFullDetails
            });
            bowlingInningsForTeamWithFullDetails.PlayerInnings.Add(new PlayerInnings
            {
                PlayerInningsId = Guid.NewGuid(),
                Batter = anyPlayerForTheOppositionTeam,
                DismissalType = DismissalType.RunOut,
                DismissedBy = identityOnTeamWithFullDetails
            });
            bowlingInningsForTeamWithFullDetails.OversBowled.Add(new Over
            {
                OverId = Guid.NewGuid(),
                OverSet = bowlingInningsForTeamWithFullDetails.OverSets.First(),
                Bowler = identityOnTeamWithFullDetails,
                BallsBowled = 8,
                RunsConceded = 10
            });
            bowlingInningsForTeamWithFullDetails.BowlingFigures = _bowlingFiguresCalculator.CalculateBowlingFigures(bowlingInningsForTeamWithFullDetails);
            identityOnTeamWithFullDetails.FirstPlayed = identityOnTeamWithFullDetails.LastPlayed = matchWhereThePlayerUnderTestBattedBowledAndFielded.StartTime;


            // Create a match between the player's two teams. This must be a different match to the one where the player has batted and taken wickets, catches and run-outs.
            // Make sure the identity NOT on testData.TeamWithFullDetails has an award but no other part in the match.
            // When test queries filter by testData.TeamWithFullDetails they should NOT include the match where the player won an award for a different team in TotalMatches for the player.
            var matchBetweenThePlayersTeams = _matchFactory.CreateMatchBetween(someOtherTeamThePlayerBelongsTo, new List<PlayerIdentity>(), readOnlyTestData.TeamWithFullDetails, new List<PlayerIdentity>(), _randomiser.FiftyFiftyChance(), readOnlyTestData);
            matchBetweenThePlayersTeams.MatchLocation = null;
            matchBetweenThePlayersTeams.Season = null;
            matchBetweenThePlayersTeams.Awards.Add(new MatchAward
            {
                AwardedToId = Guid.NewGuid(),
                Award = _playerOfTheMatchAward,
                PlayerIdentity = identityOnSomeOtherTeam
            });
            identityOnSomeOtherTeam.FirstPlayed = identityOnSomeOtherTeam.LastPlayed = matchBetweenThePlayersTeams.StartTime;

            return new Match[] { matchWhereThePlayerUnderTestBattedBowledAndFielded, matchBetweenThePlayersTeams };
        }
    }
}
