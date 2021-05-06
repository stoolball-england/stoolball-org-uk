using System;
using System.Collections.Generic;
using System.Linq;
using Stoolball.Competitions;
using Stoolball.Matches;
using Stoolball.MatchLocations;
using Stoolball.Statistics;
using Stoolball.Teams;

namespace Stoolball.Data.SqlServer.IntegrationTests.Fixtures
{
    // TODO: Situations not yet being consistently recreated 
    // 1. A team plays itself (never generated)
    // 2. A player with multiple identities is recorded with two identities in the same innings
    // 3. A player with multiple identities *on different sides in the match* is recorded with two identities in the same innings
    public class RandomSeedDataGenerator
    {
        private readonly FixedSeedDataGenerator _fixedSeedDataGenerator;

        public RandomSeedDataGenerator(FixedSeedDataGenerator fixedSeedDataGenerator)
        {
            _fixedSeedDataGenerator = fixedSeedDataGenerator ?? throw new ArgumentNullException(nameof(fixedSeedDataGenerator));
        }

        private static List<(Team team, List<PlayerIdentity> identities)> GenerateTeams(FixedSeedDataGenerator seedDataGenerator)
        {
            // Create a pool of teams of 8 players
            var poolOfTeams = new List<(Team team, List<PlayerIdentity> identities)>();
            for (var i = 0; i < 5; i++)
            {
                poolOfTeams.Add(CreateATeamWithPlayers(seedDataGenerator, $"Team {i + 1} pool player"));
                if (i % 2 == 0)
                {
                    poolOfTeams[poolOfTeams.Count - 1].team.Club = seedDataGenerator.CreateClubWithMinimalDetails();
                }
            }
            return poolOfTeams;
        }

        public TestData GenerateTestData()
        {
            var testData = new TestData();
            var playerIdentityFinder = new PlayerIdentityFinder();
            var bowlingFigures = new BowlingFiguresCalculator(new OversHelper());
            var teamComparer = new TeamEqualityComparer();
            var playerComparer = new PlayerEqualityComparer();

            testData.Matches = GenerateMatchData(bowlingFigures);
            testData.MatchLocations = testData.Matches.Where(m => m.MatchLocation != null).Select(m => m.MatchLocation).Distinct(new MatchLocationEqualityComparer()).ToList();
            testData.Competitions = testData.Matches.Where(m => m.Season != null).Select(m => m.Season.Competition).Distinct(new CompetitionEqualityComparer()).ToList();
            testData.Teams = testData.Matches.SelectMany(x => x.Teams).Select(x => x.Team).Distinct(teamComparer).ToList(); // teams that got used
            testData.TeamWithClub = testData.Teams.First(x => x.Club != null);

            testData.PlayerIdentities = testData.Matches.SelectMany(m => playerIdentityFinder.PlayerIdentitiesInMatch(m)).Distinct(new PlayerIdentityEqualityComparer()).ToList();
            testData.Players = testData.PlayerIdentities.Select(x => x.Player).Distinct(playerComparer).ToList();
            foreach (var identity in testData.PlayerIdentities)
            {
                if (testData.PlayerIdentities.Count(x => x.Player.PlayerId == identity.Player.PlayerId) > 1 &&
                    !testData.PlayersWithMultipleIdentities.Any(x => x.PlayerId == identity.Player.PlayerId))
                {
                    testData.PlayersWithMultipleIdentities.Add(identity.Player);
                }
            }

            // Since all player identities in a team get used, we can update individual participation stats from the team
            foreach (var identity in testData.PlayerIdentities)
            {
                var matchesPlayedByThisTeam = testData.Matches.Where(x => x.Teams.Any(t => t.Team.TeamId == identity.Team.TeamId)).ToList();
                identity.TotalMatches = matchesPlayedByThisTeam.Count;
                identity.FirstPlayed = matchesPlayedByThisTeam.Min(x => x.StartTime);
                identity.LastPlayed = matchesPlayedByThisTeam.Max(x => x.StartTime);
            }

            // Find any player who has multiple identities and bowled
            testData.BowlerWithMultipleIdentities = testData.Matches
                .SelectMany(x => x.MatchInnings)
                .SelectMany(x => x.BowlingFigures)
                .Where(x => testData.PlayersWithMultipleIdentities.Contains(x.Bowler.Player, playerComparer))
                .Select(x => x.Bowler.Player)
                .First();
            testData.BowlerWithMultipleIdentities.PlayerIdentities.Clear();
            testData.BowlerWithMultipleIdentities.PlayerIdentities.AddRange(testData.PlayerIdentities.Where(x => x.Player.PlayerId == testData.BowlerWithMultipleIdentities.PlayerId));

            // Get all batting records
            testData.PlayerInnings = testData.Matches.SelectMany(x => x.MatchInnings).SelectMany(x => x.PlayerInnings).ToList();

            return testData;
        }

