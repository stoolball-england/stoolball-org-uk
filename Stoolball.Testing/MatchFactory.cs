using System;
using System.Collections.Generic;
using System.Linq;
using Stoolball.Awards;
using Stoolball.Matches;
using Stoolball.Statistics;
using Stoolball.Teams;

namespace Stoolball.Testing
{
    internal class MatchFactory
    {
        private readonly Randomiser _randomiser;
        private readonly Award _playerOfTheMatchAward;

        internal MatchFactory(Randomiser randomiser, Award playerOfTheMatchAward)
        {
            _randomiser = randomiser ?? throw new ArgumentNullException(nameof(randomiser));
            _playerOfTheMatchAward = playerOfTheMatchAward ?? throw new ArgumentNullException(nameof(playerOfTheMatchAward));
        }

        internal Match CreateMatchInThePast(bool addTeams, TestData testData, string routeTag)
        {
            var match = new Match
            {
                MatchId = Guid.NewGuid(),
                MatchName = "To be confirmed vs To be confirmed",
                MatchType = MatchType.KnockoutMatch,
                MatchInnings = new List<MatchInnings>
                {
                    new MatchInnings { MatchInningsId = Guid.NewGuid(), InningsOrderInMatch = 1 },
                    new MatchInnings { MatchInningsId = Guid.NewGuid(), InningsOrderInMatch = 2 }
                },
                MatchRoute = $"/matches/minimal-match-{Guid.NewGuid()}-generated-by-{routeTag}",
                StartTime = new DateTimeOffset(2020, 6, 6, 18, 30, 0, TimeSpan.FromHours(1))
            };

            if (addTeams)
            {
                AddTeamToMatch(match, TeamRole.Home, testData);
                AddTeamToMatch(match, TeamRole.Away, testData);
            }

            return match;
        }

        internal Match CreateTrainingSessionInTheFuture(int howManyTeams, TestData testData, string routeTag)
        {
            var training = new Match
            {
                MatchId = Guid.NewGuid(),
                MatchName = "Training session",
                MatchType = MatchType.TrainingSession,
                MatchRoute = $"/matches/training-session-{Guid.NewGuid()}-generated-by-{routeTag}",
                StartTime = DateTimeOffset.UtcNow.AddMonths(_randomiser.PositiveIntegerLessThan(30)).UtcToUkTime()
            };

            for (var i = 0; i < howManyTeams; i++)
            {
                AddTeamToMatch(training, TeamRole.Training, testData);
            }

            return training;
        }

        private static void AddTeamToMatch(Match match, TeamRole teamRole, TestData testData)
        {
            var teamsInMatch = match.Teams.Where(t => t.Team?.TeamId is not null).Select(t => t.Team!.TeamId!.Value);
            var team = testData.Teams.FirstOrDefault(t => t.TeamId is not null && !teamsInMatch.Contains(t.TeamId.Value));
            if (team is null) { throw new InvalidOperationException($"No more matching teams in {nameof(CreateTrainingSessionInTheFuture)}"); }

            match.Teams.Add(new TeamInMatch
            {
                MatchTeamId = Guid.NewGuid(),
                TeamRole = teamRole,
                Team = team,
                PlayingAsTeamName = team.TeamName
            });
        }

