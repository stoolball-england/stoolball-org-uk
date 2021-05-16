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
        private readonly Random _randomiser = new Random();
        private readonly FixedSeedDataGenerator _fixedSeedDataGenerator;
        private readonly IBowlingFiguresCalculator _bowlingFiguresCalculator;
        private readonly IPlayerIdentityFinder _playerIdentityFinder;
        private List<(Team team, List<PlayerIdentity> identities)> _teams;
        private List<MatchLocation> _matchLocations = new List<MatchLocation>();
        private List<Competition> _competitions = new List<Competition>();

        public RandomSeedDataGenerator(FixedSeedDataGenerator fixedSeedDataGenerator, IBowlingFiguresCalculator bowlingFiguresCalculator, IPlayerIdentityFinder playerIdentityFinder)
        {
            _fixedSeedDataGenerator = fixedSeedDataGenerator ?? throw new ArgumentNullException(nameof(fixedSeedDataGenerator));
            _bowlingFiguresCalculator = bowlingFiguresCalculator ?? throw new ArgumentNullException(nameof(bowlingFiguresCalculator));
            _playerIdentityFinder = playerIdentityFinder ?? throw new ArgumentNullException(nameof(playerIdentityFinder));
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
            var playerComparer = new PlayerEqualityComparer();

            testData.Matches = GenerateMatchData();
            testData.MatchLocations = testData.Matches.Where(m => m.MatchLocation != null).Select(m => m.MatchLocation).Distinct(new MatchLocationEqualityComparer()).ToList();
            testData.Competitions = testData.Matches.Where(m => m.Season != null).Select(m => m.Season.Competition).Distinct(new CompetitionEqualityComparer()).ToList();
            testData.Teams = testData.Matches.SelectMany(x => x.Teams).Select(x => x.Team).Distinct(new TeamEqualityComparer()).ToList(); // teams that got used
            testData.TeamWithClub = testData.Teams.First(x => x.Club != null);

            testData.PlayerIdentities = testData.Matches.SelectMany(m => _playerIdentityFinder.PlayerIdentitiesInMatch(m)).Distinct(new PlayerIdentityEqualityComparer()).ToList();
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

        private List<Match> GenerateMatchData()
        {
            // Create utilities to randomise and build data
            _teams = GenerateTeams(_fixedSeedDataGenerator);

            // Create a pool of match locations 
            for (var i = 0; i < 5; i++)
            {
                _matchLocations.Add(_fixedSeedDataGenerator.CreateMatchLocationWithMinimalDetails());
            }

            // Create a pool of competitions
            for (var i = 0; i < 5; i++)
            {
                _competitions.Add(_fixedSeedDataGenerator.CreateCompetitionWithMinimalDetails());
                _competitions[_competitions.Count - 1].Seasons.Add(_fixedSeedDataGenerator.CreateSeasonWithMinimalDetails(_competitions[_competitions.Count - 1], DateTime.Now.Year - i, DateTime.Now.Year - i));
            }

            // Randomly assign at least two players from each team a second identity - one on the same team, one on a different team.
            // This ensure we always have lots of teams with multiple identities for the same player for both scenarios.
            foreach (var (team, playerIdentities) in _teams)
            {
                // On the same team
                var player1 = playerIdentities[_randomiser.Next(playerIdentities.Count)];
                PlayerIdentity player2;
                do
                {
                    player2 = playerIdentities[_randomiser.Next(playerIdentities.Count)];
                } while (player1.PlayerIdentityId == player2.PlayerIdentityId);
                player2.Player = player1.Player;

                // On a different team
                var player3 = playerIdentities[_randomiser.Next(playerIdentities.Count)];
                (Team targetTeam, List<PlayerIdentity> targetIdentities) = (null, null);
                do
                {
                    (targetTeam, targetIdentities) = _teams[_randomiser.Next(_teams.Count)];
                } while (targetTeam.TeamId == team.TeamId);
                var player4 = targetIdentities[_randomiser.Next(targetIdentities.Count)];
                player4.Player = player3.Player;
            }

            // Create matches for them to play in, with scorecards
            var matches = new List<Match>();
            for (var i = 0; i < 40; i++)
            {
                var homeTeamBatsFirst = FiftyFiftyChance();

                var (teamA, teamAPlayers) = _teams[_randomiser.Next(_teams.Count)];
                (Team teamB, List<PlayerIdentity> teamBPlayers) = (null, null);
                do
                {
                    (teamB, teamBPlayers) = _teams[_randomiser.Next(_teams.Count)];
                }
                while (teamA.TeamId == teamB.TeamId);

                matches.Add(CreateMatchBetween(teamA, teamAPlayers, teamB, teamBPlayers, homeTeamBatsFirst));
            }

            // Pick any innings and create a five-wicket haul for someone
            var inningsWithFiveWicketHaul = matches.SelectMany(x => x.MatchInnings).Where(x => x.PlayerInnings.Any(pi => pi.Bowler != null)).First();
            var bowlerWithFiveWicketHaul = inningsWithFiveWicketHaul.PlayerInnings.First(x => x.Bowler != null).Bowler;
            for (var i = 0; i < _randomiser.Next(5, 7); i++)
            {
                inningsWithFiveWicketHaul.PlayerInnings[i].DismissalType = StatisticsConstants.DISMISSALS_CREDITED_TO_BOWLER[_randomiser.Next(StatisticsConstants.DISMISSALS_CREDITED_TO_BOWLER.Count)];
                inningsWithFiveWicketHaul.PlayerInnings[i].Bowler = bowlerWithFiveWicketHaul;
            }

            matches.Add(CreateMatchWithFieldingByMultipleIdentities());

            matches.Add(CreateMatchWithDifferentTeamsWhereSomeonePlaysOnBothTeams());

            // Ensure there's always an intra-club match to test
            matches.Add(CreateMatchBetween(_teams[0].team, _teams[0].identities, _teams[0].team, _teams[0].identities, FiftyFiftyChance()));

            // Generate bowling figures for each innings
            foreach (var innings in matches.SelectMany(x => x.MatchInnings))
            {
                innings.BowlingFigures = _bowlingFiguresCalculator.CalculateBowlingFigures(innings);
            }

            return matches;
        }

        private Match CreateMatchWithFieldingByMultipleIdentities()
        {
            var anyTeam1 = _teams[_randomiser.Next(_teams.Count)];
            var anyTeam2 = _teams[_randomiser.Next(_teams.Count)];
            var match = CreateMatchBetween(anyTeam1.team, anyTeam1.identities, anyTeam2.team, anyTeam2.identities, FiftyFiftyChance());

            // in the first innings a fielder should take catches under multiple identities
            var firstInnings = match.MatchInnings[0];
            var firstInningsIdentities = firstInnings.BowlingTeam.Team.TeamId == anyTeam1.team.TeamId ? anyTeam1.identities : anyTeam2.identities;

            var catcherWithMultipleIdentities = firstInningsIdentities.FirstOrDefault(x => firstInningsIdentities.Count(p => p.Player.PlayerId == x.Player.PlayerId) > 1).Player.PlayerId;
            var catcherIdentities = firstInningsIdentities.Where(x => x.Player.PlayerId == catcherWithMultipleIdentities).ToList();

            for (var i = 0; i < 6; i++)
            {
                if (i % 2 == 0)
                {
                    firstInnings.PlayerInnings[i].DismissalType = DismissalType.Caught;
                    firstInnings.PlayerInnings[i].DismissedBy = catcherIdentities[0];
                }
                else
                {
                    firstInnings.PlayerInnings[i].DismissalType = DismissalType.CaughtAndBowled;
                    firstInnings.PlayerInnings[i].DismissedBy = null;
                    firstInnings.PlayerInnings[i].Bowler = catcherIdentities[1];
                }
            }

            // in the second innings a fielder should complete run-outs under multiple identities
            var secondInnings = match.MatchInnings[1];
            var secondInningsIdentities = secondInnings.BowlingTeam.Team.TeamId == anyTeam1.team.TeamId ? anyTeam1.identities : anyTeam2.identities;

            var fielderWithMultipleIdentities = secondInningsIdentities.FirstOrDefault(x => secondInningsIdentities.Count(p => p.Player.PlayerId == x.Player.PlayerId) > 1).Player.PlayerId;
            var fielderIdentities = secondInningsIdentities.Where(x => x.Player.PlayerId == fielderWithMultipleIdentities).ToList();

            for (var i = 0; i < 6; i++)
            {
                secondInnings.PlayerInnings[i].DismissalType = DismissalType.RunOut;
                secondInnings.PlayerInnings[i].DismissedBy = fielderIdentities[i % 2];
                secondInnings.PlayerInnings[i].Bowler = null;
            }

            return match;
        }

        private Match CreateMatchWithDifferentTeamsWhereSomeonePlaysOnBothTeams()
        {
            // Ensure there's always a match to test where someone swaps sides during the innings (eg a batter is loaned as a fielder and takes a wicket)

            // 1. Find any player with identities on two teams
            var anyPlayerWithIdentitiesOnMultipleTeams = _teams.SelectMany(x => x.identities)
                .GroupBy(x => x.Player.PlayerId, x => x, (playerId, playerIdentities) => new Player { PlayerId = playerId, PlayerIdentities = playerIdentities.ToList() })
                .Where(x => x.PlayerIdentities.Select(t => t.Team.TeamId.Value).Distinct().Count() > 1)
                .First();

            // 2. Create a match between those teams
            var teamsForPlayer = _teams.Where(t => anyPlayerWithIdentitiesOnMultipleTeams.PlayerIdentities.Select(x => x.Team.TeamId).Contains(t.team.TeamId)).ToList();
            var match = CreateMatchBetween(teamsForPlayer[0].team, teamsForPlayer[0].identities, teamsForPlayer[1].team, teamsForPlayer[1].identities, FiftyFiftyChance());

            // 3. We know they'll be recorded as a batter in both innings. Ensure they take a wicket too.
            var wicketTaken = match.MatchInnings.First().PlayerInnings.First();
            wicketTaken.DismissalType = DismissalType.CaughtAndBowled;
            wicketTaken.Bowler = anyPlayerWithIdentitiesOnMultipleTeams.PlayerIdentities.First(x => x.Team.TeamId == match.MatchInnings.First().BowlingTeam.Team.TeamId);

            return match;
        }

        private bool FiftyFiftyChance()
        {
            return _randomiser.Next(2) == 0;
        }

        private Match CreateMatchBetween(Team teamA, List<PlayerIdentity> teamAPlayers, Team teamB, List<PlayerIdentity> teamBPlayers, bool homeTeamBatsFirst)
        {
            var teamAInMatch = new TeamInMatch
            {
                MatchTeamId = Guid.NewGuid(),
                TeamRole = homeTeamBatsFirst ? TeamRole.Home : TeamRole.Away,
                Team = teamA,
                WonToss = _randomiser.Next(2) == 0,
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

            var match = _fixedSeedDataGenerator.CreateMatchInThePastWithMinimalDetails();
            match.StartTime = DateTimeOffset.UtcNow.AddMonths(_randomiser.Next(30) * -1 - 1);
            match.Teams.Add(teamAInMatch);
            match.Teams.Add(teamBInMatch);

            // Some matches should have multiple innings
            if (_randomiser.Next(4) == 0)
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
            if (OneInFourChance())
            {
                match.MatchLocation = _matchLocations[_randomiser.Next(_matchLocations.Count)];
            }

            // Most matches have a season and competition
            if (OneInFourChance())
            {
                match.Season = _competitions[_randomiser.Next(_competitions.Count)].Seasons.First();
            }

            return match;
        }

        private bool OneInFourChance()
        {
            return _randomiser.Next(4) != 0;
        }

        private MatchInnings CreateMatchInnings(int inningsOrderInMatch)
        {
            return new MatchInnings
            {
                MatchInningsId = Guid.NewGuid(),
                InningsOrderInMatch = inningsOrderInMatch,
                NoBalls = _randomiser.Next(30),
                Wides = _randomiser.Next(30),
                Byes = _randomiser.Next(30),
                BonusOrPenaltyRuns = _randomiser.Next(-5, 5),
                Runs = _randomiser.Next(100, 250),
                Wickets = _randomiser.Next(11)
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
                var fielderOrMissingData = _randomiser.Next(2) == 0 ? bowlingPlayers[_randomiser.Next(bowlingPlayers.Count)] : null;
                var bowlerOrMissingData = _randomiser.Next(2) == 0 ? bowlingPlayers[_randomiser.Next(bowlingPlayers.Count)] : null;
                innings.PlayerInnings.Add(CreateRandomPlayerInnings(match, p + 1, battingPlayers[p], fielderOrMissingData, bowlerOrMissingData));
            }

            // sometimes pick a random player to bat twice in the innings
            if (FiftyFiftyChance())
            {
                innings.PlayerInnings.Add(CreateRandomPlayerInnings(match, battingPlayers.Count, battingPlayers[_randomiser.Next(battingPlayers.Count)], bowlingPlayers[_randomiser.Next(bowlingPlayers.Count)], bowlingPlayers[_randomiser.Next(bowlingPlayers.Count)]));
            }

            // pick 4 players to bowl - note there may be other bowlers recorded as taking wickets on the batting card above
            var bowlers = new List<PlayerIdentity>();
            var comparer = new PlayerIdentityEqualityComparer();

            // if there's a player with multiple identities in this team, let both identities bowl!
            var playerWithMultipleIdentitiesInThisTeam = bowlingPlayers.FirstOrDefault(x => bowlingPlayers.Count(p => p.Player.PlayerId == x.Player.PlayerId) > 1);
            if (playerWithMultipleIdentitiesInThisTeam != null) { bowlers.Add(playerWithMultipleIdentitiesInThisTeam); }

            while (bowlers.Count < 4)
            {
                var potentialBowler = bowlingPlayers[_randomiser.Next(bowlingPlayers.Count)];
                if (!bowlers.Contains(potentialBowler, comparer)) { bowlers.Add(potentialBowler); }
            }

            // Create up to 12 random overs, or a missing bowling card
            var hasBowlingData = _randomiser.Next(5) > 0;
            innings.OverSets.Add(new OverSet
            {
                OverSetId = Guid.NewGuid(),
                OverSetNumber = 1,
                Overs = _randomiser.Next(8, 13),
                BallsPerOver = 8
            });
            if (hasBowlingData)
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

        private Over CreateRandomOver(OverSet overSet, int overNumber, PlayerIdentity playerIdentity)
        {
            // BallsBowled is usually provided but over data beyond the bowler name can be missing. 
            // The last over is often fewer balls.
            var ballsBowled = _randomiser.Next(10) == 0 ? (int?)null : 8;
            if (overNumber == 12) { ballsBowled = _randomiser.Next(9); }

            // Random numbers for the over, simulating missing data
            int? noBalls = null, wides = null, runsConceded = null;
            if (ballsBowled.HasValue)
            {
                noBalls = _randomiser.Next(4) == 0 ? (int?)null : _randomiser.Next(5);
                wides = _randomiser.Next(4) == 0 ? (int?)null : _randomiser.Next(5);
                runsConceded = _randomiser.Next(4) == 0 ? (int?)null : _randomiser.Next(20);
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

        private PlayerInnings CreateRandomPlayerInnings(Match match, int battingPosition, PlayerIdentity batter, PlayerIdentity fielderOrMissingData, PlayerIdentity bowlerOrMissingData)
        {
            var dismissalTypes = Enum.GetValues(typeof(DismissalType));
            var dismissal = (DismissalType)dismissalTypes.GetValue(_randomiser.Next(dismissalTypes.Length));
            if (dismissal != DismissalType.Caught || dismissal != DismissalType.RunOut)
            {
                fielderOrMissingData = null;
            }
            if (!StatisticsConstants.DISMISSALS_CREDITED_TO_BOWLER.Contains(dismissal))
            {
                bowlerOrMissingData = null;
            }
            var runsScored = _randomiser.Next(2) == 0 ? _randomiser.Next(102) : (int?)null; // simulate missing data;
            var ballsFaced = _randomiser.Next(2) == 0 ? _randomiser.Next(151) : (int?)null; // simulate missing data
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