        private List<Match> GenerateMatchData(BowlingFiguresCalculator bowlingFigures)
        {
            if (bowlingFigures is null)
            {
                throw new ArgumentNullException(nameof(bowlingFigures));
            }

            // Create utilities to randomise and build data
            var randomiser = new Random();

            var poolOfTeams = GenerateTeams(_fixedSeedDataGenerator);

            // Create a pool of match locations 
            var matchLocations = new List<MatchLocation>();
            for (var i = 0; i < 5; i++)
            {
                matchLocations.Add(_fixedSeedDataGenerator.CreateMatchLocationWithMinimalDetails());
            }

            // Create a pool of competitions
            var competitions = new List<Competition>();
            for (var i = 0; i < 5; i++)
            {
                competitions.Add(_fixedSeedDataGenerator.CreateCompetitionWithMinimalDetails());
                competitions[competitions.Count - 1].Seasons.Add(_fixedSeedDataGenerator.CreateSeasonWithMinimalDetails(competitions[competitions.Count - 1], DateTime.Now.Year - i, DateTime.Now.Year - i));
            }

            // Randomly assign at least one player from each team a second identity
            foreach (var (team, playerIdentities) in poolOfTeams)
            {
                var player1 = playerIdentities[randomiser.Next(playerIdentities.Count)];
                var (targetTeam, targetIdentities) = poolOfTeams[randomiser.Next(poolOfTeams.Count)];
                var player2 = targetIdentities[randomiser.Next(targetIdentities.Count)];
                player2.Player = player1.Player;
            }

            // Create matches for them to play in, with scorecards
            var matches = new List<Match>();
            for (var i = 0; i < 40; i++)
            {
                var homeTeamBatsFirst = randomiser.Next(2) == 0;

                var (teamA, teamAPlayers) = poolOfTeams[randomiser.Next(poolOfTeams.Count)];
                (Team teamB, List<PlayerIdentity> teamBPlayers) = (null, null);
                do
                {
                    (teamB, teamBPlayers) = poolOfTeams[randomiser.Next(poolOfTeams.Count)];
                }
                while (teamA.TeamId == teamB.TeamId);

                var teamAInMatch = new TeamInMatch
                {
                    MatchTeamId = Guid.NewGuid(),
                    TeamRole = homeTeamBatsFirst ? TeamRole.Home : TeamRole.Away,
                    Team = teamA,
                    WonToss = randomiser.Next(2) == 0,
                    BattedFirst = true
                };

                var teamBInMatch = new TeamInMatch
                {
                    MatchTeamId = Guid.NewGuid(),
                    TeamRole = homeTeamBatsFirst ? TeamRole.Away : TeamRole.Home,
                    Team = teamB,
                    WonToss = !teamAInMatch.WonToss,
                    BattedFirst = false
                };

                matches.Add(_fixedSeedDataGenerator.CreateMatchInThePastWithMinimalDetails());
                matches[i].StartTime = DateTimeOffset.UtcNow.AddMonths(i * -1 - 1);
                matches[i].Teams.Add(teamAInMatch);
                matches[i].Teams.Add(teamBInMatch);

                // Some matches should have multiple innings
                if (randomiser.Next(4) == 0)
                {
                    matches[i].MatchInnings.Add(CreateMatchInnings(randomiser, 3));
                    matches[i].MatchInnings.Add(CreateMatchInnings(randomiser, 4));
                }

                foreach (var innings in matches[i].MatchInnings.Where(x => x.InningsOrderInMatch % 2 == 1))
                {
                    var pairedInnings = matches[i].MatchInnings.Single(x => x.InningsPair() == innings.InningsPair() && x.MatchInningsId != innings.MatchInningsId);
                    CreateRandomScorecardData(randomiser, matches[i], innings, teamAInMatch, teamBInMatch, teamAPlayers, teamBPlayers, bowlingFigures);
                    CreateRandomScorecardData(randomiser, matches[i], pairedInnings, teamBInMatch, teamAInMatch, teamBPlayers, teamAPlayers, bowlingFigures);
                }

                // Most matches have a match location
                if (randomiser.Next(4) != 0)
                {
                    matches[i].MatchLocation = matchLocations[randomiser.Next(matchLocations.Count)];
                }

                // Most matches have a season and competition
                if (randomiser.Next(4) != 0)
                {
                    matches[i].Season = competitions[randomiser.Next(competitions.Count)].Seasons.First();
                }
            }

            return matches;
        }