        internal Match CreateMatchBetween(Team teamA, List<PlayerIdentity> teamAPlayers, Team teamB, List<PlayerIdentity> teamBPlayers, bool homeTeamBatsFirst, TestData testData, string routeTag)
        {
            var teamAInMatch = new TeamInMatch
            {
                MatchTeamId = Guid.NewGuid(),
                TeamRole = homeTeamBatsFirst ? TeamRole.Home : TeamRole.Away,
                Team = teamA,
                PlayingAsTeamName = teamA.TeamName,
                WonToss = _randomiser.PositiveIntegerLessThan(2) == 0,
                BattedFirst = true
            };

            var teamBInMatch = new TeamInMatch
            {
                MatchTeamId = Guid.NewGuid(),
                TeamRole = homeTeamBatsFirst ? TeamRole.Away : TeamRole.Home,
                Team = teamB,
                PlayingAsTeamName = teamB.TeamName,
                WonToss = !teamAInMatch.WonToss,
                BattedFirst = false
            };

            var match = CreateMatchInThePast(false, testData, routeTag);
            match.Teams.Add(teamAInMatch);
            match.Teams.Add(teamBInMatch);

            match.StartTime = DateTimeOffset.UtcNow.AddMonths(_randomiser.PositiveIntegerLessThan(30) * -1 - 1).UtcToUkTime();

            // Some matches should have multiple innings
            if (_randomiser.PositiveIntegerLessThan(4) == 0)
            {
                match.MatchInnings.Add(CreateMatchInnings(3));
                match.MatchInnings.Add(CreateMatchInnings(4));
            }

            foreach (var innings in match.MatchInnings.Where(x => x.InningsOrderInMatch % 2 == 1))
            {
                var pairedInnings = match.MatchInnings.Single(x => x.InningsPair() == innings.InningsPair() && x.MatchInningsId != innings.MatchInningsId);
                CreateRandomScorecardData(match, innings, teamAInMatch, teamBInMatch, teamAPlayers, teamBPlayers);
                CreateRandomScorecardData(match, pairedInnings, teamBInMatch, teamAInMatch, teamBPlayers, teamAPlayers);
            }

            // Most matches have a match location
            if (_randomiser.OneInFourChance() && testData.MatchLocations.Any())
            {
                match.MatchLocation = testData.MatchLocations[_randomiser.PositiveIntegerLessThan(testData.MatchLocations.Count)];
            }

            // Most matches have a season and competition
            if (_randomiser.OneInFourChance() && testData.Competitions.Any())
            {
                do
                {
                    var competition = testData.Competitions[_randomiser.PositiveIntegerLessThan(testData.Competitions.Count)];
                    match.Season = competition.Seasons.FirstOrDefault();
                }
                while (match.Season == null);
            }

            // Give someone an award
            if (_randomiser.FiftyFiftyChance() && teamAPlayers.Any())
            {
                match.Awards.Add(new MatchAward
                {
                    AwardedToId = Guid.NewGuid(),
                    Award = _playerOfTheMatchAward,
                    PlayerIdentity = teamAPlayers.First(),
                    Reason = "Well played"
                });
            }

            return match;
        }

        private MatchInnings CreateMatchInnings(int inningsOrderInMatch)
        {
            return new MatchInnings
            {
                MatchInningsId = Guid.NewGuid(),
                InningsOrderInMatch = inningsOrderInMatch,
                NoBalls = _randomiser.PositiveIntegerLessThan(30),
                Wides = _randomiser.PositiveIntegerLessThan(30),
                Byes = _randomiser.PositiveIntegerLessThan(30),
                BonusOrPenaltyRuns = _randomiser.Between(-5, 4),
                Runs = _randomiser.Between(100, 249),
                Wickets = _randomiser.PositiveIntegerLessThan(11),
                OverSets = CreateOverSets()
            };
        }

        private void CreateRandomScorecardData(Match match, MatchInnings innings, TeamInMatch battingTeam, TeamInMatch bowlingTeam, List<PlayerIdentity> battingPlayers, List<PlayerIdentity> bowlingPlayers)
        {
            innings.BattingMatchTeamId = battingTeam.MatchTeamId;
            innings.BowlingMatchTeamId = bowlingTeam.MatchTeamId;
            innings.BattingTeam = battingTeam;
            innings.BowlingTeam = bowlingTeam;

            for (var p = 0; p < battingPlayers.Count; p++)
            {
                var fielderOrMissingData = _randomiser.PositiveIntegerLessThan(2) == 0 ? bowlingPlayers[_randomiser.PositiveIntegerLessThan(bowlingPlayers.Count)] : null;
                var bowlerOrMissingData = _randomiser.PositiveIntegerLessThan(2) == 0 ? bowlingPlayers[_randomiser.PositiveIntegerLessThan(bowlingPlayers.Count)] : null;
                innings.PlayerInnings.Add(CreateRandomPlayerInnings(p + 1, battingPlayers[p], fielderOrMissingData, bowlerOrMissingData));
            }

            // sometimes pick a random player to bat twice in the innings
            if (_randomiser.FiftyFiftyChance() && battingPlayers.Any() && bowlingPlayers.Any())
            {
                innings.PlayerInnings.Add(CreateRandomPlayerInnings(battingPlayers.Count, battingPlayers[_randomiser.PositiveIntegerLessThan(battingPlayers.Count)], bowlingPlayers[_randomiser.PositiveIntegerLessThan(bowlingPlayers.Count)], bowlingPlayers[_randomiser.PositiveIntegerLessThan(bowlingPlayers.Count)]));
            }

            // pick 4 players to bowl - note there may be other bowlers recorded as taking wickets on the batting card above
            var bowlers = new List<PlayerIdentity>();
            var comparer = new PlayerIdentityEqualityComparer();

            // if there's a player with multiple identities in this team, let both identities bowl!
            var playerWithMultipleIdentitiesInThisTeam = bowlingPlayers.FirstOrDefault(x => bowlingPlayers.Count(p => p.Player.PlayerId == x.Player.PlayerId) > 1);
            if (playerWithMultipleIdentitiesInThisTeam != null) { bowlers.Add(playerWithMultipleIdentitiesInThisTeam); }

            while (bowlers.Count < 4 && bowlingPlayers.Count >= 4)
            {
                var potentialBowler = bowlingPlayers[_randomiser.PositiveIntegerLessThan(bowlingPlayers.Count)];
                if (!bowlers.Contains(potentialBowler, comparer)) { bowlers.Add(potentialBowler); }
            }

            // Create up to 12 random overs, or a missing bowling card
            var hasBowlingData = _randomiser.PositiveIntegerLessThan(5) > 0;
            if (!innings.OverSets.Any())
            {
                innings.OverSets.Add(new OverSet
                {
                    OverSetId = Guid.NewGuid(),
                    OverSetNumber = 1,
                    Overs = _randomiser.Between(8, 12),
                    BallsPerOver = 8
                });
            }
            if (hasBowlingData && bowlers.Count >= 4)
            {
                for (var i = 1; i <= innings.OverSets[0].Overs; i++)
                {
                    if (i < 6 && i % 2 == 1) { innings.OversBowled.Add(CreateRandomOver(innings.OverSets[0], i, bowlers[0])); }
                    if (i < 6 && i % 2 == 0) { innings.OversBowled.Add(CreateRandomOver(innings.OverSets[0], i, bowlers[1])); }
                    if (i >= 6 && i % 2 == 1) { innings.OversBowled.Add(CreateRandomOver(innings.OverSets[0], i, bowlers[2])); }
                    if (i >= 6 && i % 2 == 0) { innings.OversBowled.Add(CreateRandomOver(innings.OverSets[0], i, bowlers[3])); }
                }
            }
        }

