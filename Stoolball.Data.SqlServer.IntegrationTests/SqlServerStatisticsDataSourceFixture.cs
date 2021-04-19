using System;
using System.Collections.Generic;
using System.Linq;
using Stoolball.Competitions;
using Stoolball.Matches;
using Stoolball.MatchLocations;
using Stoolball.Statistics;
using Stoolball.Teams;

namespace Stoolball.Data.SqlServer.IntegrationTests
{
    public sealed class SqlServerStatisticsDataSourceFixture : BaseSqlServerFixture
    {
        public Player BowlerWithMultipleIdentities { get; private set; }

        public List<Player> Players { get; private set; } = new List<Player>();
        public List<PlayerInnings> PlayerInnings { get; private set; } = new List<PlayerInnings>();
        public List<PlayerIdentity> PlayerIdentities { get; private set; } = new List<PlayerIdentity>();
        public List<Match> Matches { get; private set; } = new List<Match>();
        public Player PlayerWithFifthAndSixthInningsTheSame { get; private set; }
        public Player PlayerWithFifthAndSixthBowlingFiguresTheSame { get; private set; }
        public Team TeamWithClub { get; set; }
        public List<Team> Teams { get; set; } = new List<Team>();
        public List<MatchLocation> MatchLocations { get; private set; } = new List<MatchLocation>();
        public List<Competition> Competitions { get; private set; } = new List<Competition>();
        public List<Player> PlayersWithMultipleIdentities { get; private set; } = new List<Player>();

        public SqlServerStatisticsDataSourceFixture() : base("StoolballStatisticsDataSourceIntegrationTests")
        {
            // Populate seed data so that there's a consistent baseline for each test run
            SeedDatabase();
        }