        private static MatchInnings CreateMatchInnings(Random randomiser, int inningsOrderInMatch)
        {
            return new MatchInnings
            {
                MatchInningsId = Guid.NewGuid(),
                InningsOrderInMatch = inningsOrderInMatch,
                NoBalls = randomiser.Next(30),
                Wides = randomiser.Next(30),
                Byes = randomiser.Next(30),
                BonusOrPenaltyRuns = randomiser.Next(-5, 5),
                Runs = randomiser.Next(100, 250),
                Wickets = randomiser.Next(11)
            };
        }

        private static void CreateRandomScorecardData(Random randomiser, Match match, MatchInnings innings, TeamInMatch battingTeam, TeamInMatch bowlingTeam, List<PlayerIdentity> battingPlayers, List<PlayerIdentity> bowlingPlayers, IBowlingFiguresCalculator bowlingFigures)
        {
            innings.BattingMatchTeamId = battingTeam.MatchTeamId;
            innings.BowlingMatchTeamId = bowlingTeam.MatchTeamId;
            innings.BattingTeam = battingTeam;
            innings.BowlingTeam = bowlingTeam;

            for (var p = 0; p < battingPlayers.Count; p++)
            {
                var fielderOrMissingData = randomiser.Next(2) == 0 ? bowlingPlayers[randomiser.Next(bowlingPlayers.Count)] : null;
                var bowlerOrMissingData = randomiser.Next(2) == 0 ? bowlingPlayers[randomiser.Next(bowlingPlayers.Count)] : null;
                innings.PlayerInnings.Add(CreateRandomPlayerInnings(randomiser, match, p + 1, battingPlayers[p], fielderOrMissingData, bowlerOrMissingData));
            }

            // sometimes pick a random player to bat twice in the innings
            if (randomiser.Next(2) == 0)
            {
                innings.PlayerInnings.Add(CreateRandomPlayerInnings(randomiser, match, battingPlayers.Count, battingPlayers[randomiser.Next(battingPlayers.Count)], bowlingPlayers[randomiser.Next(bowlingPlayers.Count)], bowlingPlayers[randomiser.Next(bowlingPlayers.Count)]));
            }

            // pick 4 players to bowl - note there may be other bowlers recorded as taking wickets on the batting card above
            var bowlers = new List<PlayerIdentity>();
            var comparer = new PlayerIdentityEqualityComparer();
            while (bowlers.Count < 4)
            {
                var potentialBowler = bowlingPlayers[randomiser.Next(bowlingPlayers.Count)];
                if (!bowlers.Contains(potentialBowler, comparer)) { bowlers.Add(potentialBowler); }
            }

            // Create up to 12 random overs, or a missing bowling card
            var hasBowlingData = randomiser.Next(5) > 0;
            innings.OverSets.Add(new OverSet
            {
                OverSetId = Guid.NewGuid(),
                OverSetNumber = 1,
                Overs = randomiser.Next(8, 13),
                BallsPerOver = 8
            });
            if (hasBowlingData)
            {
                for (var i = 1; i <= innings.OverSets[0].Overs; i++)
                {
                    if (i < 6 && i % 2 == 1) { innings.OversBowled.Add(CreateRandomOver(randomiser, innings.OverSets[0], i, bowlers[0])); }
                    if (i < 6 && i % 2 == 0) { innings.OversBowled.Add(CreateRandomOver(randomiser, innings.OverSets[0], i, bowlers[1])); }
                    if (i >= 6 && i % 2 == 1) { innings.OversBowled.Add(CreateRandomOver(randomiser, innings.OverSets[0], i, bowlers[2])); }
                    if (i >= 6 && i % 2 == 0) { innings.OversBowled.Add(CreateRandomOver(randomiser, innings.OverSets[0], i, bowlers[3])); }
                }
            }

            innings.BowlingFigures = bowlingFigures.CalculateBowlingFigures(innings);
        }

