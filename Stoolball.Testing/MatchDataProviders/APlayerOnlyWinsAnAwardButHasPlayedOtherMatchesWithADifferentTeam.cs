using System;
using System.Collections.Generic;
using System.Linq;
using Humanizer;
using Stoolball.Awards;
using Stoolball.Matches;
using Stoolball.Statistics;
using Stoolball.Testing.Factories;

namespace Stoolball.Testing.MatchDataProviders
{
    internal class APlayerOnlyWinsAnAwardButHasPlayedOtherMatchesWithADifferentTeam : BaseMatchDataProvider
    {
        private readonly MatchFactory _matchFactory;
        private readonly Award _playerOfTheMatchAward;
        private readonly Randomiser _randomiser;
        private readonly Faker<Team> _teamFaker;
        private readonly PlayerFactory _playerFactory;

        internal APlayerOnlyWinsAnAwardButHasPlayedOtherMatchesWithADifferentTeam(Randomiser randomiser, MatchFactory matchFactory, Award playerOfTheMatchAward, TeamFactory teamFactory, PlayerFactory playerFactory)
        {
            _matchFactory = matchFactory ?? throw new ArgumentNullException(nameof(matchFactory));
            _playerOfTheMatchAward = playerOfTheMatchAward ?? throw new ArgumentNullException(nameof(playerOfTheMatchAward));
            _randomiser = randomiser ?? throw new ArgumentNullException(nameof(randomiser));
            _teamFaker = teamFactory?.CreateBasicTeamFaker() ?? throw new ArgumentNullException(nameof(teamFactory));
            _playerFactory = playerFactory ?? throw new ArgumentNullException(nameof(playerFactory));
        }

        internal override IEnumerable<Match> CreateMatches(TestData readOnlyTestData)
        {
            // Create teams for the scenario, rather than using any team already in the test data, so that the scenario is not affected by other matches.
            var teamThePlayerPlaysFor = _teamFaker.Generate();
            var anyOppositionTeam = _teamFaker.Generate();
            var someOtherTeamThePlayerBelongsTo = _teamFaker.Generate();
            var anyPlayerForTheOppositionTeam = _playerFactory.CreatePlayerIdentityFaker(anyOppositionTeam).Generate();

            // Create a match for the team the player plays for and any other team.
            var matchWhereThePlayerUnderTestBattedBowledAndFielded = _matchFactory.CreateMatchBetween(
                teamThePlayerPlaysFor, new List<PlayerIdentity>(),
                anyOppositionTeam, new List<PlayerIdentity>(),
                _randomiser.FiftyFiftyChance(), readOnlyTestData, nameof(APlayerOnlyWinsAnAwardButHasPlayedOtherMatchesWithADifferentTeam));


            // Create a player with identities on the team the player plays for and another team.
            var playerUnderTest = new Player
            {
                PlayerId = Guid.NewGuid(),
                PlayerRoute = "/players/player-" + Guid.NewGuid(),
            };
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

            var identityOnTeamThePlayerPlaysForName = $"Identity B from {nameof(APlayerOnlyWinsAnAwardButHasPlayedOtherMatchesWithADifferentTeam)}";
            var identityOnTeamThePlayerPlaysFor = new PlayerIdentity
            {
                PlayerIdentityId = Guid.NewGuid(),
                Player = playerUnderTest,
                PlayerIdentityName = identityOnTeamThePlayerPlaysForName,
                RouteSegment = identityOnTeamThePlayerPlaysForName.Kebaberize(),
                Team = teamThePlayerPlaysFor
            };
            playerUnderTest.PlayerIdentities.Add(identityOnTeamThePlayerPlaysFor);


            // Make sure the identity that IS on the team the player plays for has batted and taken wickets, catches and run-outs in a match, so that they have averages, economy etc.
            var battingInningsForTeamThePlayerPlaysFor = matchWhereThePlayerUnderTestBattedBowledAndFielded.MatchInnings.First(x => x.BattingTeam!.Team!.TeamId == teamThePlayerPlaysFor.TeamId);
            battingInningsForTeamThePlayerPlaysFor.PlayerInnings.Add(new PlayerInnings
            {
                PlayerInningsId = Guid.NewGuid(),
                Batter = identityOnTeamThePlayerPlaysFor,
                DismissalType = DismissalType.Bowled,
                RunsScored = 40,
                BallsFaced = 36
            });
            var bowlingInningsForTeamThePlayerPlaysFor = matchWhereThePlayerUnderTestBattedBowledAndFielded.MatchInnings.First(x => x.BowlingTeam!.Team!.TeamId == teamThePlayerPlaysFor.TeamId);
            bowlingInningsForTeamThePlayerPlaysFor.PlayerInnings.Add(new PlayerInnings
            {
                PlayerInningsId = Guid.NewGuid(),
                Batter = anyPlayerForTheOppositionTeam,
                DismissalType = DismissalType.CaughtAndBowled,
                Bowler = identityOnTeamThePlayerPlaysFor
            });
            bowlingInningsForTeamThePlayerPlaysFor.PlayerInnings.Add(new PlayerInnings
            {
                PlayerInningsId = Guid.NewGuid(),
                Batter = anyPlayerForTheOppositionTeam,
                DismissalType = DismissalType.RunOut,
                DismissedBy = identityOnTeamThePlayerPlaysFor
            });
            bowlingInningsForTeamThePlayerPlaysFor.OversBowled.Add(new Over
            {
                OverId = Guid.NewGuid(),
                OverSet = bowlingInningsForTeamThePlayerPlaysFor.OverSets.First(),
                Bowler = identityOnTeamThePlayerPlaysFor,
                BallsBowled = 8,
                RunsConceded = 10
            });
            identityOnTeamThePlayerPlaysFor.FirstPlayed = identityOnTeamThePlayerPlaysFor.LastPlayed = matchWhereThePlayerUnderTestBattedBowledAndFielded.StartTime;


            // Create a match between the player's two teams. This must be a different match to the one where the player has batted and taken wickets, catches and run-outs.
            // Make sure the identity NOT on the team the player plays for has an award but no other part in the match.
            // When test queries filter by the team the player plays for they should NOT include the match where the player won an award for a different team in TotalMatches for the player.
            var matchBetweenThePlayersTeams = _matchFactory.CreateMatchBetween(someOtherTeamThePlayerBelongsTo, new List<PlayerIdentity>(), teamThePlayerPlaysFor, new List<PlayerIdentity>(), _randomiser.FiftyFiftyChance(), readOnlyTestData, nameof(APlayerOnlyWinsAnAwardButHasPlayedOtherMatchesWithADifferentTeam));
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