        protected override void SeedDatabase()
        {
            var seedDataGenerator = new SeedDataGenerator();
            var playerIdentityFinder = new PlayerIdentityFinder();
            using (var connection = ConnectionFactory.CreateDatabaseConnection())
            {
                connection.Open();

                var repo = new SqlServerIntegrationTestsRepository(connection);

                // Create utilities to randomise and build data
                var randomiser = new Random();
                var statisticsBuilder = new PlayerInMatchStatisticsBuilder(new PlayerIdentityFinder(), new OversHelper());

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

                // Create a pool of match locations 
                for (var i = 0; i < 5; i++)
                {
                    MatchLocations.Add(seedDataGenerator.CreateMatchLocationWithMinimalDetails());
                }

                // Create a pool of competitions
                for (var i = 0; i < 5; i++)
                {
                    Competitions.Add(seedDataGenerator.CreateCompetitionWithMinimalDetails());
                    Competitions[Competitions.Count - 1].Seasons.Add(seedDataGenerator.CreateSeasonWithMinimalDetails(Competitions[Competitions.Count - 1], DateTime.Now.Year - i, DateTime.Now.Year - i));
                }

                // Randomly assign at least one player from each team a second identity
                foreach (var (team, playerIdentities) in poolOfTeams)
                {
                    var player1 = playerIdentities[randomiser.Next(playerIdentities.Count)];
                    var (targetTeam, targetIdentities) = poolOfTeams[randomiser.Next(poolOfTeams.Count)];
                    var player2 = targetIdentities[randomiser.Next(targetIdentities.Count)];
                    player2.Player = player1.Player;

                    PlayersWithMultipleIdentities.Add(player2.Player);
                }

                // Create matches for them to play in, with scorecards
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

                    Matches.Add(seedDataGenerator.CreateMatchInThePastWithMinimalDetails());
                    Matches[i].StartTime = DateTimeOffset.UtcNow.AddMonths((i * -1) - 1);
                    Matches[i].Teams.Add(teamAInMatch);
                    Matches[i].Teams.Add(teamBInMatch);

                    // Some matches should have multiple innings
                    if (randomiser.Next(4) == 0)
                    {
                        Matches[i].MatchInnings.Add(CreateMatchInnings(randomiser, 3));
                        Matches[i].MatchInnings.Add(CreateMatchInnings(randomiser, 4));
                    }

                    foreach (var innings in Matches[i].MatchInnings.Where(x => x.InningsOrderInMatch % 2 == 1))
                    {
                        var pairedInnings = Matches[i].MatchInnings.Single(x => x.InningsPair() == innings.InningsPair() && x.MatchInningsId != innings.MatchInningsId);
                        CreateRandomScorecardData(randomiser, Matches[i], innings, teamAInMatch, teamBInMatch, teamAPlayers, teamBPlayers);
                        CreateRandomScorecardData(randomiser, Matches[i], pairedInnings, teamBInMatch, teamAInMatch, teamBPlayers, teamAPlayers);
                    }

                    // Most matches have a match location
                    if (randomiser.Next(4) != 0)
                    {
                        Matches[i].MatchLocation = MatchLocations[randomiser.Next(MatchLocations.Count)];
                    }

                    // Most matches have a season and competition
                    if (randomiser.Next(4) != 0)
                    {
                        Matches[i].Season = Competitions[randomiser.Next(Competitions.Count)].Seasons.First();
                    }
                }

                // Find any player with at least six innings, and make the sixth best score the same as the fifth so that we can test retrieving a top five + any equal results
                var inningsForPlayerWithAtLeast6Scores = Matches
                                 .SelectMany(x => x.MatchInnings) // for each innings of each match...
                                 .SelectMany(x => x.PlayerInnings.Where(i => i.DismissalType != DismissalType.DidNotBat && i.DismissalType != DismissalType.TimedOut && i.RunsScored.HasValue)) // get the player innings where the player got to bat...
                                 .GroupBy(x => x.Batter.Player.PlayerId) // separate them into a group of innings for each player...
                                 .First(x => x.Count() > 5) // get the first player that had more than 5 such innings...
                                 .Select(x => x) // get those innings out of the IGrouping structure...
                                 .OrderByDescending(x => x.RunsScored) // sort the best scores first, because that's what the query we're testing will do...
                                 .ToList(); // make it possible to access innings by index

                // Make the sixth innings the same as the fifth, including anything that might affect the out/not out status.
                inningsForPlayerWithAtLeast6Scores[5].DismissalType = inningsForPlayerWithAtLeast6Scores[4].DismissalType;
                inningsForPlayerWithAtLeast6Scores[5].DismissedBy = inningsForPlayerWithAtLeast6Scores[4].DismissedBy;
                inningsForPlayerWithAtLeast6Scores[5].Bowler = inningsForPlayerWithAtLeast6Scores[4].Bowler;
                inningsForPlayerWithAtLeast6Scores[5].RunsScored = inningsForPlayerWithAtLeast6Scores[4].RunsScored;

                // The assertion expects the fifth and sixth innings to be the same, but to be different that any that come before or after in the
                // result set. So make sure those others are different.

                // Step 1: Make room below if required
                if (inningsForPlayerWithAtLeast6Scores.Count > 6 && inningsForPlayerWithAtLeast6Scores[5].RunsScored == 0)
                {
                    inningsForPlayerWithAtLeast6Scores[4].RunsScored++;
                    inningsForPlayerWithAtLeast6Scores[5].RunsScored++;
                }

                // Step 2: Ensure earlier scores are higher
                for (var i = 0; i < 4; i++)
                {
                    if (inningsForPlayerWithAtLeast6Scores[i].RunsScored == inningsForPlayerWithAtLeast6Scores[4].RunsScored)
                    {
                        inningsForPlayerWithAtLeast6Scores[i].RunsScored++;
                    }
                }

                // Step 3: Ensure later scores are lower, but not below 0
                for (var i = 6; i < inningsForPlayerWithAtLeast6Scores.Count; i++)
                {
                    if (inningsForPlayerWithAtLeast6Scores[i].RunsScored > 0)
                    {
                        inningsForPlayerWithAtLeast6Scores[i].RunsScored--;
                    }
                    else
                    {
                        inningsForPlayerWithAtLeast6Scores[i].RunsScored = 0;
                    }
                }

                PlayerWithFifthAndSixthInningsTheSame = inningsForPlayerWithAtLeast6Scores.First().Batter.Player;

                // Find any player with at least six sets of bowling figures, and make the sixth set the same as the fifth so that we can test retrieving a top five + any equal results
                var inningsForPlayerWithAtLeast6BowlingFigures = Matches
                                 .SelectMany(x => x.MatchInnings) // for each innings of each match...
                                 .SelectMany(x => x.BowlingFigures) // get the bowling figures...
                                 .GroupBy(x => x.Bowler.Player.PlayerId) // separate them into a group of figures for each player...
                                 .First(x => x.Count() > 5) // get the first player that had more than 5 sets of bowling figures...
                                 .Select(x => x) // get those figures out of the IGrouping structure...
                                 .OrderByDescending(x => x.Wickets).ThenByDescending(x => x.RunsConceded.HasValue).ThenBy(x => x.RunsConceded) // sort the best scores first, because that's what the query we're testing will do...
                                 .ToList(); // make it possible to access figures by index

                // Make the sixth set of figures the same as the fifth.
                // If there are more, make sure they're worse so we know what to assert.
                inningsForPlayerWithAtLeast6BowlingFigures[5].Overs = inningsForPlayerWithAtLeast6BowlingFigures[4].Overs;
                inningsForPlayerWithAtLeast6BowlingFigures[5].Maidens = inningsForPlayerWithAtLeast6BowlingFigures[4].Maidens;
                inningsForPlayerWithAtLeast6BowlingFigures[5].RunsConceded = inningsForPlayerWithAtLeast6BowlingFigures[4].RunsConceded;
                inningsForPlayerWithAtLeast6BowlingFigures[5].Wickets = inningsForPlayerWithAtLeast6BowlingFigures[4].Wickets;
                for (var i = 6; i < inningsForPlayerWithAtLeast6BowlingFigures.Count; i++)
                {
                    inningsForPlayerWithAtLeast6BowlingFigures[i].RunsConceded++;
                }

                PlayerWithFifthAndSixthBowlingFiguresTheSame = inningsForPlayerWithAtLeast6BowlingFigures.First().Bowler.Player;


                // Add entities to expected collections for testing
                var teamComparer = new TeamEqualityComparer();
                var identityComparer = new PlayerIdentityEqualityComparer();
                Teams = Matches.SelectMany(x => x.Teams).Select(x => x.Team).Distinct(teamComparer).ToList(); // teams that got used
                TeamWithClub = Teams.First(x => x.Club != null);

                foreach (var (team, playerIdentities) in poolOfTeams.Where(x => Teams.Contains(x.team, teamComparer)))
                {
                    PlayerIdentities.AddRange(playerIdentities);

                    // Since all player identities in a team get used, we can update individual participation stats from the team
                    var matchesPlayedByThisTeam = Matches.Where(x => x.Teams.Any(t => t.Team.TeamId == team.TeamId)).ToList();
                    foreach (var identity in playerIdentities)
                    {
                        identity.TotalMatches = matchesPlayedByThisTeam.Count;
                        identity.FirstPlayed = matchesPlayedByThisTeam.Min(x => x.StartTime);
                        identity.LastPlayed = matchesPlayedByThisTeam.Max(x => x.StartTime);
                    }
                }

                // Find any player who has multiple identities and bowled
                BowlerWithMultipleIdentities = Matches
                    .SelectMany(x => x.MatchInnings)
                    .SelectMany(x => x.BowlingFigures)
                    .Where(x => PlayersWithMultipleIdentities.Contains(x.Bowler.Player, new PlayerEqualityComparer()))
                    .Select(x => x.Bowler.Player)
                    .First();
                BowlerWithMultipleIdentities.PlayerIdentities.Clear();
                BowlerWithMultipleIdentities.PlayerIdentities.AddRange(PlayerIdentities.Where(x => x.Player.PlayerId == BowlerWithMultipleIdentities.PlayerId));

                // Get all players that were used and their batting records
                Players = poolOfTeams.SelectMany(x => x.identities).Select(x => x.Player).Distinct(new PlayerEqualityComparer()).ToList();
                PlayerInnings = Matches.SelectMany(x => x.MatchInnings).SelectMany(x => x.PlayerInnings).ToList();

                // Remove any entities that didn't get used
                MatchLocations.RemoveAll(x => !Matches.Select(m => m.MatchLocation?.MatchLocationId).Contains(x.MatchLocationId));
                Competitions.RemoveAll(x => !Matches.Select(m => m.Season?.Competition?.CompetitionId).Contains(x.CompetitionId));

                // Add all of that to the database
                foreach (var player in Players)
                {
                    repo.CreatePlayer(player);
                }

                foreach (var (team, playerIdentities) in poolOfTeams)
                {
                    if (!Teams.Contains(team, teamComparer)) continue;

                    if (team.Club != null)
                    {
                        repo.CreateClub(team.Club);
                    }
                    repo.CreateTeam(team);
                    foreach (var playerIdentity in playerIdentities)
                    {
                        repo.CreatePlayerIdentity(playerIdentity);
                    }
                }
                foreach (var location in MatchLocations)
                {
                    repo.CreateMatchLocation(location);
                }
                foreach (var competition in Competitions)
                {
                    repo.CreateCompetition(competition);
                    foreach (var season in competition.Seasons)
                    {
                        repo.CreateSeason(season, competition.CompetitionId.Value);
                    }
                }
                foreach (var match in Matches)
                {
                    repo.CreateMatch(match);

                    var statisticsRecords = statisticsBuilder.BuildStatisticsForMatch(match);
                    foreach (var record in statisticsRecords)
                    {
                        repo.CreatePlayerInMatchStatisticsRecord(record);
                    }
                }
            }
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

        private static void CreateRandomScorecardData(Random randomiser, Match match, MatchInnings innings, TeamInMatch battingTeam, TeamInMatch bowlingTeam, List<PlayerIdentity> battingPlayers, List<PlayerIdentity> bowlingPlayers)
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

            var bowlingFigures = new BowlingFiguresCalculator();
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

        private static (Team, List<PlayerIdentity>) CreateATeamWithPlayers(SeedDataGenerator seedDataGenerator, string playerName)
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