        private static Over CreateRandomOver(Random randomiser, OverSet overSet, int overNumber, PlayerIdentity playerIdentity)
        {
            // BallsBowled is usually provided but over data beyond the bowler name can be missing. 
            // The last over is often fewer balls.
            var ballsBowled = randomiser.Next(10) == 0 ? (int?)null : 8;
            if (overNumber == 12) { ballsBowled = randomiser.Next(9); }

            // Random numbers for the over, simulating missing data
            int? noBalls = null, wides = null, runsConceded = null;
            if (ballsBowled.HasValue)
            {
                noBalls = randomiser.Next(4) == 0 ? (int?)null : randomiser.Next(5);
                wides = randomiser.Next(4) == 0 ? (int?)null : randomiser.Next(5);
                runsConceded = randomiser.Next(4) == 0 ? (int?)null : randomiser.Next(20);
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

        private static PlayerInnings CreateRandomPlayerInnings(Random randomiser, Match match, int battingPosition, PlayerIdentity batter, PlayerIdentity fielderOrMissingData, PlayerIdentity bowlerOrMissingData)
        {
            var dismissalTypes = Enum.GetValues(typeof(DismissalType));
            var dismissal = (DismissalType)dismissalTypes.GetValue(randomiser.Next(dismissalTypes.Length));
            if (dismissal != DismissalType.Caught || dismissal != DismissalType.RunOut)
            {
                fielderOrMissingData = null;
            }
            if (!StatisticsConstants.DISMISSALS_CREDITED_TO_BOWLER.Contains(dismissal))
            {
                bowlerOrMissingData = null;
            }
            var runsScored = randomiser.Next(2) == 0 ? randomiser.Next(102) : (int?)null; // simulate missing data;
            var ballsFaced = randomiser.Next(2) == 0 ? randomiser.Next(151) : (int?)null; // simulate missing data
            if (dismissal == DismissalType.DidNotBat || dismissal == DismissalType.TimedOut)
            {
                runsScored = null;
                ballsFaced = null;
            }

            return new PlayerInnings
            {
                PlayerInningsId = Guid.NewGuid(),
                Match = match,
                BattingPosition = battingPosition,
                Batter = batter,
                DismissalType = dismissal,
                DismissedBy = fielderOrMissingData,
                Bowler = bowlerOrMissingData,
                RunsScored = runsScored,
                BallsFaced = ballsFaced
            };
        }

        private static (Team, List<PlayerIdentity>) CreateATeamWithPlayers(FixedSeedDataGenerator seedDataGenerator, string playerName)
        {
            var poolOfPlayers = new List<PlayerIdentity>();
            for (var i = 0; i < 8; i++)
            {
                var player = seedDataGenerator.CreatePlayer($"{playerName} {i + 1}");
                var playerIdentity = player.PlayerIdentities.First();
                playerIdentity.Player = player;
                poolOfPlayers.Add(playerIdentity);
                poolOfPlayers[i].Team = poolOfPlayers[0].Team;
            }

            return (poolOfPlayers[0].Team, poolOfPlayers);
        }
    }
}