        private List<OverSet> CreateOverSets()
        {
            return new List<OverSet> { new OverSet { OverSetId = Guid.NewGuid(), OverSetNumber = 1, Overs = 15, BallsPerOver = 8 } };
        }

        private Over CreateRandomOver(OverSet overSet, int overNumber, PlayerIdentity playerIdentity)
        {
            // BallsBowled is usually provided but over data beyond the bowler name can be missing. 
            // The last over is often fewer balls.
            var ballsBowled = _randomiser.PositiveIntegerLessThan(10) == 0 ? (int?)null : 8;
            if (overNumber == 12) { ballsBowled = _randomiser.PositiveIntegerLessThan(9); }

            // Random numbers for the over, simulating missing data
            int? noBalls = null, wides = null, runsConceded = null;
            if (ballsBowled.HasValue)
            {
                noBalls = _randomiser.PositiveIntegerLessThan(4) == 0 ? (int?)null : _randomiser.PositiveIntegerLessThan(5);
                wides = _randomiser.PositiveIntegerLessThan(4) == 0 ? (int?)null : _randomiser.PositiveIntegerLessThan(5);
                runsConceded = _randomiser.PositiveIntegerLessThan(4) == 0 ? (int?)null : _randomiser.Between(-5, 19); // Can be negative in a match where penalty runs are scored for losing a wicket
            }

            return new Over
            {
                OverId = Guid.NewGuid(),
                OverSet = overSet,
                Bowler = playerIdentity,
                OverNumber = overNumber,
                BallsBowled = ballsBowled,
                NoBalls = noBalls,
                Wides = wides,
                RunsConceded = runsConceded
            };
        }

        private PlayerInnings CreateRandomPlayerInnings(int battingPosition, PlayerIdentity batter, PlayerIdentity? fielderOrMissingData, PlayerIdentity? bowlerOrMissingData)
        {
            var dismissalTypes = Enum.GetValues(typeof(DismissalType));
            var dismissal = (DismissalType)dismissalTypes.GetValue(_randomiser.PositiveIntegerLessThan(dismissalTypes.Length))!;
            if (dismissal != DismissalType.Caught || dismissal != DismissalType.RunOut)
            {
                fielderOrMissingData = null;
            }
            if (!StatisticsConstants.DISMISSALS_CREDITED_TO_BOWLER.Contains(dismissal))
            {
                bowlerOrMissingData = null;
            }
            var runsScored = _randomiser.PositiveIntegerLessThan(2) == 0 ? _randomiser.Between(-20, 101) : (int?)null; // simulate missing data; can also be negative in a match where penalty runs are awarded if the player is out
            var ballsFaced = _randomiser.PositiveIntegerLessThan(2) == 0 ? _randomiser.PositiveIntegerLessThan(151) : (int?)null; // simulate missing data
            if (dismissal == DismissalType.DidNotBat || dismissal == DismissalType.TimedOut)
            {
                runsScored = null;
                ballsFaced = null;
            }

            return new PlayerInnings
            {
                PlayerInningsId = Guid.NewGuid(),
                BattingPosition = battingPosition,
                Batter = batter,
                DismissalType = dismissal,
                DismissedBy = fielderOrMissingData,
                Bowler = bowlerOrMissingData,
                RunsScored = runsScored,
                BallsFaced = ballsFaced
            };
        }
    }
}
